using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

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
            if (Apps.Count == 0)
            {
                Apps.Add(new AppItem { Name = "Notepad", IconPath = "ms-appx:///Assets/StoreLogo.png" });
                Apps.Add(new AppItem { Name = "Calculator", IconPath = "ms-appx:///Assets/StoreLogo.png" });
                Apps.Add(new AppItem { Name = "Browser", IconPath = "ms-appx:///Assets/StoreLogo.png" });
                Apps.Add(new AppItem { Name = "File Explorer", IconPath = "ms-appx:///Assets/StoreLogo.png" });
            }
        }
    }

    public class AppItem
    {
        public string Name { get; set; }
        public string IconPath { get; set; }
    }
}
