using System.Configuration;
using System.Data;
using System.Windows;
using JournalApp.Services;

namespace JournalApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ThemeService? _themeService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize theme service
            _themeService = new ThemeService();
        }

        public static ThemeService? GetThemeService()
        {
            if (Current is App app)
            {
                return app._themeService;
            }
            return null;
        }
    }

}
