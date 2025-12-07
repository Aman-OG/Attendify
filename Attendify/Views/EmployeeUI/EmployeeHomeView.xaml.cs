using System.Windows;
using System.Windows.Controls;

namespace Attendify.Views.Employee
{
    public partial class EmployeeHomeView : UserControl
    {
        public EmployeeHomeView()
        {
            InitializeComponent();
        }

        private void ClockInOut_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement clock in/out functionality
            MessageBox.Show("Clock In/Out functionality coming soon!", "Clock Action",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RequestLeave_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to leave requests or open leave request dialog
            MessageBox.Show("Leave request functionality coming soon!", "Leave Request",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewSchedule_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to shifts view or open schedule dialog
            MessageBox.Show("Schedule view functionality coming soon!", "View Schedule",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}