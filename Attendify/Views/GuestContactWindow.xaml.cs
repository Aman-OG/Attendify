using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Attendify.Services;

namespace Attendify.Views
{
    public partial class GuestContactWindow : Window
    {
        private readonly HttpClient _httpClient;

        public GuestContactWindow()
        {
            InitializeComponent();
            _httpClient = HttpClientService.Instance;
        }

        private async void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageBox.Text.Trim();
            string type = (TypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Other";

            if (string.IsNullOrWhiteSpace(message))
            {
                GlassMessageBox.Show("Please enter a message.", "Validation Error", false, GlassMessageBox.MessageType.Error);
                return;
            }

            if (message.Length < 10)
            {
                GlassMessageBox.Show("Please provide more details (minimum 10 characters).", "Validation Error", false, GlassMessageBox.MessageType.Error);
                return;
            }

            SubmitBtn.IsEnabled = false;
            SubmitBtn.Content = "Sending...";

            try
            {
                var request = new
                {
                    EmpCode = "UNKNOWN",
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    Type = type,
                    Message = message
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{HttpClientService.ApiBaseUrl}/employeecontact/request", content);

                if (response.IsSuccessStatusCode)
                {
                    GlassMessageBox.Show("Your message has been sent to the administrator. They will review it soon.", "Success", false, GlassMessageBox.MessageType.Success);
                    this.Close();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Failed to send message: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Connection Error", false, GlassMessageBox.MessageType.Error);
            }
            finally
            {
                SubmitBtn.IsEnabled = true;
                SubmitBtn.Content = "Send Message";
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
