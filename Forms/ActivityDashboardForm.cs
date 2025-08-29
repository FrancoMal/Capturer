using System.Drawing;
using System.Windows.Forms;
using Capturer.Services;
using Capturer.Models;

namespace Capturer.Forms;

/// <summary>
/// Dashboard en tiempo real para monitorear actividad de cuadrantes
/// </summary>
public partial class ActivityDashboardForm : Form
{
    private readonly QuadrantActivityService _activityService;
    private readonly QuadrantService _quadrantService;
    private System.Windows.Forms.Timer? _refreshTimer;
    private System.Windows.Forms.Timer? _monitoringTimer;
    private DataGridView? _statsGrid;
    private Panel? _configPanel;
    private Panel? _quadrantVisualPanel;
    private Dictionary<string, ProgressBar> _activityBars = new();
    private Dictionary<string, Label> _statusLabels = new();
    private QuadrantActivityConfiguration _dashboardConfig = new();
    private string _tempActivityFolder = "";
    private Dictionary<string, string> _lastTempFiles = new(); // Track last temp files per quadrant for cleanup

    public ActivityDashboardForm(QuadrantActivityService activityService, QuadrantService quadrantService)
    {
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _quadrantService = quadrantService ?? throw new ArgumentNullException(nameof(quadrantService));
        InitializeTempFolder();
        InitializeComponent();
        SetupEventHandlers();
        StartRefreshTimer();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // Configuración básica del formulario
        Text = "Dashboard de Actividad - Cuadrantes";
        Size = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 600);

        // Panel principal
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10)
        };

        // Configurar columnas y filas
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

        // Panel de visualización de cuadrantes (arriba izquierda)
        _quadrantVisualPanel = CreateQuadrantVisualPanel();
        mainPanel.Controls.Add(_quadrantVisualPanel, 0, 0);

        // Panel de configuración (arriba derecha)
        _configPanel = CreateConfigurationPanel();
        mainPanel.Controls.Add(_configPanel, 1, 0);

        // Grid de estadísticas (abajo, span 2 columnas)
        _statsGrid = CreateStatsGrid();
        var statsContainer = new Panel { Dock = DockStyle.Fill };
        statsContainer.Controls.Add(_statsGrid);
        mainPanel.Controls.Add(statsContainer, 0, 1);
        mainPanel.SetColumnSpan(statsContainer, 2);

        Controls.Add(mainPanel);
        ResumeLayout(false);
    }

    private Panel CreateQuadrantVisualPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.WhiteSmoke
        };

        var titleLabel = new Label
        {
            Text = "Actividad de Cuadrantes en Tiempo Real",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.DarkBlue,
            ForeColor = Color.White
        };

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        panel.Controls.Add(scrollPanel);
        panel.Controls.Add(titleLabel);

        return panel;
    }

    private Panel CreateConfigurationPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.AliceBlue
        };

        var titleLabel = new Label
        {
            Text = "Configuración",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.DarkGreen,
            ForeColor = Color.White
        };

        var contentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(10),
            AutoSize = true
        };

        // Configurar filas
        for (int i = 0; i < 8; i++)
        {
            contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Threshold de actividad
        var thresholdLabel = new Label
        {
            Text = "Umbral de Actividad (%):",
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        var thresholdNumeric = new NumericUpDown
        {
            Minimum = 0.1M,
            Maximum = 50M,
            DecimalPlaces = 1,
            Value = (decimal)_activityService.Configuration.ActivityThresholdPercentage,
            Width = 100,
            Anchor = AnchorStyles.Left
        };
        thresholdNumeric.ValueChanged += (s, e) =>
        {
            _activityService.Configuration.ActivityThresholdPercentage = (double)thresholdNumeric.Value;
        };

        // Tolerancia de pixel
        var toleranceLabel = new Label
        {
            Text = "Tolerancia Pixel (0-255):",
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        var toleranceNumeric = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 255,
            Value = _activityService.Configuration.PixelToleranceThreshold,
            Width = 100,
            Anchor = AnchorStyles.Left
        };
        toleranceNumeric.ValueChanged += (s, e) =>
        {
            _activityService.Configuration.PixelToleranceThreshold = (int)toleranceNumeric.Value;
        };

        // Botones de control
        var resetButton = new Button
        {
            Text = "Resetear Estadísticas",
            Size = new Size(150, 30),
            BackColor = Color.Orange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Left
        };
        resetButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("¿Está seguro de resetear todas las estadísticas?", 
                                       "Confirmar Reset", 
                                       MessageBoxButtons.YesNo, 
                                       MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _activityService.ResetAllActivityStats();
                RefreshDisplay();
            }
        };

        // Información en tiempo real
        var infoLabel = new Label
        {
            Text = "Estado: Monitoring activo",
            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
            ForeColor = Color.DarkGreen,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        // Botón de configuración principal
        var configButton = new Button
        {
            Text = "⚙️ Configurar Dashboard",
            Size = new Size(180, 35),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Anchor = AnchorStyles.Left
        };
        configButton.FlatAppearance.BorderSize = 0;
        configButton.Click += OnConfigureButtonClick;

        // Separador
        var separatorLabel = new Label
        {
            Text = "─────────────────────",
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 8F),
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        // Agregar controles al panel
        contentPanel.Controls.Add(configButton, 0, 0);
        contentPanel.Controls.Add(separatorLabel, 0, 1);
        contentPanel.Controls.Add(thresholdLabel, 0, 2);
        contentPanel.Controls.Add(thresholdNumeric, 0, 3);
        contentPanel.Controls.Add(toleranceLabel, 0, 4);
        contentPanel.Controls.Add(toleranceNumeric, 0, 5);
        contentPanel.Controls.Add(resetButton, 0, 6);
        contentPanel.Controls.Add(infoLabel, 0, 7);

        panel.Controls.Add(contentPanel);
        panel.Controls.Add(titleLabel);

        return panel;
    }

    private DataGridView CreateStatsGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };

        // Configurar columnas
        grid.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "QuadrantName",
                HeaderText = "Cuadrante",
                DataPropertyName = "QuadrantName",
                Width = 120,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "TotalComparisons",
                HeaderText = "Comparaciones",
                DataPropertyName = "TotalComparisons",
                Width = 100,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ActivityCount",
                HeaderText = "Actividades",
                DataPropertyName = "ActivityDetectionCount",
                Width = 100,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ActivityRate",
                HeaderText = "% Actividad",
                DataPropertyName = "ActivityRateFormatted",
                Width = 100,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "AverageChange",
                HeaderText = "% Cambio Prom.",
                DataPropertyName = "AverageChangeFormatted",
                Width = 120,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "LastActivity",
                HeaderText = "Última Actividad",
                DataPropertyName = "LastActivityFormatted",
                Width = 140,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "SessionDuration",
                HeaderText = "Duración Sesión",
                DataPropertyName = "SessionDurationFormatted",
                Width = 120,
                ReadOnly = true
            }
        });

        return grid;
    }

    private void SetupEventHandlers()
    {
        _activityService.ActivityChanged += OnActivityChanged;
        FormClosing += (s, e) => _refreshTimer?.Stop();
    }

    private void OnActivityChanged(object? sender, QuadrantActivityChangedEventArgs e)
    {
        // Actualizar en el hilo de UI
        if (InvokeRequired)
        {
            Invoke(() => OnActivityChanged(sender, e));
            return;
        }

        UpdateQuadrantVisual(e.Result, e.Stats);
    }

    private void UpdateQuadrantVisual(QuadrantActivityResult result, QuadrantActivityStats stats)
    {
        if (_quadrantVisualPanel?.Controls.Count > 0)
        {
            var scrollPanel = _quadrantVisualPanel.Controls[0] as Panel;
            if (scrollPanel != null)
            {
                // Buscar o crear controles para este cuadrante
                var quadrantName = result.QuadrantName;
                
                if (!_activityBars.ContainsKey(quadrantName))
                {
                    CreateQuadrantVisualControls(scrollPanel, quadrantName);
                }

                // Actualizar barra de progreso y estado
                if (_activityBars.TryGetValue(quadrantName, out var progressBar))
                {
                    progressBar.Value = Math.Min(100, (int)result.ChangePercentage);
                    progressBar.BackColor = result.HasActivity ? Color.LightGreen : Color.LightGray;
                }

                if (_statusLabels.TryGetValue(quadrantName, out var statusLabel))
                {
                    statusLabel.Text = $"{quadrantName}: {result.ChangePercentage:F2}% " +
                                     $"({stats.ActivityDetectionCount} actividades)";
                    statusLabel.ForeColor = result.HasActivity ? Color.DarkGreen : Color.Gray;
                }
            }
        }
    }

    private void CreateQuadrantVisualControls(Panel parent, string quadrantName)
    {
        var y = parent.Controls.Count * 60 + 10;

        var statusLabel = new Label
        {
            Text = $"{quadrantName}: Esperando datos...",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(10, y),
            Size = new Size(300, 20),
            ForeColor = Color.DarkBlue
        };

        var progressBar = new ProgressBar
        {
            Location = new Point(10, y + 25),
            Size = new Size(300, 20),
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous
        };

        _statusLabels[quadrantName] = statusLabel;
        _activityBars[quadrantName] = progressBar;

        parent.Controls.Add(statusLabel);
        parent.Controls.Add(progressBar);
    }

    private void StartRefreshTimer()
    {
        _refreshTimer = new System.Windows.Forms.Timer
        {
            Interval = 2000 // Actualizar cada 2 segundos
        };
        _refreshTimer.Tick += (s, e) => RefreshDisplay();
        _refreshTimer.Start();
    }

    private void RefreshDisplay()
    {
        if (InvokeRequired)
        {
            Invoke(RefreshDisplay);
            return;
        }

        // Actualizar grid de estadísticas
        var stats = _activityService.GetAllActivityStats();
        var displayStats = stats.Values.Select(s => new
        {
            QuadrantName = s.QuadrantName,
            TotalComparisons = s.TotalComparisons,
            ActivityDetectionCount = s.ActivityDetectionCount,
            ActivityRateFormatted = $"{s.ActivityRate:F1}%",
            AverageChangeFormatted = $"{s.AverageChangePercentage:F2}%",
            LastActivityFormatted = s.LastActivityTime.ToString("HH:mm:ss"),
            SessionDurationFormatted = FormatTimeSpan(s.SessionDuration)
        }).ToList();

        if (_statsGrid != null)
        {
            _statsGrid.DataSource = displayStats;
        }
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        else
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
    }

    private void OnConfigureButtonClick(object? sender, EventArgs e)
    {
        try
        {
            using var configForm = new ActivityDashboardConfigForm(_quadrantService, _dashboardConfig);
            if (configForm.ShowDialog(this) == DialogResult.OK)
            {
                _dashboardConfig = configForm.Configuration;
                ApplyConfiguration();
                UpdateConfigurationDisplay();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir la configuración: {ex.Message}", 
                           "Error de Configuración", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyConfiguration()
    {
        // Update activity service configuration
        _activityService.Configuration.PixelToleranceThreshold = _dashboardConfig.PixelToleranceThreshold;
        _activityService.Configuration.ActivityThresholdPercentage = _dashboardConfig.ActivityThresholdPercentage;

        // Restart monitoring timer with new interval
        StopMonitoring();
        if (_dashboardConfig.IsMonitoringEnabled && _dashboardConfig.TotalIntervalMilliseconds > 0)
        {
            StartMonitoring();
        }

        Console.WriteLine($"[ActivityDashboard] Configuración aplicada: {_dashboardConfig.SelectedQuadrantConfigName}, " +
                         $"Intervalo: {_dashboardConfig.MonitoringInterval}, " +
                         $"Monitoreo: {(_dashboardConfig.IsMonitoringEnabled ? "Activo" : "Inactivo")}");
    }

    private void UpdateConfigurationDisplay()
    {
        // Update the info label in config panel to show current settings
        if (_configPanel?.Controls[0] is TableLayoutPanel contentPanel)
        {
            foreach (Control control in contentPanel.Controls)
            {
                if (control is Label label && label.Name == "infoLabel")
                {
                    var status = _dashboardConfig.IsMonitoringEnabled ? "Monitoreo activo" : "Monitoreo detenido";
                    var interval = _dashboardConfig.MonitoringInterval.TotalSeconds > 0 
                                 ? $"cada {_dashboardConfig.MonitoringInterval.TotalSeconds}s" 
                                 : "sin configurar";
                    label.Text = $"Estado: {status} {interval}";
                    label.ForeColor = _dashboardConfig.IsMonitoringEnabled ? Color.DarkGreen : Color.DarkRed;
                    break;
                }
            }
        }
    }

    private void StartMonitoring()
    {
        if (_monitoringTimer != null)
        {
            _monitoringTimer.Stop();
            _monitoringTimer.Dispose();
        }

        _monitoringTimer = new System.Windows.Forms.Timer
        {
            Interval = _dashboardConfig.TotalIntervalMilliseconds
        };
        _monitoringTimer.Tick += OnMonitoringTick;
        _monitoringTimer.Start();

        Console.WriteLine($"[ActivityDashboard] Monitoreo iniciado con intervalo de {_dashboardConfig.MonitoringInterval.TotalSeconds}s");
    }

    private void StopMonitoring()
    {
        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
        
        Console.WriteLine("[ActivityDashboard] Monitoreo detenido");
    }

    private async void OnMonitoringTick(object? sender, EventArgs e)
    {
        try
        {
            await PerformActivityMonitoringCapture();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error en monitoreo: {ex.Message}");
        }
    }

    private void InitializeTempFolder()
    {
        try
        {
            // Create temp folder for activity monitoring (separate from regular quadrants)
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _tempActivityFolder = Path.Combine(documentsPath, "Capturer", "TempActivity");
            
            if (!Directory.Exists(_tempActivityFolder))
            {
                Directory.CreateDirectory(_tempActivityFolder);
                Console.WriteLine($"[ActivityDashboard] Carpeta temporal creada: {_tempActivityFolder}");
            }
            else
            {
                // Clean up any old temp files on startup
                CleanupTempFolder();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error inicializando carpeta temporal: {ex.Message}");
            _tempActivityFolder = Path.GetTempPath(); // Fallback to system temp
        }
    }

    private void CleanupTempFolder()
    {
        try
        {
            if (Directory.Exists(_tempActivityFolder))
            {
                var tempFiles = Directory.GetFiles(_tempActivityFolder, "temp_quadrant_*.png");
                foreach (var file in tempFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore individual file deletion errors
                    }
                }
                Console.WriteLine($"[ActivityDashboard] Limpieza de carpeta temporal completada: {tempFiles.Length} archivos eliminados");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error en limpieza de carpeta temporal: {ex.Message}");
        }
    }

    private async Task PerformActivityMonitoringCapture()
    {
        // Get the selected quadrant configuration
        var configurations = _quadrantService.GetConfigurations();
        if (configurations == null || !configurations.Any()) return;

        var selectedConfig = configurations
            .FirstOrDefault(c => c.Name == _dashboardConfig.SelectedQuadrantConfigName);
        
        if (selectedConfig?.Quadrants == null) return;

        // Capture screenshot
        using var fullScreenshot = CaptureCurrentScreen();
        if (fullScreenshot == null) return;

        // Process each enabled quadrant
        foreach (var region in selectedConfig.Quadrants.Where(r => r.IsEnabled))
        {
            try
            {
                using var quadrantImage = CropImageToQuadrant(fullScreenshot, region);
                if (quadrantImage != null)
                {
                    // Save temporary quadrant image for comparison
                    var tempFilePath = await SaveTempQuadrantImage(quadrantImage, region.Name);
                    
                    // Compare with activity service
                    _activityService.CompareQuadrant(quadrantImage, region.Name);
                    
                    // Clean up previous temp file for this quadrant
                    CleanupPreviousTempFile(region.Name, tempFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActivityDashboard] Error procesando cuadrante {region.Name}: {ex.Message}");
            }
        }
    }

    private async Task<string> SaveTempQuadrantImage(Bitmap quadrantImage, string quadrantName)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var safeQuadrantName = string.Join("_", quadrantName.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"temp_quadrant_{safeQuadrantName}_{timestamp}.png";
            var filePath = Path.Combine(_tempActivityFolder, fileName);
            
            await Task.Run(() => quadrantImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Png));
            
            Console.WriteLine($"[ActivityDashboard] Imagen temporal guardada: {fileName}");
            return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error guardando imagen temporal para {quadrantName}: {ex.Message}");
            return "";
        }
    }

    private void CleanupPreviousTempFile(string quadrantName, string currentFilePath)
    {
        try
        {
            // Clean up the previous temp file for this quadrant (keep only the most recent)
            if (_lastTempFiles.TryGetValue(quadrantName, out var previousFilePath) && 
                !string.IsNullOrEmpty(previousFilePath) && 
                File.Exists(previousFilePath) && 
                previousFilePath != currentFilePath)
            {
                File.Delete(previousFilePath);
                Console.WriteLine($"[ActivityDashboard] Archivo temporal anterior eliminado: {Path.GetFileName(previousFilePath)}");
            }
            
            // Update the tracking dictionary
            _lastTempFiles[quadrantName] = currentFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error limpiando archivo temporal anterior de {quadrantName}: {ex.Message}");
        }
    }

    private Bitmap? CaptureCurrentScreen()
    {
        try
        {
            // Simple screen capture - capture primary screen for now
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error capturando pantalla: {ex.Message}");
            return null;
        }
    }

    private Bitmap? CropImageToQuadrant(Bitmap sourceImage, QuadrantRegion region)
    {
        try
        {
            // Validate bounds
            if (region.Bounds.X < 0 || region.Bounds.Y < 0 || 
                region.Bounds.Right > sourceImage.Width || 
                region.Bounds.Bottom > sourceImage.Height)
            {
                Console.WriteLine($"[ActivityDashboard] Cuadrante {region.Name} fuera de límites de imagen");
                return null;
            }

            return sourceImage.Clone(region.Bounds, sourceImage.PixelFormat);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error recortando imagen para {region.Name}: {ex.Message}");
            return null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopMonitoring();
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _activityService.ActivityChanged -= OnActivityChanged;
            
            // Clean up temporary files when form is disposed
            CleanupAllTempFiles();
        }
        base.Dispose(disposing);
    }

    private void CleanupAllTempFiles()
    {
        try
        {
            // Clean up tracked temp files
            foreach (var filePath in _lastTempFiles.Values)
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Ignore individual cleanup errors
                    }
                }
            }
            _lastTempFiles.Clear();
            
            // Additional cleanup of the temp folder
            CleanupTempFolder();
            
            Console.WriteLine("[ActivityDashboard] Limpieza final de archivos temporales completada");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error en limpieza final: {ex.Message}");
        }
    }
}