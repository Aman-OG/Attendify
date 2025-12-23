using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using Attendify.Services;

namespace Attendify.Views
{
    public partial class ForgotPasswordPage : Window
    {
        private readonly HttpClient _httpClient;

        public ForgotPasswordPage()
        {
            InitializeComponent();
            _httpClient = HttpClientService.Instance;
        }

        private async void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string empCode = EmpCodeBox.Text.Trim();
            string newPassword = NewPasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(empCode) || string.IsNullOrEmpty(newPassword))
            {
                GlassMessageBox.Show("Please fill in all fields", "Validation Error", false, GlassMessageBox.MessageType.Error);
                return;
            }

            ResetBtn.IsEnabled = false;
            ResetBtn.Content = "Resetting...";

            try
            {
                var request = new
                {
                    Email = email,
                    EmpCode = empCode,
                    NewPassword = newPassword
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{HttpClientService.ApiBaseUrl}/auth/reset-password", content);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(resultJson);
                    bool success = result.RootElement.GetProperty("success").GetBoolean();
                    string message = result.RootElement.GetProperty("message").GetString();

                    if (success)
                    {
                        GlassMessageBox.Show(message, "Success", false, GlassMessageBox.MessageType.Success);
                        this.Close();
                    }
                    else
                    {
                        GlassMessageBox.Show(message, "Reset Failed", false, GlassMessageBox.MessageType.Error);
                    }
                }
                else
                {
                    GlassMessageBox.Show($"Server error: {response.StatusCode}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Reset Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                ResetBtn.IsEnabled = true;
                ResetBtn.Content = "Reset Password";
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
