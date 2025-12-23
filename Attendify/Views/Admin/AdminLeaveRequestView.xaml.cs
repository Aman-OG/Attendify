using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Attendify.Views.UserControls
{
    public partial class LeaveRequestsView : UserControl
    {
        private ObservableCollection<LeaveRequest> _leaveRequests;
        private LeaveRequest _selectedRequest;
        private Button _activeFilterButton;
        private HttpClient _httpClient;
        private string _apiBaseUrl = $"{Attendify.Services.HttpClientService.ApiBaseUrl}/leaveRequests";

        public LeaveRequestsView()
        {
            InitializeComponent();
            InitializeHttpClient();
            InitializeRejectionPlaceholder();

            // Set up hover effects for filter buttons
            SetupFilterButtonHoverEffects();

            // Set All as default active filter
            SetActiveFilter(BtnAll);

            // Load data when control is loaded
            Loaded += async (s, e) => await LoadLeaveRequestsAsync();
        }

        private void InitializeHttpClient()
        {
            _httpClient = Attendify.Services.HttpClientService.Instance;
        }

        private async Task LoadLeaveRequestsAsync(string status = "all", string filter = "all", string search = "")
        {
            try
            {
                // Show loading
                LeaveRequestsGrid.ItemsSource = null;

                var url = $"{_apiBaseUrl}?status={status}&filter={filter}";
                if (!string.IsNullOrEmpty(search))
                {
                    url += $"&search={Uri.EscapeDataString(search)}";
                }

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var leaves = await response.Content.ReadFromJsonAsync<List<LeaveRequest>>();
                    _leaveRequests = new ObservableCollection<LeaveRequest>(leaves ?? new List<LeaveRequest>());
                    LeaveRequestsGrid.ItemsSource = _leaveRequests;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    GlassMessageBox.Show($"Error loading leave requests: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
            }
        }

        private void SetupFilterButtonHoverEffects()
        {
            // Define hover colors for each button
            var buttonColors = new Dictionary<Button, string>
            {
                { BtnAll, "#00A6FB" },
                { BtnToday, "#A95315" },
                { BtnApproved, "#2FBF4C" },
                { BtnPending, "#E3C63A" },
                { BtnRejected, "#D23C3C" }
            };

            foreach (var button in buttonColors.Keys)
            {
                // Store the original glass background
                var originalBackground = CreateGlassBackground();
                var hoverColor = buttonColors[button];

                button.MouseEnter += (s, e) =>
                {
                    if (button != _activeFilterButton)
                    {
                        button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hoverColor));
                    }
                };

                button.MouseLeave += (s, e) =>
                {
                    if (button != _activeFilterButton)
                    {
                        button.Background = originalBackground;
                    }
                };
            }
        }

        private LinearGradientBrush CreateGlassBackground()
        {
            return new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#20FFFFFF"), 0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#10FFFFFF"), 1)
                }
            };
        }

        private void SetActiveFilter(Button activeButton)
        {
            // Reset all buttons to glass background
            var glassBackground = CreateGlassBackground();

            BtnAll.Background = glassBackground;
            BtnToday.Background = glassBackground;
            BtnApproved.Background = glassBackground;
            BtnPending.Background = glassBackground;
            BtnRejected.Background = glassBackground;

            // Set active button color based on which button it is
            string activeColor = activeButton.Name switch
            {
                "BtnAll" => "#00A6FB",
                "BtnToday" => "#A95315",
                "BtnApproved" => "#2FBF4C",
                "BtnPending" => "#E3C63A",
                "BtnRejected" => "#D23C3C",
                _ => "#6000A6FB"
            };

            activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(activeColor));
            _activeFilterButton = activeButton;
        }

        private void InitializeRejectionPlaceholder()
        {
            // Set initial placeholder
            UpdateRejectionPlaceholder();

            RejectionReasonTextBox.GotFocus += (s, e) =>
            {
                RejectionPlaceholder.Visibility = Visibility.Collapsed;
            };

            RejectionReasonTextBox.LostFocus += (s, e) =>
            {
                UpdateRejectionPlaceholder();
            };

            RejectionReasonTextBox.TextChanged += (s, e) =>
            {
                UpdateRejectionPlaceholder();
            };
        }

        private void UpdateRejectionPlaceholder()
        {
            if (string.IsNullOrEmpty(RejectionReasonTextBox.Text))
            {
                RejectionPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                RejectionPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                SetActiveFilter(button);

                // Map button content to filter parameters
                var (status, filter) = button.Content.ToString() switch
                {
                    "All" => ("all", "all"),
                    "Today" => ("all", "today"),
                    "Approved" => ("Approved", "all"),
                    "Pending" => ("Pending", "all"),
                    "Rejected" => ("Rejected", "all"),
                    _ => ("all", "all")
                };

                await LoadLeaveRequestsAsync(status, filter, SearchBox.Text);
            }
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

            // Get current filter status
            var status = _activeFilterButton?.Content.ToString() switch
            {
                "Approved" => "Approved",
                "Pending" => "Pending",
                "Rejected" => "Rejected",
                _ => "all"
            };

            var filter = _activeFilterButton?.Content.ToString() == "Today" ? "today" : "all";

            await LoadLeaveRequestsAsync(status, filter, SearchBox.Text);
        }

        private void LeaveRequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reset rejection panel when selection changes
            ResetRejectionPanel();

            _selectedRequest = LeaveRequestsGrid.SelectedItem as LeaveRequest;

            if (_selectedRequest != null)
            {
                ShowDetailedView(_selectedRequest);
            }
            else
            {
                HideDetailedView();
            }
        }

        private void ShowDetailedView(LeaveRequest request)
        {
            DetailedViewPanel.Visibility = Visibility.Visible;
            EmptyStateText.Visibility = Visibility.Collapsed;

            // Populate details
            DetailEmpId.Text = request.EmpId;
            DetailEmployee.Text = request.EmployeeName;
            DetailDepartment.Text = request.Department;
            DetailPosition.Text = request.Position;
            DetailEmail.Text = request.Email;
            DetailFromDate.Text = request.FromDate;
            DetailToDate.Text = request.ToDate;
            DetailReason.Text = request.Reason;
            DetailDescription.Text = request.Description;
            DetailStatus.Text = request.Status;
            DetailStatusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(request.StatusColor));

            // Show/hide action buttons based on status
            if (request.Status == "Pending")
            {
                BtnApprove.Visibility = Visibility.Visible;
                BtnReject.Visibility = Visibility.Visible;
            }
            else
            {
                BtnApprove.Visibility = Visibility.Collapsed;
                BtnReject.Visibility = Visibility.Collapsed;
            }
        }

        private void HideDetailedView()
        {
            DetailedViewPanel.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Visible;
        }

        private void ResetRejectionPanel()
        {
            RejectionReasonPanel.Visibility = Visibility.Collapsed;
            RejectionReasonTextBox.Text = string.Empty;
            BtnReject.Content = "Reject";
            UpdateRejectionPlaceholder();
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest != null)
            {
                try
                {
                    var response = await _httpClient.PutAsync(
                        $"{_apiBaseUrl}/{_selectedRequest.LeaveId}/approve", null);

                    if (response.IsSuccessStatusCode)
                    {
                        // Update local data
                        _selectedRequest.Status = "Approved";
                        _selectedRequest.StatusColor = "#2FBF4C";

                        // Refresh the display
                        LeaveRequestsGrid.Items.Refresh();
                        ShowDetailedView(_selectedRequest);
                        ResetRejectionPanel();

                        GlassMessageBox.Show("Leave request approved successfully!", "Success", false, GlassMessageBox.MessageType.Success);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        GlassMessageBox.Show($"Error approving request: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                    }
                }
                catch (Exception ex)
                {
                    GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Clear selection
            LeaveRequestsGrid.SelectedItem = null;
            HideDetailedView();
            ResetRejectionPanel();

            // Visual feedback
            var button = sender as Button;
            if (button != null)
            {
                button.Content = "⏳";

                // Get current filter status
                var status = _activeFilterButton?.Content.ToString() switch
                {
                    "Approved" => "Approved",
                    "Pending" => "Pending",
                    "Rejected" => "Rejected",
                    _ => "all"
                };

                var filter = _activeFilterButton?.Content.ToString() == "Today" ? "today" : "all";

                await LoadLeaveRequestsAsync(status, filter, SearchBox.Text);

                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, args) =>
                {
                    button.Content = "⟳";
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest != null && _selectedRequest.Status == "Pending")
            {
                // Toggle rejection reason panel visibility
                if (RejectionReasonPanel.Visibility == Visibility.Visible)
                {
                    // If panel is already visible, confirm rejection
                    await ConfirmRejection();
                }
                else
                {
                    // Show rejection reason input
                    RejectionReasonPanel.Visibility = Visibility.Visible;
                    RejectionReasonTextBox.Text = string.Empty;
                    UpdateRejectionPlaceholder();

                    // Change reject button text to indicate confirmation
                    BtnReject.Content = "Confirm Reject";
                }
            }
        }

        private async Task ConfirmRejection()
        {
            if (_selectedRequest != null && !string.IsNullOrWhiteSpace(RejectionReasonTextBox.Text))
            {
                try
                {
                    var rejectDto = new
                    {
                        RejectionReason = RejectionReasonTextBox.Text,
                        AdminId = 1 // Get from your auth system
                    };

                    var response = await _httpClient.PutAsJsonAsync(
                        $"{_apiBaseUrl}/{_selectedRequest.LeaveId}/reject", rejectDto);

                    if (response.IsSuccessStatusCode)
                    {
                        // Update local data
                        _selectedRequest.Status = "Rejected";
                        _selectedRequest.StatusColor = "#D23C3C";

                        // Refresh the display
                        LeaveRequestsGrid.Items.Refresh();
                        ShowDetailedView(_selectedRequest);

                        // Hide rejection panel and reset
                        ResetRejectionPanel();

                        GlassMessageBox.Show($"Leave request rejected. Reason: {rejectDto.RejectionReason}", "Rejected", false, GlassMessageBox.MessageType.Success);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        GlassMessageBox.Show($"Error rejecting request: {error}", "Error", false, GlassMessageBox.MessageType.Error);
                    }
                }
                catch (Exception ex)
                {
                    GlassMessageBox.Show($"Error: {ex.Message}", "Error", false, GlassMessageBox.MessageType.Error);
                }
            }
            else
            {
                GlassMessageBox.Show("Please enter a reason for rejection.", "Reason Required");
            }
        }




        // DTO class for WPF data binding
        public class LeaveRequest
        {
            public int LeaveId { get; set; }
            public string No { get; set; }
            public string EmployeeName { get; set; }
            public string EmpId { get; set; }
            public string Department { get; set; }
            public string Position { get; set; }
            public string Email { get; set; }
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public string Reason { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public string StatusColor { get; set; } // Store as string for API compatibility
                                                    // Add a Brush property that converts the string
            public Brush StatusBrush
            {
                get
                {
                    try
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(StatusColor));
                    }
                    catch
                    {
                        return new SolidColorBrush(Colors.Gray);
                    }
                }
            }
        }
    }
}