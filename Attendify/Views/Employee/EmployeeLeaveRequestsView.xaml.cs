using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Attendify.Views.Employee
{
    public partial class EmployeeLeaveRequestsView : UserControl
    {
        public EmployeeLeaveRequestsView()
        {
            InitializeComponent();
            Loaded += EmployeeLeaveRequestsView_Loaded;
        }

        private void EmployeeLeaveRequestsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLeaveHistory();
        }

        private void LoadLeaveHistory()
        {
            var leaveHistory = new List<LeaveRequest>
            {
                new LeaveRequest
                {
                    FromDate = "Jan 13",
                    ToDate = "Jan 15",
                    Type = "Sick",
                    Status = "Approved",
                    StatusColor = new SolidColorBrush(Color.FromRgb(56, 176, 0)),
                    AdminResponse = "Take rest",
                    CanCancel = false,
                    EmployeeMessage = "Not feeling well, need rest",
                    DetailedReason = "Having fever and cold symptoms, doctor advised 3 days rest"
                },
                new LeaveRequest
                {
                    FromDate = "Jan 18",
                    ToDate = "Jan 18",
                    Type = "Emergency",
                    Status = "Rejected",
                    StatusColor = new SolidColorBrush(Color.FromRgb(255, 107, 107)),
                    AdminResponse = "Provide medical note",
                    CanCancel = false,
                    EmployeeMessage = "Family emergency",
                    DetailedReason = "Need to attend to urgent family matter"
                },
                new LeaveRequest
                {
                    FromDate = "Jan 20",
                    ToDate = "Jan 20",
                    Type = "Personal",
                    Status = "Pending",
                    StatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    AdminResponse = "-",
                    CanCancel = true,
                    EmployeeMessage = "Personal administration tasks",
                    DetailedReason = "I need time for personal administration tasks and appointments"
                }
            };

            LeaveHistoryGrid.ItemsSource = leaveHistory;
        }

        private void RequestLeave_Click(object sender, RoutedEventArgs e)
        {
            // Reset form
            DatePickerFrom.SelectedDate = DateTime.Today;
            DatePickerTo.SelectedDate = DateTime.Today;
            CmbLeaveType.SelectedIndex = 0;
            TxtReason.Text = "";
            TxtDetailedReason.Text = "";

            // Show overlay
            NewRequestOverlay.Visibility = Visibility.Visible;
            MainScrollViewer.IsEnabled = false;
        }

        private void CancelNewRequest_Click(object sender, RoutedEventArgs e)
        {
            NewRequestOverlay.Visibility = Visibility.Collapsed;
            MainScrollViewer.IsEnabled = true;
        }

        private void SubmitRequest_Click(object sender, RoutedEventArgs e)
        {
            if (DatePickerFrom.SelectedDate == null || DatePickerTo.SelectedDate == null)
            {
                MessageBox.Show("Please select both from and to dates.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtReason.Text))
            {
                MessageBox.Show("Please provide a reason for your leave.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create new leave request
            var newRequest = new LeaveRequest
            {
                FromDate = DatePickerFrom.SelectedDate.Value.ToString("MMM dd"),
                ToDate = DatePickerTo.SelectedDate.Value.ToString("MMM dd"),
                Type = (CmbLeaveType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Other",
                Status = "Pending",
                StatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                AdminResponse = "-",
                CanCancel = true,
                EmployeeMessage = TxtReason.Text,
                DetailedReason = TxtDetailedReason.Text
            };

            // Add to history
            var history = LeaveHistoryGrid.ItemsSource as List<LeaveRequest> ?? new List<LeaveRequest>();
            history.Insert(0, newRequest);
            LeaveHistoryGrid.ItemsSource = null;
            LeaveHistoryGrid.ItemsSource = history;

            // Close overlay
            NewRequestOverlay.Visibility = Visibility.Collapsed;
            MainScrollViewer.IsEnabled = true;

            MessageBox.Show("Leave request submitted successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LeaveRequest request)
            {
                // Populate details
                DetailFromDate.Text = request.FromDate;
                DetailToDate.Text = request.ToDate;
                DetailType.Text = request.Type;
                DetailMessage.Text = request.DetailedReason;
                DetailAdminResponse.Text = request.AdminResponse;

                // Show overlay
                DetailsOverlay.Visibility = Visibility.Visible;
                MainScrollViewer.IsEnabled = false;
            }
        }

        private void CloseDetails_Click(object sender, RoutedEventArgs e)
        {
            DetailsOverlay.Visibility = Visibility.Collapsed;
            MainScrollViewer.IsEnabled = true;
        }

        private void CancelRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LeaveRequest request)
            {
                var result = MessageBox.Show("Are you sure you want to cancel this leave request?", "Cancel Request",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Remove from history
                    var history = LeaveHistoryGrid.ItemsSource as List<LeaveRequest>;
                    history?.Remove(request);
                    LeaveHistoryGrid.ItemsSource = null;
                    LeaveHistoryGrid.ItemsSource = history;

                    MessageBox.Show("Leave request cancelled successfully!", "Cancelled",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }

    public class LeaveRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public Brush StatusColor { get; set; }
        public string AdminResponse { get; set; }
        public bool CanCancel { get; set; }
        public string EmployeeMessage { get; set; }
        public string DetailedReason { get; set; }
    }
}