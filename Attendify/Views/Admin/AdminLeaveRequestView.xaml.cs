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
                    Description = "Additional details about the leave request would appear here. This could include specific reasons, emergency contact information",
                    Status = "Approved",
                    StatusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2FBF4C"))
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
                    Description = "Additional details about the leave request would appear here. This could include specific reasons, emergency contact information",
                    Status = "Pending",
                    StatusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3C63A"))
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
                    Description = "Additional details about the leave request would appear here. This could include specific reasons, emergency contact information",
                    Status = "Pending",
                    StatusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3C63A"))
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
                    Description = "Additional details about the leave request would appear here. This could include specific reasons, emergency contact information",
                    Status = "Rejected",
                    StatusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D23C3C"))
                }
            };

            LeaveRequestsGrid.ItemsSource = _leaveRequests;
        }

        private void SetActiveFilter(Button activeButton)
        {
            // Reset all buttons to their original colors
            BtnAll.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4000A6FB"));
            BtnToday.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A95315"));
            BtnApproved.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2FBF4C"));
            BtnPending.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3C63A"));
            BtnRejected.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D23C3C"));

            // Highlight the active button with a brighter color
            switch (activeButton.Name)
            {
                case "BtnAll":
                    activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6000A6FB"));
                    break;
                case "BtnToday":
                    activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C9752A"));
                    break;
                case "BtnApproved":
                    activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CD96C"));
                    break;
                case "BtnPending":
                    activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5D755"));
                    break;
                case "BtnRejected":
                    activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E05A5A"));
                    break;
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                SetActiveFilter(button);
                ApplyFilter(button.Content.ToString());
            }
        }

        private void ApplyFilter(string filter)
        {
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
            DetailDescription.Text = request.Description;
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
                _selectedRequest.StatusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2FBF4C"));

                // Refresh the display
                LeaveRequestsGrid.Items.Refresh();
                ShowDetailedView(_selectedRequest);

                MessageBox.Show("Leave request approved successfully!", "Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Clear selection
            LeaveRequestsGrid.SelectedItem = null;
            HideDetailedView();

            // Visual feedback
            var button = sender as Button;
            if (button != null)
            {
                button.Content = "⏳";
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

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest != null)
            {
                _selectedRequest.Status = "Rejected";
                _selectedRequest.StatusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D23C3C"));

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
        public string Description { get; set; }
        public string Status { get; set; }
        public Brush StatusColor { get; set; }
    }
}