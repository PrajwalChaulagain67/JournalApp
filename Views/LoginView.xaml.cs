using System.Windows;
using System.Windows.Controls;

namespace JournalApp.Views
{
    public partial class LoginView : UserControl
    {
        public event EventHandler<bool>? LoginSuccessful;
        public event EventHandler? RegisterRequested;

        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            var password = PasswordBox.Password;
            var pin = PinBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Username and password are required.");
                return;
            }

            // This will be handled by the parent window
            LoginSuccessful?.Invoke(this, true);
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterRequested?.Invoke(this, EventArgs.Empty);
        }

        public void ShowError(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        public void ClearError()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        public string GetUsername() => UsernameTextBox.Text;
        public string GetPassword() => PasswordBox.Password;
        public string GetPin() => PinBox.Password;
    }
}
