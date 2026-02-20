using AMBATU_LAUNCH.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace AMBATU_LAUNCH
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "AMBATU LAUNCH";
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // NavView doesn't load any page by default, so load home page.
            NavView.SelectedItem = NavView.MenuItems[0];
            NavView_Navigate(typeof(HomePage), new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavView_Navigate(typeof(SettingsPage), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                
                // If it's a category, we might want to filter the HomePage. 
                // For now, let's just interpret "Home" as the HomePage without filter.
                if (navItemTag == "AMBATU_LAUNCH.Views.HomePage")
                {
                     NavView_Navigate(typeof(HomePage), args.RecommendedNavigationTransitionInfo);
                }
                else
                {
                    // For categories, pass the tag to HomePage to filter? 
                    // Or just navigate to HomePage with parameter.
                     NavView_Navigate(typeof(HomePage), args.RecommendedNavigationTransitionInfo, navItemTag);
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
