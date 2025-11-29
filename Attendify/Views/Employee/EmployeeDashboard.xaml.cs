using Attendify.Views.Employee;
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

        public EmployeeDashboard()
        {
            InitializeComponent();

            // Initialize ViewModel
            _viewModel = new EmployeeDashboardViewModel();
            DataContext = _viewModel;

            // Set profile initial
            ProfileInitial.Text = (ProfileName.Text.Length > 0) ? ProfileName.Text.Substring(0, 1).ToUpper() : "E";

            // Initialize timer for clock & shift
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Loaded += EmployeeDashboard_Loaded;
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

            var miProfile = new MenuItem { Header = "My Profile" };
            miProfile.Click += (s, ev) =>
            {
                // Navigate to profile view
                SetSelectedButton(BtnHome); // Profile is not in sidebar, so reset to home
                ShowHomeView();
            };

            var miSettings = new MenuItem { Header = "Settings" };
            miSettings.Click += (s, ev) =>
            {
                // Could open settings dialog or navigate to settings view
                MessageBox.Show("Settings feature coming soon!", "Settings",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            };

            var miLogout = new MenuItem { Header = "Log out" };
            miLogout.Click += (s, ev) =>
            {
                var result = MessageBox.Show("Are you sure you want to logout?", "Logout Confirmation",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Close this window and show login/main window
                    var mainWindow = new MainWindow(); // Or your login window
                    mainWindow.Show();
                    this.Close();
                }
            };

            menu.Items.Add(miProfile);
            menu.Items.Add(miSettings);
            menu.Items.Add(new Separator());
            menu.Items.Add(miLogout);
            menu.PlacementTarget = AccountBtn;
            menu.IsOpen = true;
        }
    }
}