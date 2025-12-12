using Attendify.ViewModels;
using Attendify.Views.UserControls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Attendify.Views.Employee
{
    public partial class EmployeeDashboard : Window
    {
        private DispatcherTimer _timer;
        private Button _currentSelectedButton;
        private EmployeeDashboardViewModel _viewModel;
        private EmployeeInfo _currentEmployee;

        private static readonly string[] _profileColors =
        {
            "#D93A3A", // Red
            "#3A7BD9", // Blue
            "#3AD952", // Green
            "#D9A63A", // Orange
            "#8A3AD9", // Purple
            "#D93A99", // Pink
            "#3AD9C4", // Teal
            "#D9783A"  // Brown
        };

        // EmployeeInfo class (same as in AdminDashboard but in this namespace)
        public class EmployeeInfo
        {
            public int EmployeeID { get; set; }
            public string EmpCode { get; set; } = null!;
            public string FirstName { get; set; } = null!;
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!;
        }

        // Default constructor
        public EmployeeDashboard()
        {
            InitializeComponent();
            InitializeDashboard(null);
        }

        // Constructor with employee data from this class
        public EmployeeDashboard(EmployeeInfo employee)
        {
            InitializeComponent();
            InitializeDashboard(employee);
        }

        // Constructor with employee data from AdminDashboard
        public EmployeeDashboard(Attendify.Views.AdminDashboard.EmployeeInfo adminEmployee)
        {
            InitializeComponent();

            // Convert AdminDashboard.EmployeeInfo to this EmployeeInfo
            var employeeInfo = new EmployeeInfo
            {
                EmployeeID = adminEmployee.EmployeeID,
                EmpCode = adminEmployee.EmpCode,
                FirstName = adminEmployee.FirstName,
                MiddleName = adminEmployee.MiddleName,
                LastName = adminEmployee.LastName,
                Department = adminEmployee.Department,
                Position = adminEmployee.Position,
                Email = adminEmployee.Email,
                Role = adminEmployee.Role
            };

            InitializeDashboard(employeeInfo);
        }

        private void InitializeDashboard(EmployeeInfo employee)
        {
            try
            {
                _currentEmployee = employee;

                // Initialize ViewModel
                _viewModel = new EmployeeDashboardViewModel();
                DataContext = _viewModel;

                // Set profile information
                if (_currentEmployee != null)
                {
                    // Build full name: First Name + Middle Name (skip Last Name)
                    string fullName = _currentEmployee.FirstName;

                    if (!string.IsNullOrWhiteSpace(_currentEmployee.MiddleName))
                    {
                        fullName += " " + _currentEmployee.MiddleName;
                    }

                    fullName = fullName.Trim();
                    ProfileName.Text = string.IsNullOrEmpty(fullName) ? "Employee" : fullName;

                    // Set profile initial
                    string initial = _currentEmployee.FirstName?.Length > 0
                        ? _currentEmployee.FirstName.Substring(0, 1).ToUpper()
                        : (_currentEmployee.MiddleName?.Length > 0 ? _currentEmployee.MiddleName.Substring(0, 1).ToUpper() : "E");
                    ProfileInitial.Text = initial;

                    // Set profile color
                    SetProfileColor(initial);

                    // Update account button to show role
                    AccountBtn.Content = $"{_currentEmployee.Role} ▾";

                    // Update window title
                    this.Title = $"Employee Dashboard - {fullName}";

                    // Update ViewModel with employee data
                    _viewModel.EmployeeName = _currentEmployee.FirstName;
                    _viewModel.Department = _currentEmployee.Department ?? "";
                    _viewModel.Position = _currentEmployee.Position ?? "";
                    _viewModel.FullName = fullName;
                }
                else
                {
                    // Fallback values
                    ProfileInitial.Text = "E";
                    ProfileName.Text = "Employee";
                    SetProfileColor("E");
                    AccountBtn.Content = "Employee ▾";
                }

                // Initialize timer for clock & shift
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += Timer_Tick;
                _timer.Start();

                Loaded += EmployeeDashboard_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing dashboard: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetProfileColor(string initial)
        {
            // Create a hash from the initial to get consistent color
            int hash = 0;
            if (!string.IsNullOrEmpty(initial))
            {
                foreach (char c in initial)
                {
                    hash = (hash * 31 + c) % _profileColors.Length;
                }
            }

            // Ensure positive index
            int colorIndex = Math.Abs(hash) % _profileColors.Length;

            try
            {
                Color color = (Color)ColorConverter.ConvertFromString(_profileColors[colorIndex]);
                ProfileInitial.Background = new SolidColorBrush(color);
            }
            catch
            {
                // Fallback to accent blue if color conversion fails
                ProfileInitial.Background = new SolidColorBrush(Color.FromRgb(0, 166, 251));
            }
        }

        private void EmployeeDashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Position indicator next to first button and set it as selected
            SetSelectedButton(BtnHome);
            ShowHomeView();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            _viewModel.CurrentDate = now.ToString("dd-MM-yyyy");
            _viewModel.CurrentTime = now.ToString("HH : mm : ss");

            int h = now.Hour;
            if (h >= 6 && h < 14) _viewModel.CurrentShift = "Morning Shift";
            else if (h >= 14 && h < 22) _viewModel.CurrentShift = "Afternoon Shift";
            else _viewModel.CurrentShift = "Night Shift";
        }

        private void SetSelectedButton(Button btn)
        {
            if (btn == null) return;

            // Clear previous selection
            ClearSidebarButtonHighlights();

            // Set new selection
            _currentSelectedButton = btn;
            btn.Background = new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0xA6, 0xFB));
            btn.FontWeight = FontWeights.Bold;

            // Find the TextBlock inside the button and increase font size
            if (btn.Content is StackPanel stackPanel)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is TextBlock textBlock)
                    {
                        textBlock.FontSize = 22;
                        textBlock.FontWeight = FontWeights.Bold;
                    }
                }
            }

            // Move indicator
            MoveIndicatorToButton(btn);
        }

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
        }

        private void ClearSidebarButtonHighlights()
        {
            foreach (var c in SidebarButtonsPanel.Children)
            {
                if (c is Button b)
                {
                    b.Background = Brushes.Transparent;
                    b.FontWeight = FontWeights.Normal;

                    if (b.Content is StackPanel stackPanel)
                    {
                        foreach (var child in stackPanel.Children)
                        {
                            if (child is TextBlock textBlock)
                            {
                                textBlock.FontSize = 20;
                                textBlock.FontWeight = FontWeights.Normal;
                            }
                        }
                    }
                }
            }
        }

        // Navigation Methods
        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnHome);
            ShowHomeView();
        }

        private void BtnAttendance_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnAttendance);
            ShowAttendanceView();
        }

        private void BtnLeaveRequests_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnLeaveRequests);
            ShowLeaveRequestsView();
        }

        private void BtnShifts_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnShifts);
            ShowShiftsView();
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnReports);
            ShowReportsView();
        }

        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnNotifications);
            ShowNotificationsView();
        }

        // View Switching Methods
        private void ShowHomeView()
        {
            _viewModel.CurrentPageTitle = "Dashboard Overview";
            MainContentControl.Content = new EmployeeHomeView();
        }

        private void ShowAttendanceView()
        {
            _viewModel.CurrentPageTitle = "My Attendance";
            MainContentControl.Content = new EmployeeAttendanceView();
        }

        private void ShowLeaveRequestsView()
        {
            _viewModel.CurrentPageTitle = "Leave Requests";
            MainContentControl.Content = new EmployeeLeaveRequestsView();
        }

        private void ShowShiftsView()
        {
            _viewModel.CurrentPageTitle = "My Shifts";
            MainContentControl.Content = new EmployeeShiftsView();
        }

        private void ShowReportsView()
        {
            _viewModel.CurrentPageTitle = "My Reports";
            MainContentControl.Content = new EmployeeReportsView();
        }

        private void ShowNotificationsView()
        {
            _viewModel.CurrentPageTitle = "Notifications";
            MainContentControl.Content = new EmployeeNotificationsView();
        }

        private void AccountBtn_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            // Add employee info at the top if available
            if (_currentEmployee != null)
            {
                // Build full name: First Name + Middle Name
                string fullName = _currentEmployee.FirstName;
                if (!string.IsNullOrWhiteSpace(_currentEmployee.MiddleName))
                {
                    fullName += " " + _currentEmployee.MiddleName;
                }
                fullName = fullName.Trim();

                var infoHeader = new MenuItem
                {
                    Header = fullName,
                    FontWeight = FontWeights.Bold,
                    IsEnabled = false
                };

                var roleInfo = new MenuItem
                {
                    Header = $"Role: {_currentEmployee.Role}",
                    IsEnabled = false,
                    FontSize = 12
                };

                var deptInfo = new MenuItem
                {
                    Header = $"Department: {_currentEmployee.Department}",
                    IsEnabled = false,
                    FontSize = 12
                };

                menu.Items.Add(infoHeader);
                menu.Items.Add(roleInfo);
                menu.Items.Add(deptInfo);
                menu.Items.Add(new Separator());
            }

            var miProfile = new MenuItem { Header = "My Profile" };
            miProfile.Click += (s, ev) =>
            {
                MessageBox.Show("Profile feature coming soon!", "Profile",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            };

            var miSettings = new MenuItem { Header = "Settings" };
            miSettings.Click += (s, ev) =>
            {
                MessageBox.Show("Settings feature coming soon!", "Settings",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            };

            menu.Items.Add(miProfile);
            menu.Items.Add(miSettings);
            menu.Items.Add(new Separator());

            var miLogout = new MenuItem { Header = "Log out" };
            miLogout.Click += (s, ev) =>
            {
                var result = MessageBox.Show("Are you sure you want to logout?", "Logout Confirmation",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Go back to login page
                    LoginPage loginPage = new LoginPage();
                    loginPage.Show();
                    this.Close();
                }
            };
            menu.Items.Add(miLogout);

            menu.PlacementTarget = AccountBtn;
            menu.IsOpen = true;
        }

        // Public method to set employee data after creation
        public void SetEmployeeData(EmployeeInfo employee)
        {
            InitializeDashboard(employee);
        }
    }
}