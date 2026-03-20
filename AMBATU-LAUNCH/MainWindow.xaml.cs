using AMBATU_LAUNCH.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Collections.Specialized;

namespace AMBATU_LAUNCH
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "AMBATU LAUNCH";

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon(System.IO.Path.Combine(System.AppContext.BaseDirectory, "app.ico"));

            // Make the title bar blend with the Mica backdrop
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.BackgroundColor = Colors.Transparent;
            titleBar.InactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ForegroundColor = Colors.White;
            titleBar.ButtonForegroundColor = Colors.White;

            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

            App.Categories.CollectionChanged += Categories_CollectionChanged;
        }

        private async void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            await App.EnsureAppsLoadedAsync();
            BuildNavigationItems();
            
            // NavView doesn't load any page by default, so load home page.
            if (NavView.MenuItems.Count > 0)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
            }
            NavView_Navigate(typeof(HomePage), new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo(), "Home");
        }

        private void Categories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            BuildNavigationItems();
        }

        private void BuildNavigationItems()
        {
            NavView.MenuItems.Clear();
            foreach (var category in App.Categories)
            {
                IconElement icon;
                if (System.IO.File.Exists(category.Icon))
                {
                    icon = new BitmapIcon { UriSource = new Uri(category.Icon), ShowAsMonochrome = false };
                }
                else
                {
                    Symbol iconSymbol;
                    if (!Enum.TryParse<Symbol>(category.Icon, out iconSymbol))
                    {
                        iconSymbol = category.Name == "Home" ? Symbol.Home : Symbol.Folder;
                    }
                    icon = new SymbolIcon(iconSymbol);
                }

                var navItem = new NavigationViewItem
                {
                    Content = category.Name,
                    Icon = icon,
                    Tag = category.Name
                };

                if (category.Name != "Home")
                {
                    var flyout = new MenuFlyout();

                    var editMenuItem = new MenuFlyoutItem { Text = "Edit Tab" };
                    editMenuItem.Click += async (s, e) => { await EditCategoryAsync(category); };
                    flyout.Items.Add(editMenuItem);

                    var removeMenuItem = new MenuFlyoutItem { Text = "Remove Tab", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) };
                    removeMenuItem.Click += async (s, e) =>
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Remove Category",
                            Content = $"Are you sure you want to remove the '{category.Name}' tab? Applications inside will be moved to the 'Home' tab.",
                            PrimaryButtonText = "Remove",
                            CloseButtonText = "Cancel",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = this.Content.XamlRoot
                        };

                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            // Move apps to home
                            bool appsModified = false;
                            foreach (var app in App.Apps.Where(a => a.Category == category.Name).ToList())
                            {
                                app.Category = "Home";
                                appsModified = true;
                            }

                            if (appsModified)
                            {
                                await App.SaveAppsAsync();
                            }

                            App.Categories.Remove(category);
                            await App.SaveCategoriesAsync();
                        }
                    };
                    flyout.Items.Add(removeMenuItem);
                    navItem.ContextFlyout = flyout;
                }

                NavView.MenuItems.Add(navItem);
            }
        }

        private async System.Threading.Tasks.Task EditCategoryAsync(AMBATU_LAUNCH.Models.CategoryItem category)
        {
            var oldName = category.Name;

            var stackPanel = new StackPanel { Spacing = 12 };

            var nameTextBox = new TextBox
            {
                Header = "Name",
                Text = category.Name
            };

            var iconComboBox = new ComboBox
            {
                Header = "Icon",
                ItemsSource = Enum.GetNames(typeof(Symbol)).OrderBy(s => s).ToList(),
                SelectedItem = category.Icon,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var browseButton = new Button
            {
                Content = "Browse for custom icon...",
                Margin = new Thickness(0, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var customIconPathText = new TextBlock
            {
                Text = System.IO.File.Exists(category.Icon) ? category.Icon : "No custom icon selected",
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            };

            browseButton.Click += async (s, e) =>
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".ico");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    customIconPathText.Text = file.Path;
                    iconComboBox.SelectedItem = null; // Clear symbol selection if a file is picked
                }
            };

            stackPanel.Children.Add(nameTextBox);
            stackPanel.Children.Add(iconComboBox);
            stackPanel.Children.Add(browseButton);
            stackPanel.Children.Add(customIconPathText);

            var dialog = new ContentDialog
            {
                Title = "Edit Category",
                Content = stackPanel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var newName = nameTextBox.Text.Trim();
                var newIcon = !string.IsNullOrEmpty(customIconPathText.Text) && System.IO.File.Exists(customIconPathText.Text)
                    ? customIconPathText.Text
                    : (iconComboBox.SelectedItem as string ?? "Folder");

                if (string.IsNullOrWhiteSpace(newName)) return;

                bool nameChanged = oldName != newName;
                bool iconChanged = category.Icon != newIcon;

                if (nameChanged && App.Categories.Any(c => c.Name == newName && c != category))
                {
                    // Can't rename to an existing category
                    return;
                }

                if (nameChanged || iconChanged)
                {
                    category.Name = newName;
                    category.Icon = newIcon;

                    if (nameChanged)
                    {
                        bool appsModified = false;
                        foreach (var app in App.Apps.Where(a => a.Category == oldName).ToList())
                        {
                            app.Category = newName;
                            appsModified = true;
                        }

                        if (appsModified)
                        {
                            await App.SaveAppsAsync();
                        }
                    }

                    await App.SaveCategoriesAsync();
                    
                    BuildNavigationItems();

                    if (ContentFrame.CurrentSourcePageType == typeof(HomePage) && Equals(_lastParam, oldName))
                    {
                        var transitionInfo = new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo();
                        _lastParam = null;
                        NavView_Navigate(typeof(HomePage), transitionInfo, newName);
                    }
                }
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavView_Navigate(typeof(SettingsPage), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag?.ToString() ?? "Home";
                NavView_Navigate(typeof(HomePage), args.RecommendedNavigationTransitionInfo, navItemTag);
            }
        }

        private async void AddTabButton_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var textBox = new TextBox
            {
                PlaceholderText = "Enter category name..."
            };

            var dialog = new ContentDialog
            {
                Title = "Add New Category",
                Content = textBox,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var categoryName = textBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(categoryName) && !App.Categories.Any(c => c.Name == categoryName))
                {
                    App.Categories.Add(new AMBATU_LAUNCH.Models.CategoryItem { Name = categoryName, Icon = "Folder" });
                    await App.SaveCategoriesAsync();
                }
            }
        }

        private object? _lastParam;

        private void NavView_Navigate(Type navPageType, Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo, object param = null)
        {
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded, OR if navigating to a different category parameter
            if (navPageType is not null && (!Type.Equals(preNavPageType, navPageType) || !Equals(_lastParam, param)))
            {
                _lastParam = param;
                ContentFrame.Navigate(navPageType, param, transitionInfo);
            }
        }
    }
}
