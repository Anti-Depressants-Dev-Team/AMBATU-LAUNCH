#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AMBATU_LAUNCH.Helpers;
using AMBATU_LAUNCH.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Windows.Storage;

namespace AMBATU_LAUNCH
{
    public partial class App : Application
    {
        public static Window? MainWindow { get; private set; }
        public static ObservableCollection<AppItem> Apps { get; } = new ObservableCollection<AppItem>();
        public static ObservableCollection<string> Categories { get; } = new ObservableCollection<string> { "Home" };
        public static double IconSize { get; private set; } = DefaultIconSize;
        public static DispatcherQueue? UiDispatcher { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try 
            {
                m_window = new MainWindow();
                MainWindow = m_window;
                UiDispatcher = m_window.DispatcherQueue;
                m_window.Activate();
            }
            catch (System.Exception ex)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), ex.ToString());
                throw;
            }
        }

        private Window? m_window;

        private const string AppsFileName = "apps.json";
        private const string CategoriesFileName = "categories.json";
        private const string IconSizeKey = "IconSize";
        private const double DefaultIconSize = 100d;
        private static bool s_appsLoaded;

        public static async Task EnsureAppsLoadedAsync()
        {
            if (s_appsLoaded)
            {
                return;
            }

            s_appsLoaded = true;
            await LoadCategoriesAsync();
            await LoadAppsAsync();
        }

        public static void UpdateIconSize(double size)
        {
            IconSize = size;
            SaveIconSize(size);
        }

        public static void ReloadIconSize()
        {
            IconSize = LoadIconSize();
        }

        public static async Task SaveAppsAsync()
        {
            try
            {
                var entries = Apps
                    .Where(app => !string.IsNullOrWhiteSpace(app.ExecutablePath))
                    .Select(app => new AppEntry
                    {
                        Name = app.Name,
                        ExecutablePath = app.ExecutablePath,
                        Category = app.Category
                    })
                    .ToList();

                var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
                var filePath = GetAppsFilePath();
                await File.WriteAllTextAsync(filePath, json);
            }
            catch
            {
                // Ignore persistence failures.
            }
        }

        private static async Task LoadAppsAsync()
        {
            try
            {
                var filePath = GetAppsFilePath();
                if (!File.Exists(filePath))
                {
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var entries = JsonSerializer.Deserialize<List<AppEntry>>(json);
                if (entries == null || entries.Count == 0)
                {
                    return;
                }

                foreach (var entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.ExecutablePath) || !File.Exists(entry.ExecutablePath))
                    {
                        continue;
                    }

                    await RunOnUiThreadAsync(async () =>
                    {
                        var icon = await IconHelper.GetIconFromPath(entry.ExecutablePath);
                        var displayName = string.IsNullOrWhiteSpace(entry.Name)
                            ? Path.GetFileNameWithoutExtension(entry.ExecutablePath)
                            : entry.Name;

                        Apps.Add(new AppItem
                        {
                            Name = displayName,
                            ExecutablePath = entry.ExecutablePath,
                            Icon = icon,
                            Category = string.IsNullOrWhiteSpace(entry.Category) ? "Home" : entry.Category
                        });
                    });
                }
            }
            catch
            {
                // Ignore load failures.
            }
        }

        private static double LoadIconSize()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue(IconSizeKey, out var value) && value != null)
            {
                if (value is double d)
                {
                    return d;
                }

                if (value is float f)
                {
                    return f;
                }

                if (value is int i)
                {
                    return i;
                }

                if (double.TryParse(value.ToString(), out var parsed))
                {
                    return parsed;
                }
            }

            return DefaultIconSize;
        }

        private static void SaveIconSize(double size)
        {
            ApplicationData.Current.LocalSettings.Values[IconSizeKey] = size;
        }

        private static Task RunOnUiThreadAsync(Func<Task> action)
        {
            if (UiDispatcher == null || UiDispatcher.HasThreadAccess)
            {
                return action();
            }

            var tcs = new TaskCompletionSource<object?>();
            UiDispatcher.TryEnqueue(async () =>
            {
                try
                {
                    await action();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        private static string GetAppsFilePath()
        {
            var folderPath = ApplicationData.Current.LocalFolder.Path;
            return Path.Combine(folderPath, AppsFileName);
        }

        private static string GetCategoriesFilePath()
        {
            var folderPath = ApplicationData.Current.LocalFolder.Path;
            return Path.Combine(folderPath, CategoriesFileName);
        }

        public static async Task SaveCategoriesAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(Categories, new JsonSerializerOptions { WriteIndented = true });
                var filePath = GetCategoriesFilePath();
                await File.WriteAllTextAsync(filePath, json);
            }
            catch
            {
                // Ignore persistence failures.
            }
        }

        private static async Task LoadCategoriesAsync()
        {
            try
            {
                var filePath = GetCategoriesFilePath();
                if (!File.Exists(filePath))
                {
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var categories = JsonSerializer.Deserialize<List<string>>(json);
                if (categories != null && categories.Count > 0)
                {
                    await RunOnUiThreadAsync(() =>
                    {
                        Categories.Clear();
                        foreach (var cat in categories)
                        {
                            Categories.Add(cat);
                        }
                        return Task.CompletedTask;
                    });
                }
            }
            catch
            {
                // Ignore load failures.
            }
        }

        private sealed class AppEntry
        {
            public string Name { get; set; } = string.Empty;
            public string ExecutablePath { get; set; } = string.Empty;
            public string Category { get; set; } = "Home";
        }
    }
}
