using System;
using System.IO;
using System.Windows;

namespace JournalApp.Services
{
    public enum Theme
    {
        Light,
        Dark
    }

    public class ThemeService
    {
        private const string ThemeKey = "CurrentTheme";
        private Theme _currentTheme = Theme.Light;

        public Theme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                _currentTheme = value;
                ApplyTheme(value);
            }
        }

        public ThemeService()
        {
            LoadTheme();
        }

        public void ToggleTheme()
        {
            CurrentTheme = CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light;
            SaveTheme();
        }

        public void SetTheme(Theme theme)
        {
            CurrentTheme = theme;
            SaveTheme();
        }

        private void ApplyTheme(Theme theme)
        {
            var app = Application.Current;
            if (app == null) return;

            // Remove existing theme dictionaries
            var resourcesToRemove = new System.Collections.Generic.List<ResourceDictionary>();
            foreach (ResourceDictionary dict in app.Resources.MergedDictionaries)
            {
                if (dict.Source != null && (dict.Source.ToString().Contains("LightTheme.xaml") || 
                    dict.Source.ToString().Contains("DarkTheme.xaml")))
                {
                    resourcesToRemove.Add(dict);
                }
            }

            foreach (var dict in resourcesToRemove)
            {
                app.Resources.MergedDictionaries.Remove(dict);
            }

            // Add new theme dictionary
            var themeUri = theme == Theme.Light
                ? new Uri("Themes/LightTheme.xaml", UriKind.Relative)
                : new Uri("Themes/DarkTheme.xaml", UriKind.Relative);

            var themeDict = new ResourceDictionary { Source = themeUri };
            app.Resources.MergedDictionaries.Add(themeDict);
        }

        private void SaveTheme()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "JournalApp",
                    "settings.txt");

                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                File.WriteAllText(settingsPath, CurrentTheme.ToString());
            }
            catch
            {
                // Ignore errors saving theme preference
            }
        }

        private void LoadTheme()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "JournalApp",
                    "settings.txt");

                if (File.Exists(settingsPath))
                {
                    var themeText = File.ReadAllText(settingsPath).Trim();
                    if (Enum.TryParse<Theme>(themeText, out var theme))
                    {
                        _currentTheme = theme;
                        ApplyTheme(theme);
                        return;
                    }
                }
            }
            catch
            {
                // Use default theme if loading fails
            }

            // Default to light theme
            ApplyTheme(Theme.Light);
        }
    }
}
