using Microsoft.Extensions.DependencyInjection;
using Capturer.Services;
using Capturer.Models;

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
            var services = new ServiceCollection();

            // Register core services
            services.AddSingleton<IConfigurationManager, ConfigurationManager>();
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
            
            // Register FTP service
            services.AddSingleton<IFtpService, FtpService>();
            
            // Register email service with dependencies
            services.AddSingleton<IEmailService>(provider => 
                new EmailService(
                    provider.GetRequiredService<IConfigurationManager>(),
                    provider.GetRequiredService<IFileService>(),
                    provider.GetService<IQuadrantService>(),
                    provider.GetService<ActivityReportService>()));

            return services.BuildServiceProvider();
        }
    }
}