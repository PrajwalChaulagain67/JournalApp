using System.Windows;
using System.Windows.Controls;

namespace JournalApp.Views
{
    public partial class RegisterView : UserControl
    {
        public event EventHandler<bool>? RegistrationSuccessful;
        public event EventHandler? LoginRequested;

        public RegisterView()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            var password = PasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;
            var pin = PinBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Username and password are required.");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Passwords do not match.");
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Password must be at least 4 characters long.");
                return;
            }

            // This will be handled by the parent window
            RegistrationSuccessful?.Invoke(this, true);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginRequested?.Invoke(this, EventArgs.Empty);
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
