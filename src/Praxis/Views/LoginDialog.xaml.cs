using Praxis.Services;
using Praxis.Models;
using System.Windows;
using System.Windows.Controls;

namespace Praxis.Views
{
    /// <summary>
    /// Interaction logic for LoginDialog.xaml
    /// Modal login dialog for Praxis authentication using local PostgreSQL.
    /// </summary>
    public partial class LoginDialog : Window
    {
        private bool _isPasswordVisible;
        private bool _isSyncing; // Prevent infinite loops between PasswordBox and TextBox
        private readonly AuthenticationService _authService;
        private readonly SessionManager _sessionManager;

        public LoginDialog()
        {
            InitializeComponent();

            // Get services from dependency injection
            _authService = App.Services.GetService(typeof(AuthenticationService)) as AuthenticationService
                ?? throw new InvalidOperationException("AuthenticationService not available");
            
            _sessionManager = App.Services.GetService(typeof(SessionManager)) as SessionManager
                ?? throw new InvalidOperationException("SessionManager not available");
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            if (sender is PasswordBox pb)
            {
                _isSyncing = true;
                PasswordTextBox.Text = pb.Password;
                _isSyncing = false;
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (sender is TextBox tb)
            {
                _isSyncing = true;
                PasswordBox.Password = tb.Text;
                _isSyncing = false;
            }
        }

        private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
            }
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = string.Empty;

            string email = EmailTextBox.Text.Trim();
            string password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;

            // Client-side validation
            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email address.");
                return;
            }

            if (!AuthenticationService.IsEmailValid(email))
            {
                ShowError("Please enter a valid email address.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter your password.");
                return;
            }

            // Disable button during login
            SignInButton.IsEnabled = false;

            // Attempt login
            try
            {
                var loginResult = await _authService.LoginAsync(email, password);

                if (!loginResult.Success)
                {
                    ShowError(loginResult.ErrorMessage ?? "Login failed. Please try again.");
                    SignInButton.IsEnabled = true;
                    return;
                }

                // Store session using authenticated user info
                var user = loginResult.User!;
                var currentSession = new CurrentSession
                {
                    User = new AppUser
                    {
                        AppUserId = user.UserProfileId,
                        Email = user.Email,
                        DisplayName = user.DisplayName,
                        IsActive = true
                    },
                    AccessiblePractices = user.AccessibleFirms.Select(f => new PracticeInfo
                    {
                        PracticeId = f.FirmId,
                        Name = f.Name,
                        TimeZone = f.TimeZoneIana
                    }).ToList(),
                    CurrentPractice = new PracticeInfo
                    {
                        PracticeId = user.CurrentFirmId,
                        Name = user.CurrentFirmName,
                        TimeZone = user.AccessibleFirms.FirstOrDefault()?.TimeZoneIana ?? "America/Chicago"
                    },
                    Role = Enum.TryParse<PracticeRole>(user.CurrentRole, out var role) ? role : PracticeRole.Staff
                };

                _sessionManager.SetSession(currentSession);

                // Login successful
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"An error occurred: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Login exception: {ex}");
                SignInButton.IsEnabled = true;
            }
        }

        private void ForgotPasswordLink_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show password reset dialog or open web portal
            MessageBox.Show("Password reset functionality coming soon.", "Praxis", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Application.Current.Shutdown();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Gets the email entered in the login form.
        /// </summary>
        public string GetEmail() => EmailTextBox.Text.Trim();

        /// <summary>
        /// Gets the password entered in the login form.
        /// </summary>
        public string GetPassword() => _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;

        /// <summary>
        /// Sets an error message to display on the form.
        /// </summary>
        public void SetError(string message) => ShowError(message);

        /// <summary>
        /// Clears any displayed error message.
        /// </summary>
        public void ClearError()
        {
            ErrorTextBlock.Text = string.Empty;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
