using AMBATU_LAUNCH.Views;
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
                var icon = category == "Home" ? new SymbolIcon(Symbol.Home) : new SymbolIcon(Symbol.Folder);
                var navItem = new NavigationViewItem
                {
                    Content = category,
                    Icon = icon,
                    Tag = category
                };

                if (category != "Home")
                {
                    var flyout = new MenuFlyout();
                    var removeMenuItem = new MenuFlyoutItem { Text = "Remove Tab", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) };
                    removeMenuItem.Click += async (s, e) =>
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Remove Category",
                            Content = $"Are you sure you want to remove the '{category}' tab? Applications inside will be moved to the 'Home' tab.",
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
                            foreach (var app in App.Apps.Where(a => a.Category == category).ToList())
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
                if (!string.IsNullOrWhiteSpace(categoryName) && !App.Categories.Contains(categoryName))
                {
                    App.Categories.Add(categoryName);
                    await App.SaveCategoriesAsync();
                }
            }
        }

        private void NavView_Navigate(Type navPageType, Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo, object param = null)
        {
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
            {
                ContentFrame.Navigate(navPageType, param, transitionInfo);
            }
        }
    }
}
