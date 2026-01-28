using System.Windows;
using System.Windows.Controls;
using JournalApp.Models;
using JournalApp.Services;
using JournalApp.ViewModels;
using JournalApp.Views;
using System.Linq;

namespace JournalApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly AuthService _authService;
        private readonly JournalService _journalService;
        private readonly AnalyticsService _analyticsService;
        private readonly ThemeService _themeService;
        private User? _currentUser;
        private JournalView? _journalView;
        private DashboardView? _dashboardView;
        private Button? _activeNavButton;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize services
            _databaseService = new DatabaseService();
            _authService = new AuthService(_databaseService);
            _journalService = new JournalService(_databaseService);
            _analyticsService = new AnalyticsService(_journalService);
            _themeService = App.GetThemeService() ?? new ThemeService();

            // Start directly with login page
            ShowLogin();
        }

        private void ShowLogin()
        {
            var loginView = new LoginView();
            loginView.LoginSuccessful += LoginView_LoginSuccessful;
            loginView.RegisterRequested += (s, e) => ShowRegister();
            ContentArea.Content = loginView;
        }

        private void ShowRegister()
        {
            var registerView = new RegisterView();
            registerView.RegistrationSuccessful += RegisterView_RegistrationSuccessful;
            registerView.LoginRequested += (s, e) => ShowLogin();
            ContentArea.Content = registerView;
        }

        private void RegisterView_RegistrationSuccessful(object? sender, bool e)
        {
            if (sender is RegisterView registerView)
            {
                var username = registerView.GetUsername();
                var password = registerView.GetPassword();
                var pin = registerView.GetPin();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    registerView.ShowError("Username and password are required.");
                    return;
                }

                // Create new user
                if (_authService.CreateUser(username, password, string.IsNullOrWhiteSpace(pin) ? null : pin))
                {
                    _currentUser = _authService.Authenticate(username, password);
                    if (_currentUser != null)
                    {
                        // Generate demo data for new user
                        GenerateDemoData();
                        MessageBox.Show("Account created successfully! Welcome to Journal App!", 
                            "Registration Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        ShowMainContent();
                    }
                    else
                    {
                        registerView.ShowError("Failed to create account.");
                    }
                }
                else
                {
                    registerView.ShowError("Failed to create account. Username may already exist.");
                }
            }
        }

        private void LoginView_LoginSuccessful(object? sender, bool e)
        {
            if (sender is LoginView loginView)
            {
                var username = loginView.GetUsername();
                var password = loginView.GetPassword();
                var pin = loginView.GetPin();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    loginView.ShowError("Username and password are required.");
                    return;
                }

                // Authenticate existing user
                _currentUser = _authService.Authenticate(username, password);
                if (_currentUser != null)
                {
                    // If PIN is set, verify it
                    if (!string.IsNullOrEmpty(_currentUser.PinHash) && !string.IsNullOrWhiteSpace(pin))
                    {
                        if (!_authService.VerifyPin(_currentUser, pin))
                        {
                            loginView.ShowError("Invalid PIN.");
                            return;
                        }
                    }
                    else if (!string.IsNullOrEmpty(_currentUser.PinHash) && string.IsNullOrWhiteSpace(pin))
                    {
                        loginView.ShowError("PIN is required.");
                        return;
                    }

                    MessageBox.Show($"Welcome back, {_currentUser.Username}!", 
                        "Login Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowMainContent();
                }
                else
                {
                    loginView.ShowError("Invalid username or password.");
                }
            }
        }

        private void GenerateDemoData()
        {
            // Check if user already has entries
            var existingEntries = _journalService.GetAllEntries();
            if (existingEntries.Any())
                return; // Don't generate if entries already exist

            // Generate 3 demo entries
            var demoEntries = new[]
            {
                new JournalEntry
                {
                    Date = DateTime.Now.AddDays(-2),
                    Content = "Had a wonderful day today! Met with old friends and had great conversations. Feeling grateful for the people in my life.",
                    Moods = new List<Mood> { new Mood { Type = MoodType.Happy, IsPrimary = true } },
                    Tags = new List<Tag> { new Tag { Name = "Friends" }, new Tag { Name = "Gratitude" } }
                },
                new JournalEntry
                {
                    Date = DateTime.Now.AddDays(-1),
                    Content = "Spent the morning reading and reflecting. Sometimes the quiet moments are the most valuable. Learning to appreciate the present.",
                    Moods = new List<Mood> { new Mood { Type = MoodType.Calm, IsPrimary = true } },
                    Tags = new List<Tag> { new Tag { Name = "Reading" }, new Tag { Name = "Reflection" } }
                },
                new JournalEntry
                {
                    Date = DateTime.Now,
                    Content = "Starting a new chapter! This journal app is going to help me track my journey and growth. Looking forward to documenting my thoughts and experiences.",
                    Moods = new List<Mood> { new Mood { Type = MoodType.Excited, IsPrimary = true } },
                    Tags = new List<Tag> { new Tag { Name = "New Beginning" }, new Tag { Name = "Growth" } }
                }
            };

            foreach (var entry in demoEntries)
            {
                _journalService.SaveEntry(entry);
            }
        }

        private void ShowMainContent()
        {
            var mainGrid = new Grid();
            // Changed from RowDefinitions to ColumnDefinitions for left sidebar
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220, GridUnitType.Pixel) }); // Sidebar width
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Content area

            // Left Sidebar Navigation
            var sidebar = new Border
            {
                Background = (System.Windows.Media.Brush)Application.Current.FindResource("NavBarBackgroundBrush"),
                BorderBrush = (System.Windows.Media.Brush)Application.Current.FindResource("BorderBrush"),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            var navPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 20, 0, 20)
            };

            // App Title in Sidebar
            var appTitle = new TextBlock
            {
                Text = "📔 Journal App",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(15, 0, 15, 30),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            navPanel.Children.Add(appTitle);

            var journalButton = new Button
            {
                Content = "📔 Journal",
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(10, 5, 10, 5),
                FontSize = 15,
                FontWeight = FontWeights.Medium,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            journalButton.Click += (s, e) => { SetActiveButton(journalButton); ShowJournal(); };
            journalButton.MouseEnter += (s, e) => { if (_activeNavButton != journalButton) journalButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255)); };
            journalButton.MouseLeave += (s, e) => { if (_activeNavButton != journalButton) journalButton.Background = System.Windows.Media.Brushes.Transparent; };

            var dashboardButton = new Button
            {
                Content = "📊 Dashboard",
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(10, 5, 10, 5),
                FontSize = 15,
                FontWeight = FontWeights.Medium,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            dashboardButton.Click += (s, e) => { SetActiveButton(dashboardButton); ShowDashboard(); };
            dashboardButton.MouseEnter += (s, e) => { if (_activeNavButton != dashboardButton) dashboardButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255)); };
            dashboardButton.MouseLeave += (s, e) => { if (_activeNavButton != dashboardButton) dashboardButton.Background = System.Windows.Media.Brushes.Transparent; };

            // Spacer to push theme and logout to bottom
            navPanel.Children.Add(new Border { Height = 1 }); // Spacer

            var themeButton = new Button
            {
                Content = _themeService.CurrentTheme == Theme.Light ? "🌙 Dark Mode" : "☀️ Light Mode",
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(10, 5, 10, 5),
                FontSize = 15,
                FontWeight = FontWeights.Medium,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            themeButton.Click += (s, e) => 
            { 
                _themeService.ToggleTheme();
                themeButton.Content = _themeService.CurrentTheme == Theme.Light ? "🌙 Dark Mode" : "☀️ Light Mode";
                MessageBox.Show($"Theme changed to {_themeService.CurrentTheme} mode successfully!", 
                    "Theme Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            themeButton.MouseEnter += (s, e) => { themeButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255)); };
            themeButton.MouseLeave += (s, e) => { themeButton.Background = System.Windows.Media.Brushes.Transparent; };

            var logoutButton = new Button
            {
                Content = "🚪 Logout",
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(10, 5, 10, 5),
                FontSize = 15,
                FontWeight = FontWeights.Medium,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            logoutButton.Click += (s, e) => 
            { 
                var result = MessageBox.Show("Are you sure you want to logout?", 
                    "Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _currentUser = null;
                    MessageBox.Show("Logged out successfully!", "Logout", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowLogin();
                }
            };
            logoutButton.MouseEnter += (s, e) => { logoutButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255)); };
            logoutButton.MouseLeave += (s, e) => { logoutButton.Background = System.Windows.Media.Brushes.Transparent; };

            navPanel.Children.Add(journalButton);
            navPanel.Children.Add(dashboardButton);
            
            // Add spacer to push bottom buttons down
            var bottomSpacer = new Border { Height = 1 };
            navPanel.Children.Add(bottomSpacer);
            
            navPanel.Children.Add(themeButton);
            navPanel.Children.Add(logoutButton);
            
            sidebar.Child = navPanel;

            Grid.SetColumn(sidebar, 0);
            mainGrid.Children.Add(sidebar);

            var contentArea = new ContentControl { Name = "MainContentArea" };
            Grid.SetColumn(contentArea, 1);
            mainGrid.Children.Add(contentArea);

            ContentArea.Content = mainGrid;

            // Set initial active button and show journal by default
            SetActiveButton(journalButton);
            ShowJournal();
        }

        private void SetActiveButton(Button button)
        {
            // Reset previous active button
            if (_activeNavButton != null)
            {
                _activeNavButton.Background = System.Windows.Media.Brushes.Transparent;
            }

            // Set new active button
            _activeNavButton = button;
            _activeNavButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 255, 255, 255));
        }

        private void ShowJournal()
        {
            if (ContentArea.Content is Grid mainGrid)
            {
                var contentArea = mainGrid.Children.OfType<ContentControl>().FirstOrDefault(c => c.Name == "MainContentArea");
                if (contentArea != null)
                {
                    if (_journalView == null)
                    {
                        var journalVm = new JournalViewModel(_journalService);
                        journalVm.EntriesChanged += (s, e) =>
                        {
                            if (_dashboardView?.DataContext is DashboardViewModel dvm)
                            {
                                dvm.Refresh();
                            }
                        };

                        _journalView = new JournalView
                        {
                            DataContext = journalVm
                        };
                    }
                    contentArea.Content = _journalView;
                }
            }
        }

        private void ShowDashboard()
        {
            if (ContentArea.Content is Grid mainGrid)
            {
                var contentArea = mainGrid.Children.OfType<ContentControl>().FirstOrDefault(c => c.Name == "MainContentArea");
                if (contentArea != null)
                {
                    if (_dashboardView == null)
                    {
                        _dashboardView = new DashboardView
                        {
                            DataContext = new DashboardViewModel(_analyticsService, _journalService)
                        };
                    }
                    else
                    {
                        // Refresh dashboard data
                        if (_dashboardView.DataContext is DashboardViewModel viewModel)
                        {
                            viewModel.Refresh();
                        }
                    }
                    contentArea.Content = _dashboardView;
                }
            }
        }
    }
}