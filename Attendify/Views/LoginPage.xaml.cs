using Attendify.Views;
using Attendify.Views.Employee;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            // Admin login check
            if (UsernameBox.Text == "admin" && PasswordBox.Password == "1234")
            {
                AdminDashboard dashboard = new AdminDashboard();
                dashboard.Show();
                this.Close();
                return;
            }

            // Employee login check
            if (UsernameBox.Text == "emp" && PasswordBox.Password == "123")
            {
                EmployeeDashboard employeeDashboard = new EmployeeDashboard();
                employeeDashboard.Show();
                this.Close();
                return;
            }

            // Invalid credentials
            MessageBox.Show("Invalid username or password!", "Login Failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
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