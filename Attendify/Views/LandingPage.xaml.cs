using Attendify.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Windows.Media.Animation;
using System;


namespace Attendify.Views
{
    public partial class LandingPage : Window
    {
        public LandingPage()
        {
            InitializeComponent();
        }

        // --- Login Button Hover In ---
        private void LoginButton_MouseEnter(object sender, MouseEventArgs e)
        {
            var anim = new DoubleAnimation
            {
                To = 1.1,
                Duration = TimeSpan.FromMilliseconds(150),
                AccelerationRatio = 0.3
            };

            LoginButton.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            LoginButton.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
        }

        // --- Login Button Hover Out ---
        private void LoginButton_MouseLeave(object sender, MouseEventArgs e)
        {
            var anim = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(150),
                DecelerationRatio = 0.3
            };

            LoginButton.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            LoginButton.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
        }

        // --- Go to Login Page ---
        private void Login_Click(object sender, MouseButtonEventArgs e)
        {
            LoginPage login = new LoginPage();
            login.Show();
            this.Close();
        }

        // --- Learn More Click ---
        private void LearnMore_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("More features coming soon!");
        }

        // --- Custom Close Button ---
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
