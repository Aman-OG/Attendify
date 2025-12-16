using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Attendify.Views.Employee
{
    public partial class EmployeeHomeView : UserControl
    {
        private HttpClient _httpClient;
        // private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode = "";

        public EmployeeHomeView()
        {
            InitializeComponent();

            // Initialize with loading state
            SetLoadingState();

            // Initialize HTTP client later when needed
        }

        private void SetLoadingState()
        {
            WelcomeText.Text = "Welcome";
            WelcomeSubtitle.Text = "Loading your dashboard...";
            NextShiftName.Text = "Loading...";
            NextShiftTime.Text = "--:-- – --:--";
            NextShiftStatus.Text = "Loading...";
            TodayCheckIn.Text = "Loading...";
            AttendanceStatusText.Text = "Loading...";
            TodayGracePeriod.Text = "-- min";
            PendingCount.Text = "0";
            ApprovedCount.Text = "0";
            RejectedCount.Text = "0";
            LeaveRemaining.Text = "Loading...";
            NewMessagesCount.Text = "0 New";
        }

        public void SetEmployeeCode(string empCode)
        {
            if (string.IsNullOrEmpty(empCode))
            {
                ShowErrorMessage("Employee code is required");
                return;
            }

            _currentEmpCode = empCode;

            // Initialize HTTP client only when needed
            // Initialize HTTP client only when needed
            if (_httpClient == null)
            {
                _httpClient = Attendify.Services.HttpClientService.Instance;
            }

            LoadDashboardData();
        }

        private async void LoadDashboardData()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeehome/dashboard/{_currentEmpCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var dashboardData = JsonSerializer.Deserialize<EmployeeHomeData>(json, options);

                    if (dashboardData != null)
                    {
                        // Update UI on the UI thread
                        Dispatcher.Invoke(() => UpdateUI(dashboardData));
                    }
                }
                else
                {
                    Dispatcher.Invoke(() => ShowErrorMessage("Failed to load dashboard data from server"));
                }
            }
            catch (HttpRequestException ex)
            {
                Dispatcher.Invoke(() => ShowErrorMessage($"Network error: {ex.Message}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowErrorMessage($"Error: {ex.Message}"));
            }
        }

        // Local DTO classes - UPDATED to match API controller
        public class EmployeeHomeData
        {
            public EmployeeInfo? EmployeeInfo { get; set; }
            public TodayAttendance? TodayAttendance { get; set; }
            public NextShiftInfo? NextShift { get; set; }
            public LeaveStatusInfo? LeaveStatus { get; set; }
            public List<AdminMessageInfo>? AdminMessages { get; set; }
            public List<RecentActivityInfo>? RecentActivities { get; set; }
        }

        public class EmployeeInfo
        {
            public string FirstName { get; set; } = null!;
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? Email { get; set; }
        }

        public class TodayAttendance
        {
            public string? CheckInTime { get; set; }
            public string? Status { get; set; }
            public string? ShiftName { get; set; }
            public int? GracePeriodMinutes { get; set; }
            public bool IsLate { get; set; }
            public int? LateMinutes { get; set; }
            public bool HasCheckedIn { get; set; } // Added this property
        }

        public class NextShiftInfo
        {
            public string? ShiftName { get; set; }
            public string? StartTime { get; set; }
            public string? EndTime { get; set; }
            public string? TimeUntilStart { get; set; }
        }

        public class LeaveStatusInfo
        {
            public int PendingCount { get; set; }
            public int ApprovedCount { get; set; }
            public int RejectedCount { get; set; }
            public int RemainingDays { get; set; }
            public string RemainingText { get; set; } = string.Empty; // Added this property
        }

        public class AdminMessageInfo
        {
            public string Title { get; set; } = null!;
            public string Body { get; set; } = null!;
            public string Type { get; set; } = null!;
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class RecentActivityInfo
        {
            public string ActivityType { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string TimeAgo { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
        }

        private void UpdateUI(EmployeeHomeData data)
        {
            try
            {
                // Update welcome message
                if (data.EmployeeInfo != null)
                {
                    string fullName = data.EmployeeInfo.FirstName;
                    if (!string.IsNullOrWhiteSpace(data.EmployeeInfo.MiddleName))
                    {
                        fullName += " " + data.EmployeeInfo.MiddleName;
                    }
                    WelcomeText.Text = $"Welcome, {fullName}";
                    WelcomeSubtitle.Text = "Here's your dashboard overview for today. Stay productive! 💪";
                }

                // Update next shift
                if (data.NextShift != null)
                {
                    NextShiftName.Text = data.NextShift.ShiftName ?? "Not scheduled";
                    NextShiftTime.Text = $"{data.NextShift.StartTime ?? "--:--"} – {data.NextShift.EndTime ?? "--:--"}";
                    NextShiftStatus.Text = data.NextShift.TimeUntilStart ?? "Not available";
                }

                // Update today's attendance
                if (data.TodayAttendance != null)
                {
                    // Use HasCheckedIn to determine what to display
                    if (data.TodayAttendance.HasCheckedIn && !string.IsNullOrEmpty(data.TodayAttendance.CheckInTime))
                    {
                        TodayCheckIn.Text = $"Checked In: {data.TodayAttendance.CheckInTime}";
                    }
                    else
                    {
                        TodayCheckIn.Text = "Not checked in yet";
                    }

                    if (data.TodayAttendance.IsLate && data.TodayAttendance.LateMinutes.HasValue)
                    {
                        AttendanceStatusText.Text = $"Late ({data.TodayAttendance.LateMinutes} min)";
                    }
                    else
                    {
                        AttendanceStatusText.Text = data.TodayAttendance.Status ?? "Not checked in";
                    }

                    AttendanceStatusColor.Background = GetStatusBrush(data.TodayAttendance.Status, data.TodayAttendance.IsLate);
                    TodayGracePeriod.Text = data.TodayAttendance.GracePeriodMinutes.HasValue
                        ? $"{data.TodayAttendance.GracePeriodMinutes} min"
                        : "5 min";
                }

                // Update leave status
                if (data.LeaveStatus != null)
                {
                    PendingCount.Text = data.LeaveStatus.PendingCount.ToString();
                    ApprovedCount.Text = data.LeaveStatus.ApprovedCount.ToString();
                    RejectedCount.Text = data.LeaveStatus.RejectedCount.ToString();

                    // Use RemainingText if available, otherwise use RemainingDays
                    if (!string.IsNullOrEmpty(data.LeaveStatus.RemainingText))
                    {
                        LeaveRemaining.Text = data.LeaveStatus.RemainingText;
                    }
                    else
                    {
                        LeaveRemaining.Text = $"{data.LeaveStatus.RemainingDays} days remaining";
                    }
                }

                // Update admin messages
                MessagesContainer.Children.Clear();
                if (data.AdminMessages != null && data.AdminMessages.Count > 0)
                {
                    var newMessages = data.AdminMessages.Count(m => m.IsActive);
                    NewMessagesCount.Text = $"{newMessages} New";
                    NoMessagesText.Visibility = Visibility.Collapsed;

                    foreach (var message in data.AdminMessages)
                    {
                        var messageItem = CreateMessageItem(message);
                        MessagesContainer.Children.Add(messageItem);
                    }
                }
                else
                {
                    NoMessagesText.Visibility = Visibility.Visible;
                    NewMessagesCount.Text = "0 New";
                }

                // Update recent activities
                ActivitiesContainer.Children.Clear();
                if (data.RecentActivities != null && data.RecentActivities.Count > 0)
                {
                    NoActivitiesText.Visibility = Visibility.Collapsed;
                    foreach (var activity in data.RecentActivities)
                    {
                        var activityItem = CreateActivityItem(activity);
                        ActivitiesContainer.Children.Add(activityItem);
                    }
                }
                else
                {
                    NoActivitiesText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error updating UI: {ex.Message}");
            }
        }

        private Border CreateMessageItem(AdminMessageInfo message)
        {
            var border = new Border
            {
                Style = (Style)FindResource("MessageItemStyle"),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var stackPanel = new StackPanel();

            var titleText = new TextBlock
            {
                Text = $"\"{message.Title}\"",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var bodyText = new TextBlock
            {
                Text = message.Body,
                Foreground = Brushes.LightGray,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(bodyText);

            Grid.SetColumn(stackPanel, 0);
            grid.Children.Add(stackPanel);

            var statusBorder = new Border
            {
                Background = GetMessageTypeBrush(message.Type),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10, 0, 0, 0)
            };

            var statusText = new TextBlock
            {
                Text = message.Type,
                Foreground = Brushes.White,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };

            statusBorder.Child = statusText;
            Grid.SetColumn(statusBorder, 1);
            grid.Children.Add(statusBorder);

            border.Child = grid;
            return border;
        }

        private Border CreateActivityItem(RecentActivityInfo activity)
        {
            var border = new Border
            {
                Style = (Style)FindResource("MessageItemStyle"),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            string emoji = activity.ActivityType switch
            {
                "Attendance" => "✅",
                "Leave" => "📋",
                _ => "🔄"
            };

            var emojiText = new TextBlock
            {
                Text = emoji,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var descText = new TextBlock
            {
                Text = activity.Description,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            var timeText = new TextBlock
            {
                Text = activity.TimeAgo,
                Foreground = Brushes.LightGray,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0)
            };

            stackPanel.Children.Add(emojiText);
            stackPanel.Children.Add(descText);
            stackPanel.Children.Add(timeText);

            border.Child = stackPanel;
            return border;
        }

        private Brush GetStatusBrush(string? status, bool isLate = false)
        {
            if (isLate)
                return new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));

            if (string.IsNullOrEmpty(status))
                return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));

            return status.ToLower() switch
            {
                "late" => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                "on time" or "ontime" or "present" => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                "early" => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                "absent" => new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x00)),
                _ => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00))
            };
        }

        private Brush GetMessageTypeBrush(string type)
        {
            if (string.IsNullOrEmpty(type))
                return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));

            return type.ToLower() switch
            {
                "urgent" or "emergency" => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                "important" or "warning" => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),
                "announcement" or "info" => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                "notice" => new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),
                _ => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
            };
        }

        private void ShowErrorMessage(string message)
        {
            // Simple error display
            WelcomeSubtitle.Text = $"Error: {message}";
            WelcomeSubtitle.Foreground = Brushes.OrangeRed;

            System.Diagnostics.Debug.WriteLine($"Dashboard Error: {message}");
        }
    }
}