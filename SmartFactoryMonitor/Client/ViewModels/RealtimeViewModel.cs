// Client/ViewModels/RealtimeViewModel.cs
using Client.Models;
using Client.Models.Dtos;
using Client.Service;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Client.ViewModels
{
    public class RealtimeViewModel : BindableBase, INavigationAware
    {
        // ======== 服务注入 ========
        private readonly IProductionLineService _productionLineService;
        private readonly IRealtimeService _realtimeService;

        // ======== 设备树相关 ========
        private ObservableCollection<ProductionLineTreeNode> _productionLineTreeNodes = new();
        private int _selectedDeviceId;

        // ======== 设备数据相关 ========
        private string _selectedDeviceName = string.Empty;
        private string _deviceStatus = string.Empty;
        private bool _isDeviceSelected;
        private ObservableCollection<DataPointDisplayModel> _dataPoints = new();

        // ======== 趋势图相关 ========
        private const int MaxTrendPoints = 30;
        private readonly Dictionary<int, ObservableCollection<double>> _trendDataCollections = new();
        private int _selectedTrendPointId = -1;
        private string _selectedTrendPoint = string.Empty;
        private string[] _trendPointNames = Array.Empty<string>();
        private ISeries[] _trendSeries;
        private Axis[] _trendYAxes;

        // ======== 定时器 ========
        private DispatcherTimer? _timer;

        // ======== 命令 ========
        public DelegateCommand<object?> SelectedItemChangedCommand { get; }

        // ======== 构造函数 ========
        public RealtimeViewModel(IProductionLineService lineService, IRealtimeService realtimeService)
        {
            _productionLineService = lineService;
            _realtimeService = realtimeService;
            SelectedItemChangedCommand = new DelegateCommand<object?>(OnSelectedItemChanged);

            // 初始化趋势图轴配置（X 轴固定、Y 轴默认自动范围）
            TrendYAxes = new Axis[]
            {
                 new Axis{Name = "数值",MinLimit = null,MaxLimit = null,NameTextSize = 12}
            };
            TrendXAxes = new Axis[]
            {
                new Axis{Name = "时间/秒",NameTextSize = 12,Labels = null}
            };
        }

        // ======== 数据加载方法 ========

        /// <summary>并发加载产品线列表和所有设备数据，组装为设备树</summary>
        private async Task LoadTreeAsync()
        {
            // 并发请求：产品线 + 所有设备数据
            var linesTask = _productionLineService.GetAllAsync();
            var allDeviceDataTask = _realtimeService.GetAllDataAsync();
            await Task.WhenAll(linesTask, allDeviceDataTask);

            var lines = await linesTask;
            var allDeviceData = await allDeviceDataTask;

            if (lines == null || lines.Count == 0 || allDeviceData == null)
                return;

            // 按 ProductLineId 分组组装树形结构
            ProductionLineTreeNodes = new ObservableCollection<ProductionLineTreeNode>(
                lines.Select(line => new ProductionLineTreeNode
                {
                    Id = line.Id,
                    Name = line.Name,
                    Devices = allDeviceData.Devices
                    .Where(d => d.ProductLineId == line.Id)
                    .Select(d => new DeviceTreeNode
                    {
                        Id = d.DeviceId,
                        Name = d.DeviceName,
                        Status = d.Status
                    }).ToList()
                }));
        }

        /// <summary>
        /// 加载选中设备的实时数据
        /// </summary>
        /// <param name="initializeTrend">首次加载需初始化趋势图配置</param>
        private async Task LoadDeviceDataAsync(bool initializeTrend = false)
        {
            // 调用服务端实时数据 API
            var device = await _realtimeService.GetDeviceDataAsync(_selectedDeviceId);
            if (device == null)
                return;

            SelectedDeviceName = device.DeviceName;
            DeviceStatus = device.Status;

            // 首次选中设备时初始化趋势图（只执行一次）
            if (initializeTrend)
            {
                InitializeTrendChart(device);
            }

            UpdateDataPoints(device);       // 刷新卡片数值面板
            UpdateTrendCollections(device); // 追加趋势图数据点

            EnsureTimerStarted(); // 保证 2 秒轮询定时器运行
        }

        /// <summary>首次选中设备时：初始化趋势图测点列表、数据集合、默认选中的第一个测点</summary>
        private void InitializeTrendChart(RealtimeDeviceDataDto device)
        {
            // 测点名称数组（ComboBox 数据源，避免绑定对象）
            TrendPointNames = device.DataPoints.Select(p => p.Name).ToArray();

            // 默认选中第一个测点（直接设字段避免触发 setter 中依赖 DataPoints 的逻辑）
            _selectedTrendPoint = TrendPointNames[0];
            RaisePropertyChanged(nameof(SelectedTrendPoint));
            _selectedTrendPointId = device.DataPoints[0].DataPointId;


            // 为每个测点创建独立的数据集合，并装入初始值
            foreach (var point in device.DataPoints)
            {
                _trendDataCollections[point.DataPointId] = new ObservableCollection<double> { point.Value };
            }

            UpdateTrendSeries(); // 绑定 Series → 第一个测点的数据集合
            UpdateTrendYAxis();  // 根据第一个测点的单位/报警范围设置 Y 轴
        }

        /// <summary>更新卡片数值面板：清空后重新添加（保持集合引用不变，避免 ComboBox 绑定丢失）</summary>
        private void UpdateDataPoints(RealtimeDeviceDataDto device)
        {
            DataPoints.Clear();
            foreach (var p in device.DataPoints)
            {
                DataPoints.Add(new DataPointDisplayModel
                {
                    DataPointId = p.DataPointId,
                    Name = p.Name,
                    Value = p.Value,
                    Unit = p.Unit,
                    DecimalPlaces = p.DecimalPlaces,
                    AlarmStatus = p.AlarmStatus,
                    AlarmHigh = p.AlarmHigh,
                    AlarmLow = p.AlarmLow
                });
            }
        }

        /// <summary>为所有测点追加新数值（滑动窗口：超出 MaxTrendPoints 时移除最早的数据点）</summary>
        private void UpdateTrendCollections(RealtimeDeviceDataDto device)
        {
            foreach (var p in device.DataPoints)
            {
                var collection = _trendDataCollections[p.DataPointId];
                collection.Add(p.Value);

                // 滑动窗口：保持最多 30 个点（约 60 秒历史）
                if (collection.Count > MaxTrendPoints)
                {
                    collection.RemoveAt(0);
                }
            }
        }

        // ======== 趋势图方法 ========

        /// <summary>切换测点时重建 Series，绑定到对应测点的 ObservableCollection（不清空历史数据）</summary>
        private void UpdateTrendSeries()
        {
            if (_selectedTrendPointId == -1)
                return;

            var values = _trendDataCollections[_selectedTrendPointId];

            TrendSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = values,
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 2 },
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null,
                }
            };
        }

        /// <summary>切换测点时根据单位/报警范围动态调整 Y 轴名称和范围</summary>
        private void UpdateTrendYAxis()
        {
            if (_selectedTrendPointId == -1)
                return;

            var pointInfo = DataPoints.FirstOrDefault(p => p.DataPointId == _selectedTrendPointId);
            if (pointInfo == null)
                return;

            string unit = pointInfo.Unit ?? "数值";
            double? minLimit = null;
            double? maxLimit = null;

            // 有报警阈值时，Y 轴范围设为报警范围的 ±20%（便于观察）
            if (pointInfo.AlarmHigh.HasValue && pointInfo.AlarmLow.HasValue)
            {
                minLimit = pointInfo.AlarmLow.Value * 0.8;
                maxLimit = pointInfo.AlarmHigh.Value * 1.2;
            }

            TrendYAxes = new Axis[]
            {
                new Axis
                {
                    Name = unit,
                    MinLimit = minLimit,
                    MaxLimit = maxLimit,
                    NameTextSize = 12
                }
            };
        }

        // ======== 事件处理方法 ========

        /// <summary>TreeView 选中设备时：加载设备数据并初始化趋势图</summary>
        private void OnSelectedItemChanged(object? selectedItem)
        {
            if (selectedItem is DeviceTreeNode device)
            {
                IsDeviceSelected = true;
                _selectedDeviceId = device.Id;

                // 切换设备 → 清空趋势图所有数据
                _trendDataCollections.Clear();
                TrendPointNames = Array.Empty<string>();
                SelectedTrendPoint = string.Empty;
                _selectedTrendPointId = -1;

                _ = LoadDeviceDataAsync(initializeTrend: true); // 首次加载需初始化
            }
        }

        // ======== INavigationAware（Prism 导航生命周期）========

        /// <summary>返回 true：重用当前视图实例，保持设备树状态</summary>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>离开页面时停止定时器</summary>
        public void OnNavigatedFrom(NavigationContext navigationContext) => StopTimer();

        /// <summary>进入页面时：首次加载设备树 + 启动定时器</summary>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 设备树只加载一次（后面导航回来不再重复加载）
            if (ProductionLineTreeNodes.Count == 0)
            {
                _ = LoadTreeAsync();
            }
            EnsureTimerStarted();
        }

        /// <summary>定时器回调：每 2 秒刷新设备数据和趋势图（不重新初始化）</summary>
        private async void OnRefreshTick(object? sender, EventArgs e)
        {
            await LoadDeviceDataAsync(initializeTrend: false);
        }

        // ======== 定时器方法 ========

        /// <summary>启动 2 秒轮询定时器（已有则不再新建）</summary>
        private void EnsureTimerStarted()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _timer.Tick += OnRefreshTick;
                _timer.Start();
            }
        }

        /// <summary>停止并释放定时器</summary>
        private void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
        }

        // ======== 属性 ========

        public ObservableCollection<ProductionLineTreeNode> ProductionLineTreeNodes
        {
            get => _productionLineTreeNodes;
            set => SetProperty(ref _productionLineTreeNodes, value);
        }

        public ObservableCollection<DataPointDisplayModel> DataPoints
        {
            get => _dataPoints;
            set => SetProperty(ref _dataPoints, value);
        }

        public string SelectedDeviceName
        {
            get => _selectedDeviceName;
            set => SetProperty(ref _selectedDeviceName, value);
        }

        public string DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }

        // 反向属性：通过 !IsDeviceSelected 计算，不需要自定义转换器
        public bool IsDeviceNotSelected => !IsDeviceSelected;

        /// <summary>是否选中了设备，选中时显示数值面板和趋势图，否则显示占位文本</summary>
        public bool IsDeviceSelected
        {
            get => _isDeviceSelected;
            set
            {
                if (SetProperty(ref _isDeviceSelected, value))
                {
                    RaisePropertyChanged(nameof(IsDeviceNotSelected));
                }
            }
        }

        public string[] TrendPointNames
        {
            get => _trendPointNames;
            set => SetProperty(ref _trendPointNames, value);
        }

        /// <summary>
        /// 当前选中的测点名称（ComboBox 绑定）
        /// 切换时同步更新 _selectedTrendPointId、Series 数据源、Y 轴范围
        /// </summary>
        public string SelectedTrendPoint
        {
            get => _selectedTrendPoint;
            set
            {
                if (_selectedTrendPoint != value)
                {
                    _selectedTrendPoint = value;
                    RaisePropertyChanged();

                    // 根据测点名称查找 ID，切换 Series 和 Y 轴
                    var pointInfo = DataPoints.FirstOrDefault(p => p.Name == value);
                    if (pointInfo != null)
                    {
                        _selectedTrendPointId = pointInfo.DataPointId;
                        UpdateTrendSeries(); // 切换 Series 绑定的数据集合
                        UpdateTrendYAxis();  // 调整 Y 轴单位/范围
                    }
                }
            }
        }

        public ISeries[] TrendSeries
        {
            get => _trendSeries;
            set => SetProperty(ref _trendSeries, value);
        }

        public Axis[] TrendXAxes { get; set; }

        public Axis[] TrendYAxes
        {
            get => _trendYAxes;
            set => SetProperty(ref _trendYAxes, value);
        }
    }
}