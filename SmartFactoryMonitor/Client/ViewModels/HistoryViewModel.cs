using Client.Models;
using Client.Models.Dtos;
using Client.Service;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace Client.ViewModels
{
    public class HistoryViewModel : BindableBase, INavigationAware
    {
        private readonly IProductionLineService _lineService;
        private readonly IRealtimeService _realtimeService;
        private readonly IHistoryService _historyService;

        // 设备树与测点
        private ObservableCollection<ProductionLineTreeNode> _productionLineTreeNodes = new();
        private ObservableCollection<DataPointDisplayModel> _dataPoints = new();
        private DeviceTreeNode? _selectedDevice;
        private DataPointDisplayModel? _selectedPoint;

        // 查询条件
        private DateTime _startTime;
        private DateTime _endTime;
        private string _selectedInterval = "原始";
        private bool _isLoading;

        // 趋势图
        private ISeries[] _trendSeries = Array.Empty<ISeries>();
        private Axis[] _trendXAxes;
        private Axis[] _trendYAxes;

        // 统计
        private HistoryStatisticsDto _statistics = new();

        public string[] Intervals { get; } = { "原始", "30s", "1m", "5m", "1h" };

        public DelegateCommand QueryCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand<object?> SelectedItemChangedCommand { get; }

        public HistoryViewModel(IProductionLineService lineService, IRealtimeService realtimeService, IHistoryService historyService)
        {
            _lineService = lineService;
            _realtimeService = realtimeService;
            _historyService = historyService;

            _endTime = DateTime.UtcNow;
            _startTime = _endTime.AddHours(-1);

            SelectedItemChangedCommand = new DelegateCommand<object?>(OnSelectedItemChanged);
            QueryCommand = new DelegateCommand(async () => await QueryAsync(), CanQuery);
            ExportCommand = new DelegateCommand(async () => await ExportAsync(), CanExport);

            // 默认坐标轴配置
            TrendXAxes = new Axis[]
            {
                new Axis { Name = "时间", NameTextSize = 12 }
            };
            TrendYAxes = new Axis[]
            {
                new Axis { Name = "数值", NameTextSize = 12 }
            };
        }

        /// <summary>加载产线 + 所有在线设备，构建设备树</summary>
        private async Task LoadTreeAsync()
        {
            var linesTask = _lineService.GetAllAsync();
            var allDataTask = _realtimeService.GetAllDataAsync();
            await Task.WhenAll(linesTask, allDataTask);

            var lines = await linesTask;
            RealtimeAllDataDto? allData = await allDataTask;

            if (lines == null || allData == null)
                return;

            ProductionLineTreeNodes = new ObservableCollection<ProductionLineTreeNode>(
                lines.Select(line => new ProductionLineTreeNode
                {
                    Id = line.Id,
                    Name = line.Name,
                    Devices = allData.Devices
                        .Where(d => d.ProductLineId == line.Id)
                        .Select(d => new DeviceTreeNode
                        {
                            Id = d.DeviceId,
                            Name = d.DeviceName,
                            Status = d.Status
                        }).ToList()
                }));
        }

        /// <summary>TreeView 选中设备后，加载该设备的测点列表</summary>
        private async void OnSelectedItemChanged(object? selectedItem)
        {
            if (selectedItem is not DeviceTreeNode device)
                return;

            _selectedDevice = device;
            SelectedPoint = null;

            var deviceData = await _realtimeService.GetDeviceDataAsync(device.Id);
            if (deviceData == null)
                return;

            DataPoints.Clear();
            foreach (var p in deviceData.DataPoints)
            {
                DataPoints.Add(new DataPointDisplayModel
                {
                    DataPointId = p.DataPointId,
                    Name = p.Name,
                    Unit = p.Unit,
                    AlarmHigh = p.AlarmHigh,
                    AlarmLow = p.AlarmLow
                });
            }

            QueryCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        /// <summary>查询历史数据并刷新趋势图与统计面板</summary>
        private async Task QueryAsync()
        {
            if (_selectedDevice == null || _selectedPoint == null)
                return;

            IsLoading = true;
            QueryCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();

            var interval = SelectedInterval == "原始" ? null : SelectedInterval;

            var dataTask = _historyService.GetHistoryAsync(
                _selectedDevice.Id, _selectedPoint.DataPointId, _startTime, _endTime, interval);
            var statsTask = _historyService.GetStatisticsAsync(
                _selectedDevice.Id, _selectedPoint.DataPointId, _startTime, _endTime);

            await Task.WhenAll(dataTask, statsTask);

            var data = await dataTask;
            var stats = await statsTask;

            if (data != null && data.Count > 0)
            {
                var values = new ObservableCollection<DateTimePoint>(
                    data.Select(d => new DateTimePoint(d.RecordedAt, d.Value)));

                TrendSeries = new ISeries[]
                {
                    new LineSeries<DateTimePoint>
                    {
                        Values = values,
                        Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 2 },
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null
                    }
                };

                TrendXAxes = new Axis[]
                {
                    new Axis
                    {
                        Name = "时间",
                        Labeler = value => new DateTime((long)value).ToString("HH:mm:ss"),
                        NameTextSize = 12
                    }
                };

                TrendYAxes = new Axis[]
                {
                    new Axis
                    {
                        Name = _selectedPoint.Unit ?? "数值",
                        NameTextSize = 12
                    }
                };
            }

            if (stats != null)
                Statistics = stats;

            IsLoading = false;
            QueryCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        private bool CanQuery() => _selectedDevice != null && _selectedPoint != null && !IsLoading;

        /// <summary>调用导出 API，弹出保存对话框写入文件</summary>
        private async Task ExportAsync()
        {
            if (_selectedDevice == null || _selectedPoint == null)
                return;

            var bytes = await _historyService.ExportAsync(
                _selectedDevice.Id, _selectedPoint.DataPointId, _startTime, _endTime);
            if (bytes == null)
                return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"history_{_selectedDevice.Name}_{_selectedPoint.Name}.csv",
                Filter = "CSV 文件 (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                await System.IO.File.WriteAllBytesAsync(dialog.FileName, bytes);
            }
        }

        private bool CanExport() => _selectedDevice != null && _selectedPoint != null && !IsLoading;

        // ====== INavigationAware ======
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (ProductionLineTreeNodes.Count == 0)
                _ = LoadTreeAsync();
        }

        // ====== 属性 ======
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

        public DataPointDisplayModel? SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (SetProperty(ref _selectedPoint, value))
                {
                    QueryCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public string SelectedInterval
        {
            get => _selectedInterval;
            set => SetProperty(ref _selectedInterval, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public HistoryStatisticsDto Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        public ISeries[] TrendSeries
        {
            get => _trendSeries;
            set => SetProperty(ref _trendSeries, value);
        }

        public Axis[] TrendXAxes
        {
            get => _trendXAxes;
            set => SetProperty(ref _trendXAxes, value);
        }

        public Axis[] TrendYAxes
        {
            get => _trendYAxes;
            set => SetProperty(ref _trendYAxes, value);
        }
    }
}