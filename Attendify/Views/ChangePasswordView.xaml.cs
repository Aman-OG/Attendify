using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Attendify.Views.UserControls
{
    public partial class ChangePasswordView : UserControl
    {
        private HttpClient _httpClient;
        // private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode;

        // Store visibility states as fields instead of ref parameters
        private bool _isCurrentPasswordVisible = false;
        private bool _isNewPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

        // Store textboxes for password visibility toggling
        private TextBox _currentPasswordTextBox;
        private TextBox _newPasswordTextBox;
        private TextBox _confirmPasswordTextBox;

        // DTO classes
        public class ChangePasswordDto
        {
            public string EmpCode { get; set; } = null!;
            public string CurrentPassword { get; set; } = null!;
            public string NewPassword { get; set; } = null!;
            public string ConfirmPassword { get; set; } = null!;
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        // Event for password changed successfully
        public event EventHandler PasswordChanged;
        public event EventHandler CancelClicked;

        // Constructor
        public ChangePasswordView(string empCode)
        {
            _currentEmpCode = empCode;
            InitializeComponent();
            InitializeHttpClient();

            // Set focus to current password field
            Loaded += (s, e) => TxtCurrentPassword.Focus();
        }

        private void InitializeHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = Attendify.Services.HttpClientService.Instance;
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            BtnChangePassword.IsEnabled = !show;
            BtnCancel.IsEnabled = !show;
        }

        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (!ValidateInputs())
                    return;

                ShowLoading(true);

                var changePasswordDto = new ChangePasswordDto
                {
                    EmpCode = _currentEmpCode,
                    CurrentPassword = TxtCurrentPassword.Password,
                    NewPassword = TxtNewPassword.Password,
                    ConfirmPassword = TxtConfirmPassword.Password
                };

                var json = JsonSerializer.Serialize(changePasswordDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/account/change-password", content);
                var responseString = await response.Content.ReadAsStringAsync();

                // Check if response is JSON
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Check if response starts with valid JSON
                    if (string.IsNullOrWhiteSpace(responseString))
                    {
                        throw new JsonException("Empty response");
                    }

                    // Trim to remove any whitespace
                    responseString = responseString.Trim();

                    if (responseString.StartsWith("{") || responseString.StartsWith("["))
                    {
                        // It's JSON
                        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(responseString, options);

                        if (response.IsSuccessStatusCode)
                        {
                            if (apiResponse?.Success == true)
                            {
                                 

                                // Clear fields
                                TxtCurrentPassword.Clear();
                                TxtNewPassword.Clear();
                                TxtConfirmPassword.Clear();

                                // Raise event
                                PasswordChanged?.Invoke(this, EventArgs.Empty);
                            }
                            else
                            {
                                MessageBox.Show(apiResponse?.Message ?? "Password change failed", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show(apiResponse?.Message ?? $"Failed to change password. Status: {response.StatusCode}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        // It's not JSON (probably HTML error page)
                        HandleNonJsonResponse(response, responseString);
                    }
                }
                catch (JsonException jsonEx)
                {
                    // Handle JSON parsing errors
                    HandleNonJsonResponse(response, responseString);
                }
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"Network error: {httpEx.Message}\n\nPlease check if the API server is running at {Attendify.Services.HttpClientService.ApiBaseUrl}",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Password Change Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            finally
            {
                ShowLoading(false);
            }

        }

        private void HandleNonJsonResponse(HttpResponseMessage response, string responseContent)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                MessageBox.Show("Server error occurred. Please check server logs.\n\n" +
                              $"Status Code: {response.StatusCode}\n" +
                              $"Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}...",
                              "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show($"Unexpected response from server.\n\n" +
                               $"Status: {response.StatusCode}\n" +
                               $"Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}...",
                               "Unexpected Response", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            // Check current password
            if (string.IsNullOrWhiteSpace(TxtCurrentPassword.Password))
            {
                MessageBox.Show("Please enter your current password", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCurrentPassword.Focus();
                return false;
            }

            // Check new password
            if (string.IsNullOrWhiteSpace(TxtNewPassword.Password))
            {
                MessageBox.Show("Please enter a new password", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNewPassword.Focus();
                return false;
            }

            // Check password length
            if (TxtNewPassword.Password.Length < 6)
            {
                MessageBox.Show("New password must be at least 6 characters long", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNewPassword.Focus();
                return false;
            }

            // Check if passwords match
            if (TxtNewPassword.Password != TxtConfirmPassword.Password)
            {
                MessageBox.Show("New password and confirmation do not match", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtConfirmPassword.Focus();
                return false;
            }

            // Check if new password is same as current
            if (TxtNewPassword.Password == TxtCurrentPassword.Password)
            {
                MessageBox.Show("New password must be different from current password", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNewPassword.Focus();
                return false;
            }

            return true;
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                switch (tag)
                {
                    case "Current":
                        TogglePasswordVisibility(TxtCurrentPassword, BtnToggleCurrentPassword, ref _isCurrentPasswordVisible);
                        break;
                    case "New":
                        TogglePasswordVisibility(TxtNewPassword, BtnToggleNewPassword, ref _isNewPasswordVisible);
                        break;
                    case "Confirm":
                        TogglePasswordVisibility(TxtConfirmPassword, BtnToggleConfirmPassword, ref _isConfirmPasswordVisible);
                        break;
                }
            }
        }

        private void TogglePasswordVisibility(PasswordBox passwordBox, Button toggleButton, ref bool isVisible)
        {
            if (isVisible)
            {
                // Switch back to password mode
                SwitchBackToPasswordBox(passwordBox, toggleButton);
                isVisible = false;
            }
            else
            {
                // Create a TextBox to show the password
                var textBox = new TextBox
                {
                    Text = passwordBox.Password,
                    FontSize = passwordBox.FontSize,
                    Foreground = passwordBox.Foreground,
                    Background = passwordBox.Background,
                    BorderBrush = passwordBox.BorderBrush,
                    BorderThickness = passwordBox.BorderThickness,
                    Padding = passwordBox.Padding,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Visible
                };

                // Hide password box
                passwordBox.Visibility = Visibility.Collapsed;

                // Replace in parent grid
                var parent = passwordBox.Parent as Grid;
                if (parent != null)
                {
                    // Find index of password box
                    var index = -1;
                    for (int i = 0; i < parent.Children.Count; i++)
                    {
                        if (parent.Children[i] == passwordBox)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index >= 0)
                    {
                        parent.Children.Remove(passwordBox);
                        parent.Children.Insert(index, textBox);

                        toggleButton.Content = "🙈";

                        // Store reference based on which password box this is
                        if (passwordBox == TxtCurrentPassword)
                            _currentPasswordTextBox = textBox;
                        else if (passwordBox == TxtNewPassword)
                            _newPasswordTextBox = textBox;
                        else if (passwordBox == TxtConfirmPassword)
                            _confirmPasswordTextBox = textBox;

                        // Focus on the textbox
                        textBox.Focus();
                        textBox.CaretIndex = textBox.Text.Length;

                        // Handle text changes to update password box
                        textBox.TextChanged += (s, e) =>
                        {
                            passwordBox.Password = textBox.Text;
                        };

                        // Handle lost focus to switch back
                        textBox.LostFocus += TextBox_LostFocus;

                        // Handle Enter key to switch back
                        textBox.KeyDown += TextBox_KeyDown;
                    }
                }

                isVisible = true;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Determine which textbox this is
                if (textBox == _currentPasswordTextBox)
                    SwitchBackToPasswordBox(TxtCurrentPassword, BtnToggleCurrentPassword);
                else if (textBox == _newPasswordTextBox)
                    SwitchBackToPasswordBox(TxtNewPassword, BtnToggleNewPassword);
                else if (textBox == _confirmPasswordTextBox)
                    SwitchBackToPasswordBox(TxtConfirmPassword, BtnToggleConfirmPassword);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                // Determine which textbox this is
                if (textBox == _currentPasswordTextBox)
                    SwitchBackToPasswordBox(TxtCurrentPassword, BtnToggleCurrentPassword);
                else if (textBox == _newPasswordTextBox)
                    SwitchBackToPasswordBox(TxtNewPassword, BtnToggleNewPassword);
                else if (textBox == _confirmPasswordTextBox)
                    SwitchBackToPasswordBox(TxtConfirmPassword, BtnToggleConfirmPassword);
            }
        }

        private void SwitchBackToPasswordBox(PasswordBox passwordBox, Button toggleButton)
        {
            var parent = passwordBox.Parent as Grid;
            if (parent != null)
            {
                // Find and remove the textbox
                TextBox textBoxToRemove = null;

                if (passwordBox == TxtCurrentPassword && _currentPasswordTextBox != null)
                {
                    textBoxToRemove = _currentPasswordTextBox;
                    _currentPasswordTextBox = null;
                    _isCurrentPasswordVisible = false;
                }
                else if (passwordBox == TxtNewPassword && _newPasswordTextBox != null)
                {
                    textBoxToRemove = _newPasswordTextBox;
                    _newPasswordTextBox = null;
                    _isNewPasswordVisible = false;
                }
                else if (passwordBox == TxtConfirmPassword && _confirmPasswordTextBox != null)
                {
                    textBoxToRemove = _confirmPasswordTextBox;
                    _confirmPasswordTextBox = null;
                    _isConfirmPasswordVisible = false;
                }

                if (textBoxToRemove != null)
                {
                    parent.Children.Remove(textBoxToRemove);

                    // Make sure password box is in the grid
                    if (!parent.Children.Contains(passwordBox))
                    {
                        // Find where the textbox was
                        var index = 0;
                        for (int i = 0; i < parent.Children.Count; i++)
                        {
                            var child = parent.Children[i];
                            if (child is Button btn && btn == toggleButton)
                            {
                                index = i;
                                break;
                            }
                        }
                        parent.Children.Insert(index, passwordBox);
                    }

                    passwordBox.Visibility = Visibility.Visible;
                    toggleButton.Content = "👁️";
                    passwordBox.Focus();
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }

        // Allow parent to close this control
        public void Close()
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}