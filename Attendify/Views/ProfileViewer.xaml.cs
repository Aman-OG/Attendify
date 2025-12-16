using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Attendify.Views.UserControls
{
    public partial class ProfileViewer : UserControl
    {
        private HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode;
        private DispatcherTimer _refreshTimer;

        // DTO classes nested in the code-behind
        public class EmployeeProfileDto
        {
            public int EmployeeID { get; set; }
            public string EmpCode { get; set; } = null!;
            public string FirstName { get; set; } = null!;
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? Email { get; set; }
            public string Role { get; set; } = null!;
            public string? Phone { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? LastPasswordChange { get; set; }
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public EmployeeProfileDto? Data { get; set; }
        }

        // Events
        public event EventHandler EditProfileRequested;
        public event EventHandler ChangePasswordRequested;
        public event EventHandler CloseRequested;

        // Constructor
        public ProfileViewer(string empCode)
        {
            _currentEmpCode = empCode;
            InitializeComponent();
            InitializeHttpClient();
            InitializeRefreshTimer();
            Loaded += ProfileViewer_Loaded;
        }

        private void InitializeHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
            }
        }

        private void InitializeRefreshTimer()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // Refresh every 30 seconds
            };
            _refreshTimer.Tick += async (s, e) => await LoadProfileData();
        }

        private async void ProfileViewer_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProfileData();
            _refreshTimer.Start();
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            BtnEditProfile.IsEnabled = !show;
            BtnChangePassword.IsEnabled = !show;
            BtnClose.IsEnabled = !show;
        }

        private async Task LoadProfileData()
        {
            try
            {
                ShowLoading(true);

                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/profile/{_currentEmpCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(responseJson, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        UpdateProfileUI(apiResponse.Data);
                        UpdateLastUpdatedText();
                    }
                    else
                    {
                        ShowErrorMessage(apiResponse?.Message ?? "Failed to load profile data");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(errorContent, options);

                    ShowErrorMessage(apiResponse?.Message ?? $"Failed to load profile. Status: {response.StatusCode}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                ShowErrorMessage($"Network error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading profile: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void UpdateProfileUI(EmployeeProfileDto profile)
        {
            // Update profile circle and name
            string fullName = profile.FirstName;
            if (!string.IsNullOrWhiteSpace(profile.MiddleName))
            {
                fullName += " " + profile.MiddleName;
            }
            fullName = fullName.Trim();

            ProfileName.Text = fullName;
            ProfileEmpCode.Text = profile.EmpCode;
            ProfileRole.Text = profile.Role;

            // Set profile initial
            string initial = profile.FirstName?.Length > 0
                ? profile.FirstName.Substring(0, 1).ToUpper()
                : profile.MiddleName?.Length > 0
                    ? profile.MiddleName.Substring(0, 1).ToUpper()
                    : "A";
            ProfileInitial.Text = initial;

            // Set profile circle color
            SetProfileColor(initial);

            // Update status
            UpdateStatusUI(profile.IsActive);

            // Update details
            TxtFirstName.Text = profile.FirstName;
            TxtMiddleName.Text = profile.MiddleName ?? "N/A";
            TxtLastName.Text = profile.LastName;
            TxtEmpCode.Text = profile.EmpCode;
            TxtDepartment.Text = profile.Department ?? "Not assigned";
            TxtPosition.Text = profile.Position ?? "Not assigned";
            TxtEmail.Text = profile.Email ?? "Not provided";
            TxtPhone.Text = profile.Phone ?? "Not provided";
            TxtCreatedAt.Text = profile.CreatedAt.ToString("dd MMMM yyyy");

            if (profile.LastPasswordChange.HasValue)
            {
                TxtLastPasswordChange.Text = profile.LastPasswordChange.Value.ToString("dd MMMM yyyy HH:mm");
            }
            else
            {
                TxtLastPasswordChange.Text = "Never changed";
            }
        }

        private void SetProfileColor(string initial)
        {
            // Consistent color based on initial
            var colors = (Color[])FindResource("ProfileColors");
            if (colors == null || colors.Length == 0) return;

            int hash = 0;
            if (!string.IsNullOrEmpty(initial))
            {
                foreach (char c in initial)
                {
                    hash = (hash * 31 + c) % colors.Length;
                }
            }

            int colorIndex = Math.Abs(hash) % colors.Length;
            Color selectedColor = colors[colorIndex];

            // Create gradient brush for liquid effect
            var gradientBrush = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.3, 0.3),
                Center = new Point(0.5, 0.5),
                RadiusX = 0.8,
                RadiusY = 0.8
            };

            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(150, selectedColor.R, selectedColor.G, selectedColor.B), 0));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, selectedColor.R, selectedColor.G, selectedColor.B), 0.5));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(50, selectedColor.R, selectedColor.G, selectedColor.B), 1));

            ProfileCircle.Background = gradientBrush;
        }

        private void UpdateStatusUI(bool isActive)
        {
            if (isActive)
            {
                StatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 166, 251)); // Blue
                StatusText.Text = "Active";
                ActiveIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 166, 251));
                TxtAccountStatus.Text = "Active";
            }
            else
            {
                StatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 217, 58, 58)); // Red
                StatusText.Text = "Inactive";
                ActiveIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 217, 58, 58));
                TxtAccountStatus.Text = "Inactive";
            }
        }

        private void UpdateLastUpdatedText()
        {
            LastUpdatedText.Text = $"Updated {DateTime.Now:HH:mm:ss}";
        }

        private void ShowErrorMessage(string message)
        {
            // You can show this in a status bar or message box
            LastUpdatedText.Text = $"Error: {message}";
            LastUpdatedText.Foreground = new SolidColorBrush(Color.FromArgb(255, 217, 58, 58));
        }

        private void BtnEditProfile_Click(object sender, RoutedEventArgs e)
        {
            EditProfileRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // Cleanup
        public void Cleanup()
        {
            _refreshTimer?.Stop();
            _httpClient?.Dispose();
        }
    }
}