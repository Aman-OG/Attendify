using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Windows;
using Attendify.DATA;  

namespace Attendify
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Trigger time synchronization early
            _ = Attendify.Services.TimeService.Instance.SyncWithServerAsync();

            // Build configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Register services
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

            // Add other services if needed
            ServiceProvider = services.BuildServiceProvider();

            // Test connection (optional)
            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();  // Or migrate if needed
        }
    }
}