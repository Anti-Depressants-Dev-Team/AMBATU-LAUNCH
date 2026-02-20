using AMBATU_LAUNCH.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.IO;
using WinRT.Interop;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;

namespace AMBATU_LAUNCH.Views
{
    public sealed partial class HomePage : Page
    {
        private bool m_iconSizeInitialized;
        private string m_currentCategory = "Home";

        public ObservableCollection<AppItem> DisplayApps { get; } = new ObservableCollection<AppItem>();

        public HomePage()
        {
            this.InitializeComponent();
            AppGrid.ItemsSource = DisplayApps;
            AppList.ItemsSource = DisplayApps;
            IconSizeSlider.Value = App.IconSize;
            m_iconSizeInitialized = true;
            Loaded += HomePage_Loaded;
            Unloaded += HomePage_Unloaded;

            App.Apps.CollectionChanged += Apps_CollectionChanged;
        }

        private void Apps_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            FilterApps();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string category && !string.IsNullOrWhiteSpace(category))
            {
                m_currentCategory = category;
            }
            else
            {
                m_currentCategory = "Home";
            }
            
            FilterApps();
        }

        private void FilterApps()
        {
            var searchText = SearchBox.Text?.Trim().ToLower() ?? "";

            var filtered = App.Apps.Where(app =>
            {
                // Match category precisely
                bool matchesCategory = app.Category == m_currentCategory;
                
                // Match search
                bool matchesSearch = string.IsNullOrWhiteSpace(searchText) || 
                                     app.Name.ToLower().Contains(searchText);

                return matchesCategory && matchesSearch;
            }).ToList();

            DisplayApps.Clear();
            foreach (var app in filtered)
            {
                DisplayApps.Add(app);
            }

            if (EmptyStateTextBlock != null)
            {
                EmptyStateTextBlock.Visibility = DisplayApps.Count == 0 
                    ? Microsoft.UI.Xaml.Visibility.Visible 
                    : Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterApps();
        }
        private async void AddApp_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".lnk");

            var window = App.MainWindow;
            if (window == null)
            {
                return;
            }
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    var icon = await AMBATU_LAUNCH.Helpers.IconHelper.GetIconFromPath(file.Path);
                    App.Apps.Add(new AppItem 
                    { 
                        Name = file.DisplayName, 
                        Icon = icon,
                        ExecutablePath = file.Path,
                        Category = m_currentCategory
                    });
                }
                await App.SaveAppsAsync();
            }
        }

        private void ToggleViewMode_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            App.UpdateViewMode(!App.IsListViewMode);
            UpdateViewModeUI();
        }

        private void UpdateViewModeUI()
        {
            if (App.IsListViewMode)
            {
                AppGrid.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                AppList.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }
            else
            {
                AppGrid.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                AppList.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void IconSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!m_iconSizeInitialized)
            {
                return;
            }

            App.UpdateIconSize(e.NewValue);
        }

        private void AppGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not AppItem app || string.IsNullOrWhiteSpace(app.ExecutablePath))
            {
                return;
            }

            try
            {
                var path = app.ExecutablePath;
                if (!File.Exists(path))
                {
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            catch (Exception)
            {
                // Swallow for now; consider surfacing UI feedback.
            }
        }

        private async void MoveToCategory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.Controls.MenuFlyoutItem item && item.DataContext is AppItem app)
            {
                var comboBox = new ComboBox
                {
                    ItemsSource = App.Categories,
                    SelectedItem = app.Category,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
                };

                var dialog = new ContentDialog
                {
                    Title = "Move App to Category",
                    Content = comboBox,
                    PrimaryButtonText = "Move",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && comboBox.SelectedItem is string selectedCategory)
                {
                    if (app.Category != selectedCategory)
                    {
                        app.Category = selectedCategory;
                        await App.SaveAppsAsync();
                        FilterApps();
                    }
                }
            }
        }

        private async void RenameApp_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.Controls.MenuFlyoutItem item && item.DataContext is AppItem app)
            {
                var textBox = new TextBox
                {
                    Text = app.Name,
                    PlaceholderText = "Enter new name..."
                };

                var dialog = new ContentDialog
                {
                    Title = "Rename App",
                    Content = textBox,
                    PrimaryButtonText = "Rename",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    app.Name = textBox.Text.Trim();
                    await App.SaveAppsAsync();
                    FilterApps(); // refresh UI
                }
            }
        }

        private void OpenFileLocation_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.Controls.MenuFlyoutItem item && item.DataContext is AppItem app)
            {
                if (!string.IsNullOrWhiteSpace(app.ExecutablePath))
                {
                    try
                    {
                        string directory = Path.GetDirectoryName(app.ExecutablePath) ?? "";
                        if (Directory.Exists(directory))
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = $"/select,\"{app.ExecutablePath}\"",
                                UseShellExecute = true
                            };
                            Process.Start(startInfo);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }
                }
            }
        }

        private async void RemoveApp_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.Controls.MenuFlyoutItem item && item.DataContext is AppItem app)
            {
                var dialog = new ContentDialog
                {
                    Title = "Remove App",
                    Content = $"Are you sure you want to remove '{app.Name}' from the launcher? This will not uninstall the application.",
                    PrimaryButtonText = "Remove",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    App.Apps.Remove(app);
                    await App.SaveAppsAsync();
                }
            }
        }

        private async void HomePage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            m_iconSizeInitialized = false;
            App.ReloadSettings();
            IconSizeSlider.Value = App.IconSize;
            UpdateViewModeUI();
            m_iconSizeInitialized = true;
            await App.EnsureAppsLoadedAsync();
            FilterApps();
        }

        private void HomePage_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            App.Apps.CollectionChanged -= Apps_CollectionChanged;
        }
    }
}
