using Attendify.Views;
using Attendify.Views.Employee;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Attendify
{
    public partial class LoginPage : Window
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:7129/api"; // Adjust based on your API URL

        // Local DTO classes since we can't reference AuthController directly
        public class LoginRequest
        {
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        public class LoginResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public EmployeeData? Employee { get; set; }
            public string? Role { get; set; }
        }

        public class EmployeeData
        {
            public int EmployeeID { get; set; }
            public string EmpCode { get; set; } = null!;
            public string FirstName { get; set; } = null!;
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!;
        }

        public LoginPage()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Placeholder visibility handlers
            UsernameBox.TextChanged += (s, e) =>
            {
                UserPlaceholder.Visibility =
                    string.IsNullOrWhiteSpace(UsernameBox.Text) ? Visibility.Visible : Visibility.Hidden;
            };

            PasswordBox.PasswordChanged += (s, e) =>
            {
                PassPlaceholder.Visibility =
                    string.IsNullOrWhiteSpace(PasswordBox.Password) ? Visibility.Visible : Visibility.Hidden;
            };
        }

        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UserPlaceholder.Visibility =
                string.IsNullOrEmpty(UsernameBox.Text) ? Visibility.Visible : Visibility.Hidden;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PassPlaceholder.Visibility =
                string.IsNullOrEmpty(PasswordBox.Password) ? Visibility.Visible : Visibility.Hidden;
        }

        private void LoginButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var anim = new DoubleAnimation
            {
                To = 1.08,
                Duration = TimeSpan.FromMilliseconds(150),
                AccelerationRatio = 0.3
            };

            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        private void LoginButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var anim = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(0.15),
                DecelerationRatio = 0.3
            };

            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string email = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            // Validation
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowErrorMessage("Please enter both email and password");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowErrorMessage("Please enter a valid email address");
                return;
            }

            // Disable login button during API call
            LoginButton.IsEnabled = false;
            var originalBackground = LoginButton.Background;
            LoginButton.Background = new SolidColorBrush(Colors.Gray);

            try
            {
                var loginResponse = await AuthenticateUserAsync(email, password);

                if (loginResponse != null && loginResponse.Success)
                {
                    // Create dashboard with employee data
                    if (loginResponse.Employee != null)
                    {
                        switch (loginResponse.Role?.ToLower())
                        {
                            case "admin":
                            case "administrator":
                            case "superadmin":
                                AdminDashboard adminDashboard = new AdminDashboard(loginResponse.Employee);
                                adminDashboard.Show();
                                this.Close();
                                break;

                            case "employee":
                            case "user":
                            case "staff":
                            case "member":
                                EmployeeDashboard employeeDashboard = new EmployeeDashboard(loginResponse.Employee);
                                employeeDashboard.Show();
                                this.Close();
                                break;

                            default:
                                ShowErrorMessage($"User role '{loginResponse.Role}' not recognized.");
                                break;
                        }
                    }
                    else
                    {
                        ShowErrorMessage("No employee data received.");
                    }
                }
                else
                {
                    ShowErrorMessage(loginResponse?.Message ?? "Login failed");
                }
            }
            catch (HttpRequestException ex)
            {
                ShowErrorMessage($"Network error: {ex.Message}. Please check your connection.");
            }
            catch (TaskCanceledException)
            {
                ShowErrorMessage("Request timeout. Please try again.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Re-enable login button
                LoginButton.IsEnabled = true;
                LoginButton.Background = originalBackground;
            }
        }

        private async Task<LoginResponse?> AuthenticateUserAsync(string email, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Email = email,
                    Password = password
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return loginResponse;
                }
                else
                {
                    // Try to read error message
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<LoginResponse>(
                            errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        return errorResponse ?? new LoginResponse
                        {
                            Success = false,
                            Message = $"Server error: {response.StatusCode}"
                        };
                    }
                    catch
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Message = $"Server error: {response.StatusCode}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Login Failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            // You can implement forgot password functionality later
            MessageBox.Show("Please contact your administrator to reset your password.",
                "Forgot Password", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Admin_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Please contact system administrator at admin@attendify.com",
                "Contact Admin", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}