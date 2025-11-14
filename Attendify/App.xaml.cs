using System.Configuration;
using System.Data;
using System.Windows;
using Attendify.Services;

namespace Attendify
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize database when the app launches
            DatabaseService.InitializeDatabase();
        }
    }
}
