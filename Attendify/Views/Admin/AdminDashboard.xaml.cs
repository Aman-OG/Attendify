using Attendify.Views;
using Attendify.ViewModels;
using Attendify.Views.UserControls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Attendify.Models;

namespace Attendify.Views
{
    public partial class AdminDashboard : Window
    {
        private DispatcherTimer _timer;
        private Button _currentSelectedButton;
        private AdminDashboardViewModel _viewModel;
        private EmployeeInfo _currentEmployee; // Changed from EmployeeData to EmployeeInfo

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

        // EmployeeInfo class - NOT nested inside AdminDashboard
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

        // Default constructor (for backward compatibility)
        public AdminDashboard()
        {
            InitializeComponent();
            InitializeDashboard(null);
        }

        // New constructor with employee data
        public AdminDashboard(EmployeeInfo employee)
        {
            InitializeComponent();
            InitializeDashboard(employee);
        }

        private void InitializeDashboard(EmployeeInfo employee)
        {
            try
            {
                _currentEmployee = employee;

                // Initialize ViewModel
                _viewModel = new AdminDashboardViewModel();
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
                    ProfileName.Text = string.IsNullOrEmpty(fullName) ? "Administrator" : fullName;

                    // Set profile initial (use first letter of first name)
                    string initial = _currentEmployee.FirstName?.Length > 0
                        ? _currentEmployee.FirstName.Substring(0, 1).ToUpper()
                        : (_currentEmployee.MiddleName?.Length > 0 ? _currentEmployee.MiddleName.Substring(0, 1).ToUpper() : "A");
                    ProfileInitial.Text = initial;

                    // Set random profile color
                    SetProfileColor(initial);

                    // Update account button to show role
                    AccountBtn.Content = $"{_currentEmployee.Role} ▾";

                    // Update window title
                    this.Title = $"Admin Dashboard - {fullName}";
                }
                else
                {
                    // Fallback values
                    ProfileInitial.Text = "A";
                    ProfileName.Text = "Admin";
                    SetProfileColor("A");
                    AccountBtn.Content = "Admin ▾";
                }
                // Initialize timer for clock & shift
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += Timer_Tick;
                _timer.Start();

                Loaded += AdminDashboard_Loaded;
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
                // Fallback to blue if color conversion fails
                ProfileInitial.Background = new SolidColorBrush(Color.FromRgb(0, 166, 251));
            }
        }

        private void AdminDashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Position indicator next to first button and set it as selected
            SetSelectedButton(BtnAttendance);
            ShowAttendanceView();
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
        private void BtnAttendance_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnAttendance);
            ShowAttendanceView();
        }

        private void BtnLeave_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnLeave);
            ShowLeaveRequestsView();
        }

        private void BtnEmployees_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnEmployees);
            ShowEmployeesView();
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnReports);
            ShowReportsView();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(BtnSettings);
            ShowSettingsView();
        }

        // View Switching Methods
        private void ShowAttendanceView()
        {
            _viewModel.CurrentPageTitle = "Employee Attendance - Day";
            MainContentControl.Content = new AttendanceView();
        }

        private void ShowLeaveRequestsView()
        {
            _viewModel.CurrentPageTitle = "Leave Requests";
            MainContentControl.Content = new LeaveRequestsView();
        }

        private void ShowEmployeesView()
        {
            _viewModel.CurrentPageTitle = "Employees";
            MainContentControl.Content = new EmployeesView();
        }

        private void ShowReportsView()
        {
            _viewModel.CurrentPageTitle = "Reports";
            MainContentControl.Content = new ReportsView();
        }

        private void ShowSettingsView()
        {
            _viewModel.CurrentPageTitle = "Settings";
            MainContentControl.Content = new SettingsView();
        }



        private void ShowChangePasswordView()
        {
            if (_currentEmployee != null && !string.IsNullOrEmpty(_currentEmployee.EmpCode))
            {
                var changePasswordView = new ChangePasswordView(_currentEmployee.EmpCode);

                // Handle events
                changePasswordView.PasswordChanged += (s, e) =>
                {
                    MessageBox.Show("Password changed successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    MainContentControl.Content = null; // Clear the view
                };

                changePasswordView.CancelClicked += (s, e) =>
                {
                    MainContentControl.Content = null; // Clear the view
                };

                MainContentControl.Content = changePasswordView;
            }
            else
            {
                MessageBox.Show("Employee information not available.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                var emailInfo = new MenuItem
                {
                    Header = $"Email: {_currentEmployee.Email}",
                    IsEnabled = false,
                    FontSize = 12
                };

                menu.Items.Add(infoHeader);
                menu.Items.Add(roleInfo);
                menu.Items.Add(emailInfo);
                menu.Items.Add(new Separator());
            }

            var miProfile = new MenuItem { Header = "My Profile" };
            miProfile.Click += (s, ev) =>
            {
                ShowProfileView();
            };

            var miChangePassword = new MenuItem { Header = "Change Password" };
            miChangePassword.Click += (s, ev) =>
            {
                ShowChangePasswordView();
            };
            menu.Items.Add(miProfile);
            menu.Items.Add(miChangePassword);
            menu.Items.Add(new Separator());

            var miLogout = new MenuItem { Header = "Log out" };
            miLogout.Click += (s, ev) =>
            {
                // Go back to login page
                LoginPage loginPage = new LoginPage();
                loginPage.Show();
                this.Close();
            };
            menu.Items.Add(miLogout);

            menu.PlacementTarget = AccountBtn;
            menu.IsOpen = true;
        }
        private void ShowProfileView()
        {
            if (_currentEmployee != null && !string.IsNullOrEmpty(_currentEmployee.EmpCode))
            {
                var profileViewer = new ProfileViewer(_currentEmployee.EmpCode);

                // Handle events
                profileViewer.EditProfileRequested += (s, e) =>
                {
                    MessageBox.Show("Edit profile feature coming soon!", "Edit Profile",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                };

                profileViewer.ChangePasswordRequested += (s, e) =>
                {
                    ShowChangePasswordView();
                };

                profileViewer.CloseRequested += (s, e) =>
                {
                    MainContentControl.Content = null; // Clear the view
                };

                MainContentControl.Content = profileViewer;
            }
            else
            {
                MessageBox.Show("Employee information not available.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}