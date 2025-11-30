using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Attendify.Views.Employee
{
    public partial class EmployeeAttendanceView : UserControl
    {
        private DispatcherTimer _clockTimer;
        private bool _isCheckedIn = false;

        public EmployeeAttendanceView()
        {
            InitializeComponent();
            Loaded += EmployeeAttendanceView_Loaded;
        }

        private void EmployeeAttendanceView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeClock();
            LoadAttendanceHistory();
            UpdateTodayInfo();
        }

        private void InitializeClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            TxtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void UpdateTodayInfo()
        {
            TxtTodayDate.Text = DateTime.Now.ToString("dddd, MMM dd yyyy");
            TxtShiftDetails.Text = "Morning Shift (08:00 – 14:00)";
            TxtGracePeriod.Text = "5 min";
        }

        private void LoadAttendanceHistory()
        {
            var attendanceData = new List<AttendanceRecord>
            {
                new AttendanceRecord { Date = "2025-01-20", Shift = "Morning", CheckIn = "08:02", Status = "On Time", StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)) },
                new AttendanceRecord { Date = "2025-01-19", Shift = "Morning", CheckIn = "08:09", Status = "Late", StatusColor = new SolidColorBrush(Color.FromRgb(255, 107, 107)) },
                new AttendanceRecord { Date = "2025-01-18", Shift = "Afternoon", CheckIn = "13:50", Status = "On Time", StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)) },
                new AttendanceRecord { Date = "2025-01-17", Shift = "Morning", CheckIn = "08:01", Status = "On Time", StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)) },
                new AttendanceRecord { Date = "2025-01-16", Shift = "Morning", CheckIn = "08:15", Status = "Late", StatusColor = new SolidColorBrush(Color.FromRgb(255, 107, 107)) },
                new AttendanceRecord { Date = "2025-01-15", Shift = "Afternoon", CheckIn = "13:45", Status = "On Time", StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)) },
                new AttendanceRecord { Date = "2025-01-14", Shift = "Morning", CheckIn = "07:58", Status = "On Time", StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)) }
            };

            AttendanceHistoryGrid.ItemsSource = attendanceData;
        }

        private void BtnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCheckedIn)
            {
                _isCheckedIn = true;
                BtnCheckIn.IsEnabled = false;
                BtnCheckIn.Content = "CHECKED IN";
                TxtCheckedInTime.Text = $"Checked in at {DateTime.Now:HH:mm}";
                TxtCheckedInTime.Visibility = Visibility.Visible;

                MessageBox.Show($"Successfully checked in at {DateTime.Now:HH:mm:ss}", "Check In Successful",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class AttendanceRecord
    {
        public string Date { get; set; }
        public string Shift { get; set; }
        public string CheckIn { get; set; }
        public string Status { get; set; }
        public Brush StatusColor { get; set; }
    }
}