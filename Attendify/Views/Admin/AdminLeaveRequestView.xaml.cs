using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Attendify.Views.UserControls
{
    public partial class LeaveRequestsView : UserControl
    {
        private ObservableCollection<LeaveRequest> _leaveRequests;
        private LeaveRequest _selectedRequest;

        public LeaveRequestsView()
        {
            InitializeComponent();
            LoadSampleData();
            SetActiveFilter(BtnAll);
        }

        private void LoadSampleData()
        {
            _leaveRequests = new ObservableCollection<LeaveRequest>
            {
                new LeaveRequest
                {
                    No = "1",
                    EmployeeName = "Aman Baye",
                    EmpId = "emp10002",
                    Department = "HR",
                    Position = "Manager",
                    Email = "am@gmail.com",
                    FromDate = "12/05/25",
                    ToDate = "13/05/25",
                    Reason = "Sick leave",
                    Status = "Approved",
                    StatusColor = Brushes.Green
                },
                new LeaveRequest
                {
                    No = "2",
                    EmployeeName = "Markos Neby",
                    EmpId = "emp1500975",
                    Department = "HR",
                    Position = "Manager",
                    Email = "markos@gmail.com",
                    FromDate = "12/05/25",
                    ToDate = "16/05/25",
                    Reason = "Family Trip",
                    Status = "Pending",
                    StatusColor = Brushes.Orange
                },
                new LeaveRequest
                {
                    No = "3",
                    EmployeeName = "Teddy K",
                    EmpId = "emp10003",
                    Department = "Software",
                    Position = "Developer",
                    Email = "teddy@g.com",
                    FromDate = "15/05/25",
                    ToDate = "18/05/25",
                    Reason = "Vacation",
                    Status = "Pending",
                    StatusColor = Brushes.Orange
                },
                new LeaveRequest
                {
                    No = "4",
                    EmployeeName = "Sarah Johnson",
                    EmpId = "emp10004",
                    Department = "Electrical",
                    Position = "Engineer",
                    Email = "sarah@email.com",
                    FromDate = "10/05/25",
                    ToDate = "11/05/25",
                    Reason = "Medical appointment",
                    Status = "Rejected",
                    StatusColor = Brushes.Red
                }
            };

            LeaveRequestsGrid.ItemsSource = _leaveRequests;
        }

        private void SetActiveFilter(Button activeButton)
        {
            // Reset all buttons
            BtnAll.Background = Brushes.Transparent;
            BtnToday.Background = Brushes.Transparent;
            BtnApproved.Background = Brushes.Transparent;
            BtnPending.Background = Brushes.Transparent;
            BtnRejected.Background = Brushes.Transparent;

            // Set active button
            activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4000A6FB"));
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                SetActiveFilter(button);
                // Implement filter logic based on button content
                ApplyFilter(button.Content.ToString());
            }
        }

        private void ApplyFilter(string filter)
        {
            // Simple filter implementation - you can enhance this
            var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(_leaveRequests);

            if (filter == "All")
            {
                collectionView.Filter = null;
            }
            else
            {
                collectionView.Filter = item =>
                {
                    var request = item as LeaveRequest;
                    return request?.Status == filter;
                };
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

            // Implement search logic
            var searchText = SearchBox.Text.ToLower();
            var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(_leaveRequests);

            if (string.IsNullOrEmpty(searchText))
            {
                collectionView.Filter = null;
            }
            else
            {
                collectionView.Filter = item =>
                {
                    var request = item as LeaveRequest;
                    return request?.EmployeeName.ToLower().Contains(searchText) == true ||
                           request?.EmpId.ToLower().Contains(searchText) == true ||
                           request?.Department.ToLower().Contains(searchText) == true;
                };
            }
        }

        private void LeaveRequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
            DetailStatus.Text = request.Status;
            DetailStatusBorder.Background = request.StatusColor;

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

        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest != null)
            {
                _selectedRequest.Status = "Approved";
                _selectedRequest.StatusColor = Brushes.Green;

                // Refresh the display
                LeaveRequestsGrid.Items.Refresh();
                ShowDetailedView(_selectedRequest);

                MessageBox.Show("Leave request approved successfully!", "Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Refresh button click handler
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Implement refresh logic here
            RefreshData();

            // Optional: Show a brief visual feedback
            var button = sender as Button;
            if (button != null)
            {
                button.Content = "⏳";
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, e) =>
                {
                    button.Content = "🔄";
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void RefreshData()
        {
            // Implement your data refresh logic here
            // For example: reload data from database, update DataGrid, etc.

            // Clear selection


            // Show refresh message (optional)
            // MessageBox.Show("Data refreshed successfully!", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest != null)
            {
                _selectedRequest.Status = "Rejected";
                _selectedRequest.StatusColor = Brushes.Red;

                // Refresh the display
                LeaveRequestsGrid.Items.Refresh();
                ShowDetailedView(_selectedRequest);

                MessageBox.Show("Leave request rejected.", "Rejected",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class LeaveRequest
    {
        public string No { get; set; }
        public string EmployeeName { get; set; }
        public string EmpId { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public Brush StatusColor { get; set; }
    }
}