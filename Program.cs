using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Capturer.Services;
using Capturer.Models;
using IAppConfigurationManager = Capturer.Services.IConfigurationManager;
using AppConfigurationManager = Capturer.Services.ConfigurationManager;

namespace Capturer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            
            // Configure dependency injection
            var serviceProvider = ConfigureServices();
            
            try
            {
                // Run the main form with dependency injection
                var mainForm = new Form1(serviceProvider);
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico de la aplicación: {ex.Message}", "Error Fatal", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                serviceProvider?.Dispose();
            }
        }

        private static ServiceProvider ConfigureServices()
        {
            Console.WriteLine("[DEBUG] ConfigureServices starting...");
            var services = new ServiceCollection();

            // Register core services
            services.AddSingleton<IAppConfigurationManager, AppConfigurationManager>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IScreenshotService, ScreenshotService>();
            services.AddSingleton<IReportPeriodService, ReportPeriodService>();
            services.AddSingleton<ISchedulerService, SchedulerService>();
            
            // Register quadrant system services
            services.AddSingleton<IQuadrantService, QuadrantService>();
            services.AddSingleton<IQuadrantSchedulerService, QuadrantSchedulerService>();
            
            // Register activity report services
            services.AddSingleton<ActivityReportService>(provider =>
                new ActivityReportService(
                    (provider.GetRequiredService<IQuadrantService>() as QuadrantService)?.ActivityService!));
            
            services.AddSingleton<ActivityDashboardSchedulerService>(provider =>
                new ActivityDashboardSchedulerService(
                    provider.GetRequiredService<ActivityReportService>(),
                    (provider.GetRequiredService<IQuadrantService>() as QuadrantService)?.ActivityService!,
                    new CapturerConfiguration(), // Will be loaded by service
                    provider.GetRequiredService<IEmailService>()));
            
            // Register NEW simplified reports scheduler
            services.AddSingleton<SimplifiedReportsSchedulerService>(provider =>
                new SimplifiedReportsSchedulerService(
                    provider.GetRequiredService<ActivityReportService>(),
                    provider.GetRequiredService<IEmailService>()));
            
            // Register FTP service
            services.AddSingleton<IFtpService, FtpService>();
            
            // Register email service with dependencies
            services.AddSingleton<IEmailService>(provider => 
                new EmailService(
                    provider.GetRequiredService<IAppConfigurationManager>(),
                    provider.GetRequiredService<IFileService>(),
                    provider.GetService<IQuadrantService>(),
                    provider.GetService<ActivityReportService>()));

            // ✨ NEW v4.0: Register API service as hosted service
            Console.WriteLine("[DEBUG] Registering CapturerApiService...");
            services.AddSingleton<CapturerApiService>();
            services.AddSingleton<ICapturerApiService>(provider => 
                provider.GetRequiredService<CapturerApiService>());
            services.AddHostedService<CapturerApiService>(provider => 
                provider.GetRequiredService<CapturerApiService>());
            Console.WriteLine("[DEBUG] CapturerApiService registered successfully");

            // Add configuration for API service
            services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(provider =>
            {
                var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string?>("CapturerApi:Enabled", "true"),
                        new KeyValuePair<string, string?>("CapturerApi:Port", "8080"),
                        new KeyValuePair<string, string?>("CapturerApi:DashboardUrl", "http://localhost:5000"),
                        new KeyValuePair<string, string?>("CapturerApi:ApiKey", $"cap_{Guid.NewGuid():N}")
                    });
                return configBuilder.Build();
            });

            // Add logging for API service
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            return services.BuildServiceProvider();
        }
    }
}