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



namespace Attendify.Views
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            StartLoading();
        }

        private async void StartLoading()
        {
            // 3-second loading (0 → 100)
            for (int i = 0; i <= 100; i++)
            {
                LoaderBar.Value = i;
                await Task.Delay(50); // smooth animation
            }

            // After loading complete → Open Login window
            var landing = new LandingPage();
            landing.Show();

            // Fade out animation
            this.Opacity = 1;
            for (double op = 1; op >= 0; op -= 0.05)
            {
                this.Opacity = op;
                await Task.Delay(15);
            }

            this.Close();
        }
    }
}

