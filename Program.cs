using Microsoft.Extensions.DependencyInjection;
using Capturer.Services;

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
            
            // Register email service with quadrant service dependency
            services.AddSingleton<IEmailService>(provider => 
                new EmailService(
                    provider.GetRequiredService<IConfigurationManager>(),
                    provider.GetRequiredService<IFileService>(),
                    provider.GetService<IQuadrantService>()));

            return services.BuildServiceProvider();
        }
    }
}