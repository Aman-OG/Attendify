using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Attendify.Views.UserControls
{
    public partial class EmployeeNotificationsView : UserControl
    {
        private HttpClient _httpClient;
        // private const string ApiBaseUrl = "https://localhost:7129/api";
        private string _currentEmpCode = "";
        private DispatcherTimer _refreshTimer;

        // DTO classes
        public class NotificationResponseDto
        {
            public int MessageId { get; set; }
            public string Title { get; set; } = null!;
            public string Body { get; set; } = null!;
            public bool IsActive { get; set; }
            public string CreatedAt { get; set; } = null!;
            public string CreatedDate { get; set; } = null!;
            public string CreatedTime { get; set; } = null!;
            public string NotificationType { get; set; } = "Info";
            public string StatusBadge { get; set; } = "📢 Info";
            public string StatusColor { get; set; } = "#00A6FB";
            public string CardStyle { get; set; } = "Info";
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        public class UnreadCountDto
        {
            public int Count { get; set; }
        }

        public EmployeeNotificationsView()
        {
            InitializeComponent();
            Loaded += EmployeeNotificationsView_Loaded;
        }

        // Constructor with empCode parameter
        public EmployeeNotificationsView(string empCode) : this()
        {
            _currentEmpCode = empCode;
        }

        private void EmployeeNotificationsView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeHttpClient();
            LoadNotifications();

            // Set up auto-refresh timer (every 2 minutes)
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            _refreshTimer.Tick += (s, e) => LoadNotifications();
            _refreshTimer.Start();
        }

        private void InitializeHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = Attendify.Services.HttpClientService.Instance;
            }
        }

        private async void LoadNotifications()
        {
            try
            {
                await LoadUnreadCount();
                await LoadMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading notifications: {ex.Message}");
            }
        }

        private async Task LoadUnreadCount()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeenotifications/unread-count");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var countJson = apiResponse.Data.ToString();
                        var countData = JsonSerializer.Deserialize<UnreadCountDto>(countJson, options);

                        Dispatcher.Invoke(() =>
                        {
                            // You could display the count somewhere if you want
                            // For example: UnreadCountBadge.Text = countData?.Count.ToString() ?? "0";
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading unread count: {ex.Message}");
            }
        }

        private async Task LoadMessages()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Attendify.Services.HttpClientService.ApiBaseUrl}/employeenotifications/messages");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto>(json, options);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var messagesJson = apiResponse.Data.ToString();
                        var messages = JsonSerializer.Deserialize<List<NotificationResponseDto>>(messagesJson, options);

                        Dispatcher.Invoke(() =>
                        {
                            UpdateNotificationsDisplay(messages ?? new List<NotificationResponseDto>());
                        });
                    }
                }
                else
                {
                    // Show sample data if API fails
                    Dispatcher.Invoke(() => ShowSampleNotifications());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading messages: {ex.Message}");
                Dispatcher.Invoke(() => ShowSampleNotifications());
            }
        }

        private void UpdateNotificationsDisplay(List<NotificationResponseDto> notifications)
        {
            // Clear existing notifications
            NotificationsListPanel.Children.Clear();

            if (notifications == null || !notifications.Any())
            {
                ShowNoNotificationsMessage();
                return;
            }

            // Create notification cards dynamically
            foreach (var notification in notifications)
            {
                var notificationCard = CreateNotificationCard(notification);
                NotificationsListPanel.Children.Add(notificationCard);
            }
        }

        private Border CreateNotificationCard(NotificationResponseDto notification)
        {
            var card = new Border
            {
                Style = (Style)FindResource($"{notification.CardStyle}NotificationStyle"),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left side - Content
            var contentPanel = new StackPanel();
            Grid.SetColumn(contentPanel, 0);

            // Title with icon
            var titleIcon = GetNotificationIcon(notification.NotificationType);
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var iconText = new TextBlock
            {
                Text = titleIcon,
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var titleText = new TextBlock
            {
                Text = notification.Title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            titlePanel.Children.Add(iconText);
            titlePanel.Children.Add(titleText);

            // Body
            var bodyText = new TextBlock
            {
                Text = notification.Body,
                FontSize = 13,
                Foreground = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                LineHeight = 18
            };

            // Info panel
            var infoPanel = new WrapPanel { Orientation = Orientation.Horizontal };

            // Date
            var datePanel = CreateInfoPanel("📅", "Date:", $"{notification.CreatedDate} • {notification.CreatedTime}",
                new Thickness(0, 0, 20, 0));
            infoPanel.Children.Add(datePanel);

            // Type indicator
            var typePanel = CreateInfoPanel("📢", "Type:", notification.NotificationType, new Thickness(0, 0, 0, 0));
            infoPanel.Children.Add(typePanel);

            // Add all content
            contentPanel.Children.Add(titlePanel);
            contentPanel.Children.Add(bodyText);
            contentPanel.Children.Add(infoPanel);

            // Right side - Status badge
            var badgePanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(badgePanel, 1);

            var statusBadge = new Border
            {
                Style = (Style)FindResource("StatusBadgeStyle"),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(notification.StatusColor + "80")),
                Margin = new Thickness(10, 0, 0, 0)
            };

            var statusText = new TextBlock
            {
                Text = notification.StatusBadge,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            statusBadge.Child = statusText;
            badgePanel.Children.Add(statusBadge);

            // Add to grid
            grid.Children.Add(contentPanel);
            grid.Children.Add(badgePanel);

            card.Child = grid;
            return card;
        }

        private StackPanel CreateInfoPanel(string icon, string label, string value, Thickness margin)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = margin
            };

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 12,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var labelText = new TextBlock
            {
                Text = label + " ",
                FontSize = 12,
                Foreground = Brushes.Gray
            };

            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            panel.Children.Add(iconText);
            panel.Children.Add(labelText);
            panel.Children.Add(valueText);

            return panel;
        }

        private string GetNotificationIcon(string notificationType)
        {
            return notificationType switch
            {
                "Important" => "🚨",
                "Success" => "🎉",
                "System" => "🔧",
                "Event" => "📅",
                _ => "📢"
            };
        }

        private void ShowNoNotificationsMessage()
        {
            var messageCard = new Border
            {
                Style = (Style)FindResource("NotificationCardStyle"),
                Margin = new Thickness(0, 20, 0, 0)
            };

            var contentPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20)
            };

            var iconText = new TextBlock
            {
                Text = "📭",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var messageText = new TextBlock
            {
                Text = "No New Notifications",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var subText = new TextBlock
            {
                Text = "You're all caught up! Check back later for updates.",
                FontSize = 14,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            contentPanel.Children.Add(iconText);
            contentPanel.Children.Add(messageText);
            contentPanel.Children.Add(subText);
            messageCard.Child = contentPanel;

            NotificationsListPanel.Children.Add(messageCard);
        }

        private void ShowSampleNotifications()
        {
            var sampleNotifications = new List<NotificationResponseDto>
            {
                new NotificationResponseDto
                {
                    Title = "System Maintenance Tonight",
                    Body = "All systems will be unavailable from 10:00 PM to 2:00 AM for scheduled maintenance. Please save your work and log out before this time.",
                    CreatedDate = DateTime.Now.ToString("MMM dd, yyyy"),
                    CreatedTime = "8:00 PM",
                    NotificationType = "Important",
                    StatusBadge = "⚠️ Active",
                    StatusColor = "#FF6B6B",
                    CardStyle = "Important"
                },
                new NotificationResponseDto
                {
                    Title = "Meeting Tomorrow at 10 AM",
                    Body = "Quarterly performance review meeting in Conference Room A. Please bring your progress reports.",
                    CreatedDate = DateTime.Now.AddDays(1).ToString("MMM dd, yyyy"),
                    CreatedTime = "10:00 AM",
                    NotificationType = "Event",
                    StatusBadge = "📅 Upcoming",
                    StatusColor = "#00A6FB",
                    CardStyle = "Info"
                }
            };

            UpdateNotificationsDisplay(sampleNotifications);
        }
    }
}