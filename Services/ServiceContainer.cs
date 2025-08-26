using Microsoft.Extensions.DependencyInjection;
using Capturer.Services;

namespace Capturer.Services;

public static class ServiceContainer
{
    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IScreenshotService, ScreenshotService>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<ISchedulerService, SchedulerService>();

        // Register forms - will be added later
        // services.AddTransient<MainForm>();
        // services.AddTransient<SettingsForm>();
        // services.AddTransient<EmailForm>();

        return services.BuildServiceProvider();
    }
}