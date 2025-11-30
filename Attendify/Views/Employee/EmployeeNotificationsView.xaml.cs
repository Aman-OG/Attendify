using System.Windows;
using System.Windows.Controls;

namespace Attendify.Views.UserControls
{
    public partial class EmployeeNotificationsView : UserControl
    {
        public EmployeeNotificationsView()
        {
            InitializeComponent();
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Showing all notifications", "Filter Applied", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FilterImportant_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Showing important notifications only", "Filter Applied", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FilterMeetings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Showing meeting notifications only", "Filter Applied", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FilterSystem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Showing system update notifications only", "Filter Applied", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FilterCompleted_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Showing completed notifications only", "Filter Applied", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadMore_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Loading more notifications...", "Load More", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}