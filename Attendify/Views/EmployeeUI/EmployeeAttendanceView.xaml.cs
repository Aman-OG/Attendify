using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Attendify.Views.Employee
{
    public partial class EmployeeAttendanceView : UserControl
    {
        private DispatcherTimer _clockTimer;
        private HttpClient _httpClient;
        // private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode = "";

        public EmployeeAttendanceView()
        {
            InitializeComponent();
            Loaded += EmployeeAttendanceView_Loaded;
        }

        private void EmployeeAttendanceView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeClock();
            SetLoadingState();
        }

        public void SetEmployeeCode(string empCode)
        {
            if (string.IsNullOrEmpty(empCode))
            {
                ShowErrorMessage("Employee code is required");
                return;
            }

            _currentEmpCode = empCode;

            // Initialize HTTP client
            if (_httpClient == null)
            {
                _httpClient = Attendify.Services.HttpClientService.Instance;
            }

            LoadAttendanceData();
        }

        private void SetLoadingState()
        {
            TxtTodayDate.Text = "Loading...";
            TxtShiftDetails.Text = "Loading...";
            TxtGracePeriod.Text = "-- min";
            TxtCurrentTime.Text = "--:-- --";

            // Disable check-in button initially
            BtnCheckIn.IsEnabled = false;
            BtnCheckIn.Content = "LOADING...";

            // Clear history grid
            AttendanceHistoryGrid.ItemsSource = null;
        }

        private void InitializeClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            TxtCurrentTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        private async void LoadAttendanceData()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeattendance/data/{_currentEmpCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var attendanceData = JsonSerializer.Deserialize<AttendanceData>(json, options);

                    if (attendanceData != null)
                    {
                        Dispatcher.Invoke(() => UpdateUI(attendanceData));
                    }
                }
                else
                {
                    Dispatcher.Invoke(() => ShowErrorMessage("Failed to load attendance data"));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowErrorMessage($"Error: {ex.Message}"));
            }
        }

        // Local DTO classes
        public class AttendanceData
        {
            public TodayAttendanceInfo? TodayAttendance { get; set; }
            public List<AttendanceHistoryRecord>? AttendanceHistory { get; set; }
            public MonthlyStats? MonthlyStats { get; set; }
            public ShiftInfo? CurrentShift { get; set; }
            public bool CanCheckIn { get; set; }
            public bool CanCheckOut { get; set; }
        }

        public class TodayAttendanceInfo
        {
            public string TodayDate { get; set; } = null!;
            public string ShiftName { get; set; } = null!;
            public string ShiftTime { get; set; } = null!;
            public int GracePeriodMinutes { get; set; }
            public string CurrentTime { get; set; } = null!;
            public string? CheckInTime { get; set; }
            public string? Status { get; set; }
            public string StatusColor { get; set; } = "#FF6B6B";
            public bool IsCheckedIn { get; set; }
            public bool IsLate { get; set; }
            public int? LateMinutes { get; set; }
        }

        public class AttendanceHistoryRecord
        {
            public string Date { get; set; } = null!;
            public string Shift { get; set; } = null!;
            public string CheckIn { get; set; } = null!;
            public string Status { get; set; } = null!;
            public string StatusColor { get; set; } = "#FF6B6B";
        }

        public class MonthlyStats
        {
            public int DaysPresent { get; set; }
            public int OnTimeRate { get; set; }
            public int LateArrivals { get; set; }
        }

        public class ShiftInfo
        {
            public string Name { get; set; } = null!;
            public string StartTime { get; set; } = null!;
            public string EndTime { get; set; } = null!;
            public int GracePeriodMinutes { get; set; }
        }

        private void UpdateUI(AttendanceData data)
        {
            try
            {
                // Update today's attendance
                if (data.TodayAttendance != null)
                {
                    TxtTodayDate.Text = data.TodayAttendance.TodayDate;
                    TxtShiftDetails.Text = $"{data.TodayAttendance.ShiftName} ({data.TodayAttendance.ShiftTime})";
                    TxtGracePeriod.Text = $"{data.TodayAttendance.GracePeriodMinutes} min";

                    if (data.TodayAttendance.IsCheckedIn && !string.IsNullOrEmpty(data.TodayAttendance.CheckInTime))
                    {
                        // Already checked in
                        BtnCheckIn.IsEnabled = false;
                        BtnCheckIn.Content = "CHECKED IN";
                        TxtCheckedInTime.Text = $"Checked in at {data.TodayAttendance.CheckInTime}";
                        TxtCheckedInTime.Visibility = Visibility.Visible;

                        // Update status
                        if (data.TodayAttendance.IsLate && data.TodayAttendance.LateMinutes.HasValue)
                        {
                            StatusBadge.Background = new SolidColorBrush(Colors.OrangeRed);
                            StatusText.Text = $"LATE ({data.TodayAttendance.LateMinutes} min)";
                            LateInfo.Text = $"{data.TodayAttendance.LateMinutes} min behind schedule";
                        }
                        else
                        {
                            StatusBadge.Background = new SolidColorBrush(Colors.LimeGreen);
                            StatusText.Text = "ON TIME";
                            LateInfo.Text = "Right on schedule";
                        }
                    }
                    else
                    {
                        // Not checked in yet
                        BtnCheckIn.IsEnabled = data.CanCheckIn;
                        BtnCheckIn.Content = "CHECK IN";
                        TxtCheckedInTime.Visibility = Visibility.Collapsed;

                        // Update status based on current time vs shift
                        StatusBadge.Background = new SolidColorBrush(Colors.Gray);
                        StatusText.Text = "NOT CHECKED IN";
                        LateInfo.Text = "Waiting for check-in";
                    }
                }

                // Update attendance history
                if (data.AttendanceHistory != null)
                {
                    var historyItems = new List<AttendanceHistoryItem>();
                    foreach (var record in data.AttendanceHistory)
                    {
                        historyItems.Add(new AttendanceHistoryItem
                        {
                            Date = record.Date,
                            Shift = record.Shift,
                            CheckIn = record.CheckIn,
                            Status = record.Status,
                            StatusColor = GetStatusColor(record.StatusColor)
                        });
                    }
                    AttendanceHistoryGrid.ItemsSource = historyItems;
                }

                // Update monthly stats
                if (data.MonthlyStats != null)
                {
                    DaysPresentCount.Text = data.MonthlyStats.DaysPresent.ToString();
                    OnTimeRateCount.Text = $"{data.MonthlyStats.OnTimeRate}%";
                    LateArrivalsCount.Text = data.MonthlyStats.LateArrivals.ToString();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error updating UI: {ex.Message}");
            }
        }

        private Brush GetStatusColor(string colorHex)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            }
            catch
            {
                return Brushes.Gray;
            }
        }

        private async void BtnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentTime = DateTime.Now.ToString("HH:mm");

                var checkInRequest = new
                {
                    EmpCode = _currentEmpCode,
                    CheckInTime = currentTime
                };

                var json = JsonSerializer.Serialize(checkInRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeeattendance/checkin", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<CheckInResult>(responseJson, options);

                    if (result?.Success == true)
                    {
                        MessageBox.Show(result.Message, "Check In Successful",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Reload data
                        LoadAttendanceData();
                    }
                    else
                    {
                        MessageBox.Show(result?.Message ?? "Check-in failed", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to check in. Please try again.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Check In Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper classes for check-in response
        private class CheckInResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public string CheckInTime { get; set; } = null!;
            public string Status { get; set; } = null!;
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Class for DataGrid items
    public class AttendanceHistoryItem
    {
        public string Date { get; set; } = null!;
        public string Shift { get; set; } = null!;
        public string CheckIn { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Brush StatusColor { get; set; } = Brushes.Gray;
    }
}