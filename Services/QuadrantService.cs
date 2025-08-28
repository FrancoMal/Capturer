using System.Drawing;
using Capturer.Models;
using Capturer.Utils;

namespace Capturer.Services;

public class QuadrantService : IQuadrantService, IDisposable
{
    private readonly IConfigurationManager _configManager;
    private readonly IFileService _fileService;
    private CapturerConfiguration _config = new();
    private readonly List<ProcessingTask> _processingHistory = new();
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public event EventHandler<ProcessingProgressEventArgs>? ProcessingProgressChanged;
    public event EventHandler<ProcessingCompletedEventArgs>? ProcessingCompleted;

    public QuadrantService(IConfigurationManager configManager, IFileService fileService)
    {
        _configManager = configManager;
        _fileService = fileService;
        LoadConfigurationAsync().ConfigureAwait(false);
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
            
            // Initialize with default configuration if none exists
            if (!_config.QuadrantSystem.Configurations.Any())
            {
                var defaultConfig = QuadrantConfiguration.CreateDefault(new Size(1920, 1080));
                _config.QuadrantSystem.Configurations.Add(defaultConfig);
                _config.QuadrantSystem.ActiveConfigurationName = defaultConfig.Name;
                await _configManager.SaveConfigurationAsync(_config);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading quadrant configuration: {ex.Message}");
            _config = new CapturerConfiguration();
        }
    }

    public List<QuadrantConfiguration> GetConfigurations()
    {
        return _config.QuadrantSystem.Configurations.ToList();
    }

    public QuadrantConfiguration? GetActiveConfiguration()
    {
        return _config.QuadrantSystem.GetActiveConfiguration();
    }

    public async Task<bool> SaveConfigurationAsync(QuadrantConfiguration configuration)
    {
        try
        {
            _config.QuadrantSystem.AddOrUpdateConfiguration(configuration);
            await _configManager.SaveConfigurationAsync(_config);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteConfigurationAsync(string configurationName)
    {
        try
        {
            var config = _config.QuadrantSystem.Configurations.FirstOrDefault(c => c.Name == configurationName);
            if (config != null)
            {
                _config.QuadrantSystem.Configurations.Remove(config);
                
                // If this was the active configuration, set a new one
                if (_config.QuadrantSystem.ActiveConfigurationName == configurationName)
                {
                    var newActive = _config.QuadrantSystem.Configurations.FirstOrDefault();
                    _config.QuadrantSystem.ActiveConfigurationName = newActive?.Name ?? "";
                }
                
                await _configManager.SaveConfigurationAsync(_config);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting configuration: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetActiveConfigurationAsync(string configurationName)
    {
        try
        {
            var config = _config.QuadrantSystem.Configurations.FirstOrDefault(c => c.Name == configurationName);
            if (config != null)
            {
                _config.QuadrantSystem.ActiveConfigurationName = configurationName;
                await _configManager.SaveConfigurationAsync(_config);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting active configuration: {ex.Message}");
            return false;
        }
    }

    public async Task<ProcessingTask> ProcessImagesAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? configurationName = null,
        IProgress<ProcessingProgress>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        // Initial progress report
        progress?.Report(new ProcessingProgress
        {
            CurrentOperation = "Iniciando procesamiento...",
            CurrentFile = 0,
            TotalFiles = 0,
            CurrentFileName = "Preparando..."
        });
        
        Console.WriteLine($"[QuadrantService] Iniciando procesamiento: {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}");
        
        await _processingSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Report configuration loading
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Cargando configuración...",
                CurrentFile = 0,
                TotalFiles = 0,
                CurrentFileName = "Configuración"
            });
            
            // Get configuration to use
            var config = string.IsNullOrEmpty(configurationName) 
                ? GetActiveConfiguration() 
                : _config.QuadrantSystem.Configurations.FirstOrDefault(c => c.Name == configurationName);

            Console.WriteLine($"[QuadrantService] Configuración: {config?.Name ?? "NULL"}");
            
            if (config == null || !config.IsValid())
            {
                var errorMsg = "No hay configuración válida de cuadrantes disponible";
                Console.WriteLine($"[QuadrantService] ERROR: {errorMsg}");
                var task = new ProcessingTask(startDate, endDate, configurationName ?? "Unknown");
                task.Fail(errorMsg);
                
                // Report error to UI
                progress?.Report(new ProcessingProgress
                {
                    CurrentOperation = "Error: " + errorMsg,
                    CurrentFile = 0,
                    TotalFiles = 0,
                    CurrentFileName = "Error"
                });
                
                return task;
            }

            // Report getting images
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Buscando imágenes...",
                CurrentFile = 0,
                TotalFiles = 0,
                CurrentFileName = "Explorando archivos..."
            });
            
            // Get images to process
            var imagePaths = await GetImagesToProcess(startDate, endDate);
            
            Console.WriteLine($"[QuadrantService] Encontradas {imagePaths.Count} imágenes para procesar");
            
            if (!imagePaths.Any())
            {
                var msg = "No se encontraron imágenes en el rango de fechas especificado";
                Console.WriteLine($"[QuadrantService] {msg}");
                
                var task = new ProcessingTask(startDate, endDate, config.Name);
                task.Status = ProcessingStatus.Completed;
                task.Complete();
                
                // Report completion
                progress?.Report(new ProcessingProgress
                {
                    CurrentOperation = msg,
                    CurrentFile = 0,
                    TotalFiles = 0,
                    CurrentFileName = "Completado"
                });
                
                return task;
            }

            // Report folder creation
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Preparando carpetas de salida...",
                CurrentFile = 0,
                TotalFiles = imagePaths.Count,
                CurrentFileName = "Configurando estructura..."
            });
            
            // Create output folder structure
            var quadrantsBaseFolder = _config.QuadrantSystem.QuadrantsFolder;
            Console.WriteLine($"[QuadrantService] Carpeta base: {quadrantsBaseFolder}");
            Directory.CreateDirectory(quadrantsBaseFolder);

            // Create progress wrapper to forward events
            var progressWrapper = new Progress<ProcessingProgress>(p =>
            {
                Console.WriteLine($"[QuadrantService] Progreso: {p.ProgressPercentage}% - {p.CurrentOperation} - {p.CurrentFileName}");
                progress?.Report(p);
                ProcessingProgressChanged?.Invoke(this, new ProcessingProgressEventArgs { Progress = p });
            });

            // Report start of actual processing
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Iniciando procesamiento de imágenes...",
                CurrentFile = 0,
                TotalFiles = imagePaths.Count,
                CurrentFileName = "Comenzando..."
            });

            Console.WriteLine($"[QuadrantService] Iniciando procesamiento con {config.GetEnabledQuadrants().Count} cuadrantes activos");

            // Process images
            var processingTask = await ImageCropUtils.ProcessImagesAsync(
                imagePaths, 
                config, 
                quadrantsBaseFolder,
                progressWrapper, 
                cancellationToken);

            Console.WriteLine($"[QuadrantService] Procesamiento completado: {processingTask.Status}");
            Console.WriteLine($"[QuadrantService] Archivos procesados: {processingTask.ProcessedFiles}");
            Console.WriteLine($"[QuadrantService] Errores: {processingTask.ErrorMessages.Count}");
            
            // Save to history
            _processingHistory.Add(processingTask);
            
            // Keep only last 100 tasks
            if (_processingHistory.Count > 100)
            {
                _processingHistory.RemoveAt(0);
            }

            // Log results
            await LogProcessingResults(processingTask);

            // Fire completion event
            ProcessingCompleted?.Invoke(this, new ProcessingCompletedEventArgs
            {
                Task = processingTask,
                Success = processingTask.Status == ProcessingStatus.Completed,
                ErrorMessage = processingTask.Status == ProcessingStatus.Failed 
                    ? string.Join("; ", processingTask.ErrorMessages.Take(3)) 
                    : null
            });

            return processingTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuadrantService] ERROR CRÍTICO: {ex.Message}");
            Console.WriteLine($"[QuadrantService] Stack trace: {ex.StackTrace}");
            
            var errorTask = new ProcessingTask(startDate, endDate, configurationName ?? "Unknown");
            errorTask.Fail($"Error crítico durante el procesamiento: {ex.Message}");
            
            // Report error to UI
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Error crítico: " + ex.Message,
                CurrentFile = 0,
                TotalFiles = 0,
                CurrentFileName = "Error"
            });
            
            // Fire completion event with error
            ProcessingCompleted?.Invoke(this, new ProcessingCompletedEventArgs
            {
                Task = errorTask,
                Success = false,
                ErrorMessage = ex.Message
            });
            
            return errorTask;
        }
        finally
        {
            _processingSemaphore.Release();
            Console.WriteLine("[QuadrantService] Liberando semáforo de procesamiento");
        }
    }

    public async Task<ProcessingTask> ProcessSpecificImagesAsync(
        List<string> imagePaths,
        string? configurationName = null,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Initial progress report
        progress?.Report(new ProcessingProgress
        {
            CurrentOperation = "Iniciando procesamiento específico...",
            CurrentFile = 0,
            TotalFiles = imagePaths.Count,
            CurrentFileName = "Preparando..."
        });
        
        Console.WriteLine($"[QuadrantService] Iniciando procesamiento específico de {imagePaths.Count} imágenes");
        
        await _processingSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Report configuration loading
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Cargando configuración...",
                CurrentFile = 0,
                TotalFiles = imagePaths.Count,
                CurrentFileName = "Configuración"
            });
            
            // Get configuration to use
            var config = string.IsNullOrEmpty(configurationName) 
                ? GetActiveConfiguration() 
                : _config.QuadrantSystem.Configurations.FirstOrDefault(c => c.Name == configurationName);

            Console.WriteLine($"[QuadrantService] Configuración: {config?.Name ?? "NULL"}");
            
            if (config == null || !config.IsValid())
            {
                var errorMsg = "No hay configuración válida de cuadrantes disponible";
                Console.WriteLine($"[QuadrantService] ERROR: {errorMsg}");
                var task = new ProcessingTask(DateTime.Now, DateTime.Now, configurationName ?? "Unknown");
                task.Fail(errorMsg);
                
                progress?.Report(new ProcessingProgress
                {
                    CurrentOperation = "Error: " + errorMsg,
                    CurrentFile = 0,
                    TotalFiles = 0,
                    CurrentFileName = "Error"
                });
                
                return task;
            }

            // Filter out non-existent files
            var validImagePaths = imagePaths.Where(File.Exists).ToList();
            
            Console.WriteLine($"[QuadrantService] {validImagePaths.Count} imágenes válidas de {imagePaths.Count} especificadas");
            
            if (!validImagePaths.Any())
            {
                var msg = "No se encontraron imágenes válidas en la lista especificada";
                Console.WriteLine($"[QuadrantService] {msg}");
                
                var task = new ProcessingTask(DateTime.Now, DateTime.Now, config.Name);
                task.Status = ProcessingStatus.Completed;
                task.Complete();
                
                progress?.Report(new ProcessingProgress
                {
                    CurrentOperation = msg,
                    CurrentFile = 0,
                    TotalFiles = 0,
                    CurrentFileName = "Completado"
                });
                
                return task;
            }

            // Report folder creation
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Preparando carpetas de salida...",
                CurrentFile = 0,
                TotalFiles = validImagePaths.Count,
                CurrentFileName = "Configurando estructura..."
            });
            
            // Create output folder structure
            var quadrantsBaseFolder = _config.QuadrantSystem.QuadrantsFolder;
            Console.WriteLine($"[QuadrantService] Carpeta base: {quadrantsBaseFolder}");
            Directory.CreateDirectory(quadrantsBaseFolder);

            // Create progress wrapper to forward events
            var progressWrapper = new Progress<ProcessingProgress>(p =>
            {
                Console.WriteLine($"[QuadrantService] Progreso: {p.ProgressPercentage}% - {p.CurrentOperation} - {p.CurrentFileName}");
                progress?.Report(p);
                ProcessingProgressChanged?.Invoke(this, new ProcessingProgressEventArgs { Progress = p });
            });

            // Report start of actual processing
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Procesando imágenes específicas...",
                CurrentFile = 0,
                TotalFiles = validImagePaths.Count,
                CurrentFileName = "Comenzando..."
            });

            Console.WriteLine($"[QuadrantService] Iniciando procesamiento con {config.GetEnabledQuadrants().Count} cuadrantes activos");

            // Process specific images
            var processingTask = await ImageCropUtils.ProcessImagesAsync(
                validImagePaths, 
                config, 
                quadrantsBaseFolder,
                progressWrapper, 
                cancellationToken);

            Console.WriteLine($"[QuadrantService] Procesamiento específico completado: {processingTask.Status}");
            Console.WriteLine($"[QuadrantService] Archivos procesados: {processingTask.ProcessedFiles}");
            Console.WriteLine($"[QuadrantService] Errores: {processingTask.ErrorMessages.Count}");
            
            // Save to history
            _processingHistory.Add(processingTask);
            
            // Keep only last 100 tasks
            if (_processingHistory.Count > 100)
            {
                _processingHistory.RemoveAt(0);
            }

            // Log results
            await LogProcessingResults(processingTask);

            // Fire completion event
            ProcessingCompleted?.Invoke(this, new ProcessingCompletedEventArgs
            {
                Task = processingTask,
                Success = processingTask.Status == ProcessingStatus.Completed,
                ErrorMessage = processingTask.Status == ProcessingStatus.Failed 
                    ? string.Join("; ", processingTask.ErrorMessages.Take(3)) 
                    : null
            });

            return processingTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuadrantService] ERROR CRÍTICO en procesamiento específico: {ex.Message}");
            Console.WriteLine($"[QuadrantService] Stack trace: {ex.StackTrace}");
            
            var errorTask = new ProcessingTask(DateTime.Now, DateTime.Now, configurationName ?? "Unknown");
            errorTask.Fail($"Error crítico durante el procesamiento específico: {ex.Message}");
            
            progress?.Report(new ProcessingProgress
            {
                CurrentOperation = "Error crítico: " + ex.Message,
                CurrentFile = 0,
                TotalFiles = 0,
                CurrentFileName = "Error"
            });
            
            ProcessingCompleted?.Invoke(this, new ProcessingCompletedEventArgs
            {
                Task = errorTask,
                Success = false,
                ErrorMessage = ex.Message
            });
            
            return errorTask;
        }
        finally
        {
            _processingSemaphore.Release();
            Console.WriteLine("[QuadrantService] Liberando semáforo de procesamiento específico");
        }
    }

    public List<ProcessingTask> GetProcessingHistory()
    {
        return _processingHistory.OrderByDescending(t => t.StartTime).ToList();
    }

    public List<string> GetAvailableQuadrantFolders()
    {
        var quadrantsFolder = _config.QuadrantSystem.QuadrantsFolder;
        
        if (!Directory.Exists(quadrantsFolder))
            return new List<string>();

        return Directory.GetDirectories(quadrantsFolder)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList()!;
    }

    public async Task<List<string>> GetQuadrantFilesAsync(List<string> quadrantNames, DateTime startDate, DateTime endDate)
    {
        var files = new List<string>();
        var quadrantsFolder = _config.QuadrantSystem.QuadrantsFolder;

        await Task.Run(() =>
        {
            foreach (var quadrantName in quadrantNames)
            {
                var quadrantFolder = Path.Combine(quadrantsFolder, quadrantName);
                if (!Directory.Exists(quadrantFolder)) continue;

                var quadrantFiles = Directory.GetFiles(quadrantFolder, "*.png")
                    .Concat(Directory.GetFiles(quadrantFolder, "*.jpg"))
                    .Concat(Directory.GetFiles(quadrantFolder, "*.jpeg"))
                    .Where(file =>
                    {
                        var fileInfo = new FileInfo(file);
                        return fileInfo.CreationTime >= startDate && fileInfo.CreationTime <= endDate;
                    });

                files.AddRange(quadrantFiles);
            }
        });

        return files.OrderByDescending(f => new FileInfo(f).CreationTime).ToList();
    }

    public async Task<Bitmap?> CreatePreviewImageAsync(string imagePath, string? configurationName = null)
    {
        try
        {
            if (!File.Exists(imagePath))
                return null;

            var config = string.IsNullOrEmpty(configurationName) 
                ? GetActiveConfiguration() 
                : _config.QuadrantSystem.Configurations.FirstOrDefault(c => c.Name == configurationName);

            if (config == null) return null;

            return await Task.Run(() =>
            {
                using var sourceImage = new Bitmap(imagePath);
                return ImageCropUtils.CreatePreviewImage(sourceImage, config, _config.QuadrantSystem.ShowPreviewColors);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating preview: {ex.Message}");
            return null;
        }
    }

    private async Task<List<string>> GetImagesToProcess(DateTime startDate, DateTime endDate)
    {
        try
        {
            var screenshotsFolder = _config.Storage.ScreenshotFolder;
            if (!Directory.Exists(screenshotsFolder))
                return new List<string>();

            return await Task.Run(() =>
            {
                return Directory.GetFiles(screenshotsFolder, "*.png")
                    .Concat(Directory.GetFiles(screenshotsFolder, "*.jpg"))
                    .Concat(Directory.GetFiles(screenshotsFolder, "*.jpeg"))
                    .Where(file =>
                    {
                        var fileInfo = new FileInfo(file);
                        return fileInfo.CreationTime >= startDate && fileInfo.CreationTime <= endDate;
                    })
                    .OrderBy(f => new FileInfo(f).CreationTime)
                    .ToList();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting images to process: {ex.Message}");
            return new List<string>();
        }
    }

    private async Task LogProcessingResults(ProcessingTask task)
    {
        if (!_config.QuadrantSystem.EnableLogging) return;

        try
        {
            var logFolder = Path.Combine(_config.QuadrantSystem.QuadrantsFolder, "Logs");
            Directory.CreateDirectory(logFolder);
            
            var logFile = Path.Combine(logFolder, $"processing_{DateTime.Now:yyyy-MM-dd}.log");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {task.GetSummary()}\n";
            
            if (task.SkippedFiles > 0)
            {
                logEntry += $"  Archivos omitidos: {string.Join(", ", task.SkippedFileNames.Take(10))}\n";
            }
            
            if (task.ErrorMessages.Any())
            {
                logEntry += $"  Errores: {string.Join("; ", task.ErrorMessages.Take(5))}\n";
            }
            
            await File.AppendAllTextAsync(logFile, logEntry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging processing results: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _processingSemaphore?.Dispose();
    }
}