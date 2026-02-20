using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System;
using WinRT.Interop;

namespace AMBATU_LAUNCH.Views
{
    public sealed partial class HomePage : Page
    {
        public ObservableCollection<AppItem> Apps { get; set; } = new ObservableCollection<AppItem>();

        public HomePage()
        {
            this.InitializeComponent();
            AppGrid.ItemsSource = Apps;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Mock Data
            // Mock Data
            // if (Apps.Count == 0)
            // {
            //    Apps.Add(new AppItem { Name = "Notepad", IconPath = "ms-appx:///Assets/StoreLogo.png" });
            // }
        }
        private async void AddApp_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".lnk");

            var window = App.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var icon = await AMBATU_LAUNCH.Helpers.IconHelper.GetIconFromPath(file.Path);
                Apps.Add(new AppItem 
                { 
                    Name = file.DisplayName, 
                    Icon = icon,
                    ExecutablePath = file.Path 
                });
            }
        }
    }

    public class AppItem
    {
        public string Name { get; set; }
        public Microsoft.UI.Xaml.Media.ImageSource Icon { get; set; }
        public string ExecutablePath { get; set; }
    }
}
