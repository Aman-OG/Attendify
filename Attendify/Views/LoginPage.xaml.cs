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
using System.Windows;
using System.Windows.Media.Animation;


namespace Attendify
{
    public partial class LoginPage : Window
    {
        public LoginPage()
        {
            InitializeComponent();

            UsernameBox.TextChanged += (s, e) =>
            {
                UserPlaceholder.Visibility =
                    string.IsNullOrWhiteSpace(UsernameBox.Text) ? Visibility.Visible : Visibility.Hidden;
            };

            PasswordBox.PasswordChanged += (s, e) =>
            {
                PassPlaceholder.Visibility =
                    string.IsNullOrWhiteSpace(PasswordBox.Password) ? Visibility.Visible : Visibility.Hidden;
            };
        }
        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UserPlaceholder.Visibility =
                string.IsNullOrEmpty(UsernameBox.Text) ? Visibility.Visible : Visibility.Hidden;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PassPlaceholder.Visibility =
                string.IsNullOrEmpty(PasswordBox.Password) ? Visibility.Visible : Visibility.Hidden;
        }
        private void LoginButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var anim = new DoubleAnimation
            {
                To = 1.08,
                Duration = TimeSpan.FromMilliseconds(150),
                AccelerationRatio = 0.3
            };

            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        private void LoginButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var anim = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(150),
                DecelerationRatio = 0.3
            };

            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            LoginButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }


        private void Login_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Login pressed!");
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Forgot password clicked!");
        }

        private void Admin_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Admin clicked!");
        }
    }
}
