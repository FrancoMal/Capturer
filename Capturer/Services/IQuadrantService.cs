using Capturer.Models;

namespace Capturer.Services;

public interface IQuadrantService : IDisposable
{
    /// <summary>
    /// Gets all available quadrant configurations
    /// </summary>
    List<QuadrantConfiguration> GetConfigurations();

    /// <summary>
    /// Gets the active quadrant configuration
    /// </summary>
    QuadrantConfiguration? GetActiveConfiguration();

    /// <summary>
    /// Saves a quadrant configuration
    /// </summary>
    Task<bool> SaveConfigurationAsync(QuadrantConfiguration configuration);

    /// <summary>
    /// Deletes a quadrant configuration
    /// </summary>
    Task<bool> DeleteConfigurationAsync(string configurationName);

    /// <summary>
    /// Sets the active configuration
    /// </summary>
    Task<bool> SetActiveConfigurationAsync(string configurationName);

    /// <summary>
    /// Processes images in a date range with progress reporting
    /// </summary>
    Task<ProcessingTask> ProcessImagesAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? configurationName = null,
        IProgress<ProcessingProgress>? progress = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes specific images from a pre-filtered list
    /// </summary>
    Task<ProcessingTask> ProcessSpecificImagesAsync(
        List<string> imagePaths,
        string? configurationName = null,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets processing history
    /// </summary>
    List<ProcessingTask> GetProcessingHistory();

    /// <summary>
    /// Gets available quadrant folders for email selection
    /// </summary>
    List<string> GetAvailableQuadrantFolders();

    /// <summary>
    /// Gets files from specific quadrant folders
    /// </summary>
    Task<List<string>> GetQuadrantFilesAsync(List<string> quadrantNames, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Creates a preview image showing quadrants overlay
    /// </summary>
    Task<System.Drawing.Bitmap?> CreatePreviewImageAsync(string imagePath, string? configurationName = null);

    /// <summary>
    /// Event fired when processing progress updates
    /// </summary>
    event EventHandler<ProcessingProgressEventArgs>? ProcessingProgressChanged;

    /// <summary>
    /// Event fired when processing completes
    /// </summary>
    event EventHandler<ProcessingCompletedEventArgs>? ProcessingCompleted;
}

public class ProcessingProgressEventArgs : EventArgs
{
    public ProcessingProgress Progress { get; set; } = new();
    public ProcessingTask Task { get; set; } = new();
}

public class ProcessingCompletedEventArgs : EventArgs
{
    public ProcessingTask Task { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}