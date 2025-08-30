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
    private readonly ActivityReportService _reportService;
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
    
    // Countdown timer functionality
    private DateTime _nextMonitoringTime = DateTime.Now;
    private System.Windows.Forms.Timer? _countdownTimer;
    private Label? _countdownLabel;
    
    // Pause/Resume functionality
    private bool _isPaused = false;
    private Button? _pauseResumeButton;
    private DateTime _pauseStartTime;
    
    // System tray functionality
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _trayContextMenu;

    public ActivityDashboardForm(QuadrantActivityService activityService, QuadrantService quadrantService)
    {
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _quadrantService = quadrantService ?? throw new ArgumentNullException(nameof(quadrantService));
        _reportService = new ActivityReportService(_activityService);
        InitializeTempFolder();
        InitializeComponent();
        SetupSystemTray();
        SetupEventHandlers();
        StartRefreshTimer();
        StartCountdownTimer();
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
            RowCount = 13,  // Aumentamos para pause button
            Padding = new Padding(10),
            AutoSize = true
        };

        // Configurar filas
        for (int i = 0; i < 13; i++)  // Más filas para pause functionality
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
            Anchor = AnchorStyles.Left,
            Name = "infoLabel"
        };

        // Etiqueta de countdown
        _countdownLabel = new Label
        {
            Text = "Próxima verificación: --:--",
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            ForeColor = Color.DarkBlue,
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

        // Export buttons
        var exportSeparatorLabel = new Label
        {
            Text = "─────────────────────",
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 8F),
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        var exportHtmlButton = new Button
        {
            Text = "📊 Exportar HTML",
            Size = new Size(150, 25),
            BackColor = Color.FromArgb(0, 150, 136),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
            Anchor = AnchorStyles.Left
        };
        exportHtmlButton.FlatAppearance.BorderSize = 0;
        exportHtmlButton.Click += async (s, e) => await ExportReportAsync("HTML");

        var exportCsvButton = new Button
        {
            Text = "📄 Exportar CSV", 
            Size = new Size(150, 25),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
            Anchor = AnchorStyles.Left
        };
        exportCsvButton.FlatAppearance.BorderSize = 0;
        exportCsvButton.Click += async (s, e) => await ExportReportAsync("CSV");

        // Pause/Resume button
        _pauseResumeButton = new Button
        {
            Text = "⏸️ Pausar Monitoreo",
            Size = new Size(150, 30),
            BackColor = Color.FromArgb(255, 152, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Anchor = AnchorStyles.Left
        };
        _pauseResumeButton.FlatAppearance.BorderSize = 0;
        _pauseResumeButton.Click += OnPauseResumeClick;

        // Agregar controles al panel
        contentPanel.Controls.Add(configButton, 0, 0);
        contentPanel.Controls.Add(separatorLabel, 0, 1);
        contentPanel.Controls.Add(_pauseResumeButton, 0, 2);
        contentPanel.Controls.Add(thresholdLabel, 0, 3);
        contentPanel.Controls.Add(thresholdNumeric, 0, 4);
        contentPanel.Controls.Add(toleranceLabel, 0, 5);
        contentPanel.Controls.Add(toleranceNumeric, 0, 6);
        contentPanel.Controls.Add(resetButton, 0, 7);
        contentPanel.Controls.Add(infoLabel, 0, 8);
        contentPanel.Controls.Add(_countdownLabel!, 0, 9);
        contentPanel.Controls.Add(exportSeparatorLabel, 0, 10);
        contentPanel.Controls.Add(exportHtmlButton, 0, 11);
        contentPanel.Controls.Add(exportCsvButton, 0, 12);

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

    private void StartCountdownTimer()
    {
        _countdownTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000 // Update every second
        };
        _countdownTimer.Tick += UpdateCountdownDisplay;
        _countdownTimer.Start();
    }

    private void SetupSystemTray()
    {
        try
        {
            // Create system tray icon
            _notifyIcon = new NotifyIcon
            {
                Text = "Dashboard de Actividad - Capturer",
                Visible = false
            };

            // Try to load icon
            try
            {
                if (File.Exists("Capturer_Logo.ico"))
                {
                    _notifyIcon.Icon = new Icon("Capturer_Logo.ico");
                }
                else
                {
                    // Fallback to embedded resource or default icon
                    var stream = GetType().Assembly.GetManifestResourceStream("Capturer.Capturer_Logo.ico");
                    if (stream != null)
                    {
                        _notifyIcon.Icon = new Icon(stream);
                    }
                    else
                    {
                        _notifyIcon.Icon = SystemIcons.Application;
                    }
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            // Create context menu
            _trayContextMenu = new ContextMenuStrip();
            
            var showItem = new ToolStripMenuItem("Mostrar Dashboard")
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            showItem.Click += (s, e) => ShowFromTray();

            var pauseResumeItem = new ToolStripMenuItem(_isPaused ? "▶️ Reanudar Monitoreo" : "⏸️ Pausar Monitoreo");
            pauseResumeItem.Click += (s, e) => 
            {
                OnPauseResumeClick(null, EventArgs.Empty);
                pauseResumeItem.Text = _isPaused ? "▶️ Reanudar Monitoreo" : "⏸️ Pausar Monitoreo";
            };

            var exportHtmlItem = new ToolStripMenuItem("📊 Exportar HTML");
            exportHtmlItem.Click += async (s, e) => await ExportReportAsync("HTML");

            var exportCsvItem = new ToolStripMenuItem("📄 Exportar CSV");
            exportCsvItem.Click += async (s, e) => await ExportReportAsync("CSV");

            var separatorItem = new ToolStripSeparator();

            var hideItem = new ToolStripMenuItem("Minimizar a bandeja");
            hideItem.Click += (s, e) => HideToTray();

            var exitItem = new ToolStripMenuItem("Cerrar Dashboard");
            exitItem.Click += (s, e) => Close();

            _trayContextMenu.Items.AddRange(new ToolStripItem[]
            {
                showItem,
                separatorItem,
                pauseResumeItem,
                exportHtmlItem,
                exportCsvItem,
                new ToolStripSeparator(),
                hideItem,
                exitItem
            });

            _notifyIcon.ContextMenuStrip = _trayContextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowFromTray();

            Console.WriteLine("[ActivityDashboard] System tray configurado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error configurando system tray: {ex.Message}");
        }
    }

    private void UpdateCountdownDisplay(object? sender, EventArgs e)
    {
        if (_countdownLabel == null) return;

        if (InvokeRequired)
        {
            Invoke(() => UpdateCountdownDisplay(sender, e));
            return;
        }

        if (_isPaused)
        {
            var pausedDuration = DateTime.Now - _pauseStartTime;
            _countdownLabel.Text = $"⏸️ Pausado desde hace: {pausedDuration:mm\\:ss}";
            _countdownLabel.ForeColor = Color.DarkRed;
        }
        else if (_dashboardConfig.IsMonitoringEnabled && _monitoringTimer != null)
        {
            var timeRemaining = _nextMonitoringTime - DateTime.Now;
            if (timeRemaining.TotalSeconds > 0)
            {
                _countdownLabel.Text = $"Próxima verificación: {timeRemaining:mm\\:ss}";
                _countdownLabel.ForeColor = Color.DarkBlue;
            }
            else
            {
                _countdownLabel.Text = "Verificando ahora...";
                _countdownLabel.ForeColor = Color.DarkOrange;
            }
        }
        else
        {
            _countdownLabel.Text = "Monitoreo detenido";
            _countdownLabel.ForeColor = Color.Gray;
        }
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
        
        // Set next monitoring time
        _nextMonitoringTime = DateTime.Now.AddMilliseconds(_dashboardConfig.TotalIntervalMilliseconds);

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
            if (!_isPaused)
            {
                await PerformActivityMonitoringCapture();
                
                // Update next monitoring time
                _nextMonitoringTime = DateTime.Now.AddMilliseconds(_dashboardConfig.TotalIntervalMilliseconds);
            }
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
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _notifyIcon?.Dispose();
            _trayContextMenu?.Dispose();
            _reportService?.Dispose();
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

    /// <summary>
    /// Exporta el reporte actual de actividad
    /// </summary>
    private async Task ExportReportAsync(string format)
    {
        try
        {
            // Generate report from current session
            var currentReport = _reportService.GetCurrentSessionReport();
            if (currentReport == null || currentReport.Entries.Count == 0)
            {
                MessageBox.Show("No hay datos de actividad para exportar. " +
                               "Espere a que se generen datos de monitoreo.",
                               "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Update report info
            currentReport.ReportEndTime = DateTime.Now;
            currentReport.QuadrantConfigurationName = _dashboardConfig.SelectedQuadrantConfigName;
            currentReport.ReportType = "Dashboard Export";
            currentReport.Comments = $"Reporte exportado desde el Dashboard de Actividad en formato {format}";

            // Export the report
            var exportPath = await _reportService.ExportReportAsync(currentReport, format);
            
            // Show success message with option to open
            var result = MessageBox.Show(
                $"Reporte exportado exitosamente:\n\n{exportPath}\n\n¿Desea abrir el archivo?",
                "Exportación Exitosa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exportPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir el archivo automáticamente:\n{ex.Message}\n\nRuta: {exportPath}",
                                   "Error al abrir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            Console.WriteLine($"[ActivityDashboard] Reporte exportado en formato {format}: {exportPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al exportar el reporte:\n{ex.Message}",
                           "Error de exportación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"[ActivityDashboard] Error en exportación {format}: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja el botón de pausa/reanudar
    /// </summary>
    private void OnPauseResumeClick(object? sender, EventArgs e)
    {
        if (_isPaused)
        {
            // Resume monitoring
            _isPaused = false;
            if (_dashboardConfig.IsMonitoringEnabled)
            {
                StartMonitoring();
            }
            
            if (_pauseResumeButton != null)
            {
                _pauseResumeButton.Text = "⏸️ Pausar Monitoreo";
                _pauseResumeButton.BackColor = Color.FromArgb(255, 152, 0);
            }
            
            Console.WriteLine("[ActivityDashboard] Monitoreo reanudado por el usuario");
        }
        else
        {
            // Pause monitoring
            _isPaused = true;
            _pauseStartTime = DateTime.Now;
            StopMonitoring();
            
            if (_pauseResumeButton != null)
            {
                _pauseResumeButton.Text = "▶️ Reanudar Monitoreo";
                _pauseResumeButton.BackColor = Color.FromArgb(76, 175, 80);
            }
            
            Console.WriteLine("[ActivityDashboard] Monitoreo pausado por el usuario");
        }
    }

    /// <summary>
    /// Minimiza el formulario a la bandeja del sistema
    /// </summary>
    public void HideToTray()
    {
        try
        {
            if (_notifyIcon != null)
            {
                Hide();
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(2000, "Dashboard de Actividad", 
                    "El dashboard continúa ejecutándose en segundo plano", ToolTipIcon.Info);
                Console.WriteLine("[ActivityDashboard] Minimizado a bandeja del sistema");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error al minimizar a bandeja: {ex.Message}");
        }
    }

    /// <summary>
    /// Muestra el formulario desde la bandeja del sistema
    /// </summary>
    public void ShowFromTray()
    {
        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                Show();
                WindowState = FormWindowState.Normal;
                BringToFront();
                Activate();
                Console.WriteLine("[ActivityDashboard] Restaurado desde bandeja del sistema");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error al restaurar desde bandeja: {ex.Message}");
        }
    }

    /// <summary>
    /// Override del evento FormClosing para manejar minimización a tray
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && _notifyIcon != null)
        {
            // Ask user if they want to minimize to tray or exit
            var result = MessageBox.Show(
                "¿Desea minimizar el dashboard a la bandeja del sistema?\n\n" +
                "Sí: Minimizar a bandeja (continúa ejecutándose)\n" +
                "No: Cerrar completamente el dashboard",
                "Cerrar Dashboard",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (result == DialogResult.Yes)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            else if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            // If No, continue with normal close
        }

        base.OnFormClosing(e);
    }
}