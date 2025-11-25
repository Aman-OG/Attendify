using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Attendify.Views.UserControls
{
    public partial class AttendanceView : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<AttendanceRecord> _attendanceRecords;
        private string _currentStatusFilter = "All";

        public ObservableCollection<AttendanceRecord> AttendanceRecords
        {
            get => _attendanceRecords;
            set
            {
                _attendanceRecords = value;
                OnPropertyChanged();
                UpdateStatusCounts();
            }
        }

        public AttendanceView()
        {
            InitializeComponent();
            DataContext = this;
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            AttendanceRecords = new ObservableCollection<AttendanceRecord>
            {
                new AttendanceRecord {
                    EmployeeID = "EMP10001",
                    FirstName = "Aman",
                    LastName = "Baye",
                    Department = "HR",
                    Position = "Manager",
                    Status = "Present",
                    StatusColor = Brushes.Green
                },
                new AttendanceRecord {
                    EmployeeID = "EMP10002",
                    FirstName = "Markos",
                    LastName = "Neby",
                    Department = "Software",
                    Position = "Developer",
                    Status = "Late",
                    StatusColor = Brushes.Orange
                },
                new AttendanceRecord {
                    EmployeeID = "EMP10003",
                    FirstName = "Teddy",
                    LastName = "Smith",
                    Department = "Electrical",
                    Position = "Engineer",
                    Status = "On Leave",
                    StatusColor = Brushes.Blue
                },
                new AttendanceRecord {
                    EmployeeID = "EMP10004",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Department = "Chemical",
                    Position = "Analyst",
                    Status = "Present",
                    StatusColor = Brushes.Green
                },
                new AttendanceRecord {
                    EmployeeID = "EMP10005",
                    FirstName = "Mike",
                    LastName = "Brown",
                    Department = "Finance",
                    Position = "Accountant",
                    Status = "Present",
                    StatusColor = Brushes.Green
                },
                new AttendanceRecord {
                    EmployeeID = "EMP10006",
                    FirstName = "Emily",
                    LastName = "Davis",
                    Department = "Software",
                    Position = "Developer",
                    Status = "Late",
                    StatusColor = Brushes.Orange
                },
                new AttendanceRecord {
                    EmployeeID = "EMP10007",
                    FirstName = "David",
                    LastName = "Wilson",
                    Department = "HR",
                    Position = "Assistant",
                    Status = "On Leave",
                    StatusColor = Brushes.Blue
                },
                new AttendanceRecord {
                    EmployeeID = "EMP10008",
                    FirstName = "Lisa",
                    LastName = "Anderson",
                    Department = "Marketing",
                    Position = "Coordinator",
                    Status = "Absent",
                    StatusColor = Brushes.Red
                },
                new AttendanceRecord {
                    EmployeeID = "EMP10009",
                    FirstName = "James",
                    LastName = "Miller",
                    Department = "Software",
                    Position = "Tester",
                    Status = "Absent",
                    StatusColor = Brushes.Red
                }
            };

            AttendanceGrid.ItemsSource = AttendanceRecords;
            UpdateStatusCounts();
        }

        private void UpdateStatusCounts()
        {
            if (AttendanceRecords == null) return;

            var presentCount = AttendanceRecords.Count(r => r.Status == "Present");
            var lateCount = AttendanceRecords.Count(r => r.Status == "Late");
            var onLeaveCount = AttendanceRecords.Count(r => r.Status == "On Leave");
            var absentCount = AttendanceRecords.Count(r => r.Status == "Absent");

            PresentCount.Text = presentCount.ToString();
            LateCount.Text = lateCount.ToString();
            OnLeaveCount.Text = onLeaveCount.ToString();
            AbsentCount.Text = absentCount.ToString();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchPlaceholder != null)
                SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            ApplyFilters();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (AttendanceRecords == null) return;

            var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(AttendanceRecords);

            collectionView.Filter = item =>
            {
                var record = item as AttendanceRecord;
                if (record == null) return false;

                try
                {
                    // Search filter
                    var searchText = SearchBox?.Text?.ToLower() ?? "";
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        var matchesSearch = (record.FirstName?.ToLower().Contains(searchText) == true) ||
                                          (record.LastName?.ToLower().Contains(searchText) == true) ||
                                          (record.EmployeeID?.ToLower().Contains(searchText) == true) ||
                                          (record.Department?.ToLower().Contains(searchText) == true);
                        if (!matchesSearch) return false;
                    }

                    // Department filter
                    var departmentFilterItem = DepartmentFilter?.SelectedItem as ComboBoxItem;
                    var departmentFilter = departmentFilterItem?.Content?.ToString();

                    if (!string.IsNullOrEmpty(departmentFilter) &&
                        departmentFilter != "Department" &&
                        departmentFilter != "All Departments" &&
                        departmentFilter != record.Department)
                        return false;

                    // Position filter
                    var positionFilterItem = PositionFilter?.SelectedItem as ComboBoxItem;
                    var positionFilter = positionFilterItem?.Content?.ToString();

                    if (!string.IsNullOrEmpty(positionFilter) &&
                        positionFilter != "Position" &&
                        positionFilter != "All Positions" &&
                        positionFilter != record.Position)
                        return false;

                    // Status filter
                    var statusFilterItem = StatusFilter?.SelectedItem as ComboBoxItem;
                    var statusFilter = statusFilterItem?.Content?.ToString();

                    if (!string.IsNullOrEmpty(statusFilter) &&
                        statusFilter != "Status" &&
                        statusFilter != "All Status" &&
                        statusFilter != record.Status)
                        return false;

                    // Status card filter
                    if (_currentStatusFilter != "All" && _currentStatusFilter != record.Status)
                        return false;

                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Filter error: {ex.Message}");
                    return true;
                }
            };
        }

        private void StatusCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Reset all cards
            ResetStatusCards();

            var card = sender as Border;
            if (card != null)
            {
                // Highlight selected card
                card.Background = card.Background.ToString().Contains("40") ?
                    card.Background :
                    new SolidColorBrush(Color.FromArgb(0x60, 0x00, 0xA6, 0xFB));

                // Set filter based on card
                if (card == PresentCard)
                    _currentStatusFilter = "Present";
                else if (card == LateCard)
                    _currentStatusFilter = "Late";
                else if (card == OnLeaveCard)
                    _currentStatusFilter = "On Leave";
                else if (card == AbsentCard)
                    _currentStatusFilter = "Absent";
                else
                    _currentStatusFilter = "All";
            }

            ApplyFilters();
        }

        private void ResetStatusCards()
        {
            PresentCard.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x28, 0xA7, 0x45));
            LateCard.Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xC1, 0x07));
            OnLeaveCard.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x7B, 0xFF));
            AbsentCard.Background = new SolidColorBrush(Color.FromArgb(0x20, 0xDC, 0x35, 0x45));
            _currentStatusFilter = "All";
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Clear all filters
            SearchBox.Text = "";
            DepartmentFilter.SelectedIndex = 0;
            PositionFilter.SelectedIndex = 0;
            StatusFilter.SelectedIndex = 0;
            ResetStatusCards();

            // Refresh data (in real app, this would fetch from database)
            ApplyFilters();

            // Show refresh feedback
            var animation = new System.Windows.Media.Animation.DoubleAnimation(360, 0,
                new System.Windows.Duration(TimeSpan.FromSeconds(0.5)));
            ((Button)sender).RenderTransform = new RotateTransform();
            ((Button)sender).RenderTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AttendanceRecord
    {
        public string EmployeeID { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string Status { get; set; } = "";
        public Brush StatusColor { get; set; } = Brushes.White;
    }
}