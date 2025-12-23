using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Attendify.Views.Employee
{
    public partial class EmployeeShiftsView : UserControl
    {
        private HttpClient _httpClient;
        private string _currentEmpCode = "";
        private DispatcherTimer _refreshTimer;

        public EmployeeShiftsView()
        {
            InitializeComponent();
            Loaded += EmployeeShiftsView_Loaded;
        }

        public EmployeeShiftsView(string empCode) : this()
        {
            _currentEmpCode = empCode;
        }

        private void EmployeeShiftsView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeHttpClient();
            LoadShifts();

            // Auto-refresh every 60s
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _refreshTimer.Tick += (s, e) => LoadShifts();
            _refreshTimer.Start();
        }

        private void InitializeHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = Attendify.Services.HttpClientService.Instance;
            }
        }

        private async void LoadShifts()
        {
            try
            {
                LoadingText.Visibility = Visibility.Visible;
                string apiUrl = !string.IsNullOrEmpty(_currentEmpCode) 
                    ? $"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeshifts/shifts/{_currentEmpCode}"
                    : $"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeshifts/shifts";

                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var shiftsJson = apiResponse.Data.ToString();
                        var shifts = JsonSerializer.Deserialize<List<ShiftResponseDto>>(shiftsJson, options);

                        Dispatcher.Invoke(() =>
                        {
                            BindShifts(shifts);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading shifts: {ex.Message}");
                // Ideally show a "Retry" button or error message here
            }
            finally
            {
                LoadingText.Visibility = Visibility.Collapsed;
            }
        }

        private void BindShifts(List<ShiftResponseDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                // Show default/empty state if needed
                ShiftsItemsControl.ItemsSource = null;
                return;
            }

            var viewModels = dtos.Select(dto => new ShiftViewModel(dto)).ToList();
            ShiftsItemsControl.ItemsSource = viewModels;
        }

        // --- View Models & DTOs ---

        public class ShiftViewModel
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public string DisplayTime { get; set; }
            public string GracePeriodText { get; set; }
            public string DaysText { get; set; }
            public Brush StatusBackground { get; set; }
            public string StatusText { get; set; }

            public ShiftViewModel(ShiftResponseDto dto)
            {
                Name = dto.Name;
                Icon = GetShiftIcon(dto.Name);
                
                // Format Time: 24h -> 12h
                string start = FormatTime(dto.StartTime);
                string end = FormatTime(dto.EndTime);
                DisplayTime = $"{start} – {end}";

                GracePeriodText = $"{dto.GracePeriodMinutes} min";
                DaysText = GetShiftDays(dto.Name);

                // Status Logic
                if (dto.IsCurrentlyActive)
                {
                    StatusText = "🟢 CURRENTLY ACTIVE";
                    StatusBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38b000")) { Opacity = 0.8 };
                }
                else
                {
                    StatusText = "⚪ INACTIVE";
                    StatusBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")) { Opacity = 0.6 };
                }
            }

            private string FormatTime(string timeString)
            {
                 // Expected format: "14:00:00" or "8:00"
                 if (TimeSpan.TryParse(timeString, out TimeSpan ts))
                 {
                     DateTime dt = DateTime.Today.Add(ts);
                     return dt.ToString("hh:mm tt");
                 }
                 return timeString; // Fallback
            }

            private string GetShiftIcon(string shiftName)
            {
                if (string.IsNullOrEmpty(shiftName)) return "🕒";
                if (shiftName.Contains("Morning", StringComparison.OrdinalIgnoreCase)) return "🌅";
                if (shiftName.Contains("Evening", StringComparison.OrdinalIgnoreCase)) return "🌇";
                if (shiftName.Contains("Night", StringComparison.OrdinalIgnoreCase)) return "🌙";
                if (shiftName.Contains("Weekend", StringComparison.OrdinalIgnoreCase)) return "🎯";
                return "🕒";
            }

            private string GetShiftDays(string shiftName)
            {
                 if (string.IsNullOrEmpty(shiftName)) return "Mon – Fri";
                 if (shiftName.Contains("Weekend", StringComparison.OrdinalIgnoreCase) ||
                     shiftName.Contains("Sat", StringComparison.OrdinalIgnoreCase)) return "Sat – Sun";
                 if (shiftName.Contains("Flex", StringComparison.OrdinalIgnoreCase)) return "Flexible";
                 return "Mon – Fri";
            }
        }

        public class ShiftResponseDto
        {
            public int ShiftId { get; set; }
            public string Name { get; set; } = null!;
            public string StartTime { get; set; } = null!; // Expecting "HH:mm:ss"
            public string EndTime { get; set; } = null!;
            public int GracePeriodMinutes { get; set; }
            public bool IsCurrentlyActive { get; set; }
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }
    }
}