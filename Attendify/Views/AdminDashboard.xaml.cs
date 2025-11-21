using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Attendify.Views
{
    public partial class AdminDashboard : Window
    {
        private DispatcherTimer _timer;
        private ObservableCollection<EmployeeRow> _rows = new ObservableCollection<EmployeeRow>();

        public AdminDashboard()
        {
            InitializeComponent();

            // sample rows
            _rows.Add(new EmployeeRow { No = "01", EmpID = "emp10002", FirstName = "Aman", LastName = "Baye", Email = "am@gmail.com", Department = "HR", Position = "Manager", Status = "Active" });
            _rows.Add(new EmployeeRow { No = "02", EmpID = "emp10003", FirstName = "Teddy", LastName = "K", Email = "teddy@g.com", Department = "Software", Position = "Dev", Status = "Attended" });

            EmployeesGrid.ItemsSource = _rows;

            ProfileInitial.Text = (ProfileName.Text.Length > 0) ? ProfileName.Text.Substring(0, 1).ToUpper() : "A";

            // timer for clock & shift
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Loaded += AdminDashboard_Loaded;
        }

        private void AdminDashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // position indicator next to first button
            MoveIndicatorToButton(BtnAttendance);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            DateText.Text = now.ToString("dd-MM-yyyy");
            ClockText.Text = now.ToString("HH : mm : ss");

            int h = now.Hour;
            if (h >= 6 && h < 14) ShiftText.Text = "Morning Shift";
            else if (h >= 14 && h < 22) ShiftText.Text = "Afternoon Shift";
            else ShiftText.Text = "Night Shift";
        }

        // move indicator animation to clicked button
        private void MoveIndicatorToButton(Button btn)
        {
            if (btn == null) return;
            var transform = btn.TransformToAncestor(SidebarButtonsPanel);
            var pos = transform.Transform(new Point(0, 0));

            double targetY = pos.Y;

            var anim = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            IndicatorTransform.BeginAnimation(TranslateTransform.YProperty, anim);

            // highlight clicked button (clear others)
            ClearSidebarButtonHighlights();
            btn.Background = new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0xA6, 0xFB));
        }

        private void ClearSidebarButtonHighlights()
        {
            foreach (var c in SidebarButtonsPanel.Children)
            {
                if (c is Button b) b.Background = Brushes.Transparent;
            }
        }

        private void BtnAttendance_Click(object sender, RoutedEventArgs e)
        {
            MoveIndicatorToButton(BtnAttendance);
            ShowPanel("attendance");
        }

        private void BtnLeave_Click(object sender, RoutedEventArgs e)
        {
            MoveIndicatorToButton(BtnLeave);
            ShowPanel("leave");
        }

        private void BtnEmployees_Click(object sender, RoutedEventArgs e)
        {
            MoveIndicatorToButton(BtnEmployees);
            ShowPanel("employees");
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            MoveIndicatorToButton(BtnReports);
            ShowPanel("reports");
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            MoveIndicatorToButton(BtnSettings);
            ShowPanel("settings");
        }

        // stub panel switcher for now (changes header only)
        private void ShowPanel(string name)
        {
            AttendanceTitle.Text = name switch
            {
                "attendance" => "Employee Attendance - Day",
                "leave" => "Leave Requests",
                "employees" => "Employees",
                "reports" => "Reports",
                "settings" => "Settings",
                _ => "Employee Attendance - Day"
            };
        }

        private void AccountBtn_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var miSettings = new MenuItem { Header = "Settings" };
            miSettings.Click += (s, ev) => MessageBox.Show("Open settings");
            var miLogout = new MenuItem { Header = "Log out" };
            miLogout.Click += (s, ev) => Application.Current.Shutdown();
            menu.Items.Add(miSettings);
            menu.Items.Add(miLogout);
            menu.PlacementTarget = AccountBtn;
            menu.IsOpen = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // search placeholder control
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            // you can add filtering logic here to filter _rows and refresh EmployeesGrid.ItemsSource
        }

        private class EmployeeRow
        {
            public string No { get; set; }
            public string EmpID { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Department { get; set; }
            public string Position { get; set; }
            public string Status { get; set; }
        }

  
    }
}
