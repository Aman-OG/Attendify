using Attendify.ViewModels;
using Attendify.Views.Admin;
using Attendify.Views.UserControls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Attendify.Views
{
    public partial class AdminDashboard : Window
    {
        private DispatcherTimer _timer;
        private Button _currentSelectedButton;
        private AdminDashboardViewModel _viewModel;

        public AdminDashboard()
        {
            InitializeComponent();

            // Initialize ViewModel
            _viewModel = new AdminDashboardViewModel();
            DataContext = _viewModel;

            // Set profile initial
            ProfileInitial.Text = (ProfileName.Text.Length > 0) ? ProfileName.Text.Substring(0, 1).ToUpper() : "A";

            // Initialize timer for clock & shift
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Loaded += AdminDashboard_Loaded;
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

        private void AccountBtn_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var miSettings = new MenuItem { Header = "Settings" };
            miSettings.Click += (s, ev) =>
            {
                SetSelectedButton(BtnSettings);
                ShowSettingsView();
            };
            var miLogout = new MenuItem { Header = "Log out" };
            miLogout.Click += (s, ev) => Application.Current.Shutdown();
            menu.Items.Add(miSettings);
            menu.Items.Add(miLogout);
            menu.PlacementTarget = AccountBtn;
            menu.IsOpen = true;
        }
    }
}