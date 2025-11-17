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

namespace Attendify.Views
{
    public partial class LandingPage : Window
    {
        public LandingPage()
        {
            InitializeComponent();
        }

        // Hover In Animation
        private void LoginButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var anim = new DoubleAnimation
            {
                To = 1.1,
                Duration = TimeSpan.FromMilliseconds(150),
                AccelerationRatio = 0.3,
            };

            LoginButton.RenderTransform = new ScaleTransform(1, 1);
            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        // Hover Out Animation
        private void LoginButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var anim = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(150),
                DecelerationRatio = 0.3,
            };

            LoginButton.RenderTransform = new ScaleTransform(1, 1);
            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        // Click -> Go to login page
        private void Login_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoginPage login = new LoginPage();
            login.Show();
            this.Close();
        }

        private void LearnMore_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("More info page coming soon!");
        }
    }
}