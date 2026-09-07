
using Client.Models;
using Client.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Client.ViewModels
{
    public class ShellViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        // 选中元素跳转到viewName对应的视图
        private MenuItemModel? _selectedMenuItem;
        public ObservableCollection<MenuItemModel> MenuItems { get; }

        public ShellViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;

            MenuItems = new ObservableCollection<MenuItemModel>
            {
                new MenuItemModel { Header = "实时监控", ViewName = "RealtimeView" },
                new MenuItemModel { Header = "设备管理", ViewName = "DeviceListView" }, // Module里的DeviceListView
                new MenuItemModel { Header = "报警中心", ViewName = "AlarmView" },
                new MenuItemModel { Header = "历史趋势", ViewName = "HistoryView" },
                new MenuItemModel { Header = "系统设置", ViewName = "SettingsView" }
            };

            // 延迟加载
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SelectedMenuItem = MenuItems[0];
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public MenuItemModel? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (SetProperty(ref _selectedMenuItem,value) && value != null)
                {
                    NavigateTo(value.ViewName); 
                }
            }
        }
        private void NavigateTo(string viewName)
        {
            _regionManager.RequestNavigate("MainRegion", viewName);
        }
    }
}
