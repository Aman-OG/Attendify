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
        // private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode = "";
        private DispatcherTimer _refreshTimer;

        // DTO classes
        public class ShiftResponseDto
        {
            public int ShiftId { get; set; }
            public string Name { get; set; } = null!;
            public string StartTime { get; set; } = null!;
            public string EndTime { get; set; } = null!;
            public int GracePeriodMinutes { get; set; }
            public bool IsCurrentlyActive { get; set; }
            public string DisplayTime { get; set; } = null!;
            public string StatusColor { get; set; } = "#38b000";
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        public EmployeeShiftsView()
        {
            InitializeComponent();
            Loaded += EmployeeShiftsView_Loaded;
        }

        // Constructor with empCode parameter
        public EmployeeShiftsView(string empCode) : this()
        {
            _currentEmpCode = empCode;
        }

        private void EmployeeShiftsView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeHttpClient();
            LoadShifts();

            // Set up auto-refresh timer (every 60 seconds)
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
                string apiUrl;
                if (!string.IsNullOrEmpty(_currentEmpCode))
                {
                    apiUrl = $"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeshifts/shifts/{_currentEmpCode}";
                }
                else
                {
                    apiUrl = $"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeshifts/shifts";
                }

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
                            UpdateShiftsDisplay(shifts ?? new List<ShiftResponseDto>());
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading shifts: {ex.Message}");
                // Keep displaying the default shifts if API fails
            }
        }

        private void UpdateShiftsDisplay(List<ShiftResponseDto> shifts)
        {
            // Clear existing shift cards
            AssignedShiftsPanel.Children.Clear();

            if (shifts == null || !shifts.Any())
            {
                // Show default shifts if no data
                ShowDefaultShifts();
                return;
            }

            // Create shift cards dynamically
            foreach (var shift in shifts)
            {
                var shiftCard = CreateShiftCard(shift);
                AssignedShiftsPanel.Children.Add(shiftCard);
            }
        }

        private ContentControl CreateShiftCard(ShiftResponseDto shift)
        {
            var card = new ContentControl
            {
                Style = (Style)FindResource("ShiftCardStyle"),
                Margin = new Thickness(10)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            // Shift header with icon
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var icon = GetShiftIcon(shift.Name);
            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#00A6FB"),
                Margin = new Thickness(0, 0, 8, 0)
            };

            var nameText = new TextBlock
            {
                Text = shift.Name,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#00A6FB"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerPanel.Children.Add(iconText);
            headerPanel.Children.Add(nameText);

            // Shift details
            var detailsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            // Time
            var timePanel = CreateDetailRow("⏰ Time:", shift.DisplayTime, "White", new Thickness(0, 0, 0, 5));
            detailsPanel.Children.Add(timePanel);

            // Grace period
            var gracePanel = CreateDetailRow("⏱️ Grace:", $"{shift.GracePeriodMinutes} min", "#4CAF50", new Thickness(0, 0, 0, 5));
            detailsPanel.Children.Add(gracePanel);

            // Days (simplified based on shift name)
            var days = GetShiftDays(shift.Name);
            var daysPanel = CreateDetailRow("📅 Days:", days, "White", new Thickness(0, 0, 0, 0));
            detailsPanel.Children.Add(daysPanel);

            // Status badge
            var statusText = shift.IsCurrentlyActive ? "🟢 Currently Active" : "🔴 Not Active";
            if (shift.Name.Contains("Weekend", StringComparison.OrdinalIgnoreCase))
            {
                statusText = "🟡 Weekend Shift";
            }

            var statusBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(shift.StatusColor + "90")), // Add transparency
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 3, 0,0),
                Height = 32,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var statusTextBlock = new TextBlock
            {
                Text = statusText,
                Foreground = Brushes.White,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            statusBorder.Child = statusTextBlock;

            // Add all elements to the card
            stackPanel.Children.Add(headerPanel);
            stackPanel.Children.Add(detailsPanel);
            stackPanel.Children.Add(statusBorder);

            card.Content = stackPanel;
            return card;
        }

        private StackPanel CreateDetailRow(string label, string value, string valueColor, Thickness margin)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = margin
            };

            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 12,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var valueText = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(valueColor)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };

            panel.Children.Add(labelText);
            panel.Children.Add(valueText);

            return panel;
        }

        private string GetShiftIcon(string shiftName)
        {
            if (shiftName.Contains("Morning", StringComparison.OrdinalIgnoreCase))
                return "🌅";
            if (shiftName.Contains("Evening", StringComparison.OrdinalIgnoreCase))
                return "🌇";
            if (shiftName.Contains("Night", StringComparison.OrdinalIgnoreCase))
                return "🌙";
            if (shiftName.Contains("Weekend", StringComparison.OrdinalIgnoreCase))
                return "🎯";
            return "🕒";
        }

        private string GetShiftDays(string shiftName)
        {
            if (shiftName.Contains("Weekend", StringComparison.OrdinalIgnoreCase) ||
                shiftName.Contains("Sat", StringComparison.OrdinalIgnoreCase) ||
                shiftName.Contains("Sun", StringComparison.OrdinalIgnoreCase))
                return "Sat – Sun";
            if (shiftName.Contains("Flex", StringComparison.OrdinalIgnoreCase))
                return "Flexible";
            return "Mon – Fri";
        }

        private void ShowDefaultShifts()
        {
            var defaultShifts = new List<ShiftResponseDto>
            {
                new ShiftResponseDto
                {
                    Name = "Morning Shift",
                    DisplayTime = "08:00 – 14:00",
                    GracePeriodMinutes = 5,
                    IsCurrentlyActive = DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 14,
                    StatusColor = DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 14 ? "#38b000" : "#666666"
                },
                new ShiftResponseDto
                {
                    Name = "Evening Shift",
                    DisplayTime = "14:00 – 22:00",
                    GracePeriodMinutes = 10,
                    IsCurrentlyActive = DateTime.Now.Hour >= 14 && DateTime.Now.Hour < 22,
                    StatusColor = "#FF9800"
                }
            };

            UpdateShiftsDisplay(defaultShifts);
        }
    }
}