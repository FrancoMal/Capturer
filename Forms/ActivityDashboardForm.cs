using System.Drawing;
using System.Windows.Forms;
using Capturer.Services;
using Capturer.Models;
using Capturer.Forms;

namespace Capturer.Forms;

/// <summary>
/// Dashboard en tiempo real para monitorear actividad de cuadrantes
/// </summary>
public partial class ActivityDashboardForm : Form
{
    private readonly QuadrantActivityService _activityService;
    private readonly QuadrantService _quadrantService;
    private readonly ActivityReportService _reportService;
    private readonly ActivityDashboardSchedulerService? _schedulerService;
    private readonly SimplifiedReportsSchedulerService? _simplifiedScheduler;
    private readonly IEmailService? _emailService;
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

    private CapturerConfiguration? _capturerConfig;

    public ActivityDashboardForm(QuadrantActivityService activityService, QuadrantService quadrantService, ActivityReportService reportService, ActivityDashboardSchedulerService? schedulerService = null, SimplifiedReportsSchedulerService? simplifiedScheduler = null, CapturerConfiguration? config = null, IEmailService? emailService = null)
    {
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _quadrantService = quadrantService ?? throw new ArgumentNullException(nameof(quadrantService));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _schedulerService = schedulerService; // Legacy scheduler (optional)
        _simplifiedScheduler = simplifiedScheduler; // New simplified scheduler  
        _capturerConfig = config; // Store for later use
        _emailService = emailService; // Store for later use
        
        // Subscribe to simplified scheduler events if available (preferred)
        if (_simplifiedScheduler != null)
        {
            _simplifiedScheduler.EmailSent += OnSimplifiedEmailSent;
            Console.WriteLine("[ActivityDashboardForm] Usando SimplifiedReportsScheduler");
        }
        // Fallback to legacy scheduler
        else if (_schedulerService != null)
        {
            _schedulerService.DailyReportGenerated += OnDailyReportGenerated;
            _schedulerService.DailyReportEmailSent += OnDailyReportEmailSent;
            Console.WriteLine("[ActivityDashboardForm] Usando ActivityDashboardScheduler (legacy)");
        }
        else
        {
            Console.WriteLine("[ActivityDashboardForm] Sin scheduler service - funcionalidad de reportes deshabilitada");
        }
        
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

        // Panel principal con distribución optimizada: Activity arriba, Table abajo, Config derecha
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2, // 2 columns: Main content | Configuration
            RowCount = 2,    // 2 rows: Activity bars | Table
            Padding = new Padding(8)
        };

        // Configurar columnas: Main content (left) | Configuration (right)
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F)); // Main content area
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F)); // Configuration panel
        
        // Configurar filas: Activity bars (top) | Table (bottom)
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F)); // Activity visualization
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F)); // Table area

        // Panel de visualización de cuadrantes (arriba, toda la fila)
        _quadrantVisualPanel = CreateQuadrantVisualPanel();
        mainPanel.Controls.Add(_quadrantVisualPanel, 0, 0);

        // Grid de estadísticas (abajo izquierda - área principal)
        _statsGrid = CreateStatsGrid();
        var statsContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        statsContainer.Controls.Add(_statsGrid);
        mainPanel.Controls.Add(statsContainer, 0, 1);

        // Panel de configuración (derecha, span 2 filas)
        _configPanel = CreateConfigurationPanel();
        mainPanel.Controls.Add(_configPanel, 1, 0);
        mainPanel.SetRowSpan(_configPanel, 2); // Span both rows

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

        // Create scroll container for configuration content
        var scrollContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.AliceBlue,
            Padding = new Padding(5)
        };

        var contentPanel = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            Size = new Size(250, 600), // Fixed size to trigger scrolling when needed
            ColumnCount = 1,
            RowCount = 16,
            Padding = new Padding(5),
            AutoSize = false, // Keep fixed to ensure scrolling works
            BackColor = Color.AliceBlue
        };

        // Configurar filas con spacing más compacto
        for (int i = 0; i < 14; i++)  
        {
            contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F)); // Fixed compact height
        }
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F)); // Export buttons compact
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F)); 
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
            Text = "🔄 Reset",
            Size = new Size(140, 26), // Compact size
            BackColor = Color.Orange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(1)
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
            Text = "⚙️ Configurar",
            Size = new Size(140, 26), // Compact size
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold), // Smaller font
            Anchor = AnchorStyles.Left,
            Margin = new Padding(1)
        };
        configButton.FlatAppearance.BorderSize = 0;
        configButton.Click += OnConfigureButtonClick;

        // Botón de configuración de reportes diarios
        var dailyReportsButton = new Button
        {
            Text = (_simplifiedScheduler != null || _schedulerService != null) ? "📅 Reportes" : "📅 No disponible",
            Size = new Size(140, 26), // Compact size
            BackColor = (_simplifiedScheduler != null || _schedulerService != null) ? Color.FromArgb(156, 39, 176) : Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold), // Consistent smaller font
            Anchor = AnchorStyles.Left,
            Enabled = (_simplifiedScheduler != null || _schedulerService != null)
        };
        dailyReportsButton.FlatAppearance.BorderSize = 0;
        dailyReportsButton.Click += OnDailyReportsConfigClick;

        // Botón para guardar configuración del scheduler
        var saveConfigButton = new Button
        {
            Text = _schedulerService != null ? "💾 Guardar Config" : "💾 (No disponible)",
            Size = new Size(140, 30),
            BackColor = _schedulerService != null ? Color.FromArgb(46, 125, 50) : Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Anchor = AnchorStyles.Left,
            Enabled = (_simplifiedScheduler != null || _schedulerService != null)
        };
        saveConfigButton.FlatAppearance.BorderSize = 0;
        saveConfigButton.Click += OnSaveSchedulerConfigClick;

        // Botón para enviar email de prueba
        var testEmailButton = new Button
        {
            Text = _schedulerService != null ? "🧪 Prueba Email" : "🧪 (No disponible)",
            Size = new Size(130, 30),
            BackColor = _schedulerService != null ? Color.FromArgb(255, 152, 0) : Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Anchor = AnchorStyles.Left,
            Enabled = (_simplifiedScheduler != null || _schedulerService != null)
        };
        testEmailButton.FlatAppearance.BorderSize = 0;
        testEmailButton.Click += OnTestEmailClick;

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

        // Agregar controles al panel con spacing más compacto
        contentPanel.Controls.Add(configButton, 0, 0);
        contentPanel.Controls.Add(dailyReportsButton, 0, 1);
        contentPanel.Controls.Add(saveConfigButton, 0, 2);
        contentPanel.Controls.Add(testEmailButton, 0, 3);
        contentPanel.Controls.Add(separatorLabel, 0, 4);
        contentPanel.Controls.Add(_pauseResumeButton, 0, 5);
        contentPanel.Controls.Add(thresholdLabel, 0, 6);
        contentPanel.Controls.Add(thresholdNumeric, 0, 7);
        contentPanel.Controls.Add(toleranceLabel, 0, 8);
        contentPanel.Controls.Add(toleranceNumeric, 0, 9);
        contentPanel.Controls.Add(resetButton, 0, 10);
        contentPanel.Controls.Add(infoLabel, 0, 11);
        contentPanel.Controls.Add(_countdownLabel!, 0, 12);
        contentPanel.Controls.Add(exportSeparatorLabel, 0, 13);
        contentPanel.Controls.Add(exportHtmlButton, 0, 14);
        contentPanel.Controls.Add(exportCsvButton, 0, 15);

        // Add content panel to scroll container, then scroll to main panel
        scrollContainer.Controls.Add(contentPanel);
        panel.Controls.Add(scrollContainer);
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
        var y = parent.Controls.Count * 40 + 10; // Reduced spacing from 60 to 40

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
            Location = new Point(10, y + 22),
            Size = new Size(300, 15), // Reduced height from 20 to 15
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous,
            Margin = new Padding(2, 1, 2, 1) // Reduced margins
        };

        // Adjust label size and spacing
        statusLabel.Size = new Size(300, 18); // Reduced from 20 to 18
        statusLabel.Location = new Point(10, y);

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
            // Check if system tray is enabled for ActivityDashboard
            if (_capturerConfig?.Application.SystemTray.EnableActivityDashboardSystemTray != true)
            {
                Console.WriteLine("[ActivityDashboard] System tray deshabilitado en configuración");
                // Ensure NotifyIcon is null and not created
                _notifyIcon = null;
                return; // Don't create system tray if disabled
            }

            Console.WriteLine("[ActivityDashboard] Creando system tray habilitado");

            // Create system tray icon
            _notifyIcon = new NotifyIcon
            {
                Text = "Dashboard de Actividad - Capturer",
                Visible = false // Start invisible, will be shown only when needed
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
            _schedulerService?.Dispose(); // Dispose scheduler service
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
    /// Updates system tray configuration when settings change
    /// </summary>
    public void UpdateSystemTrayConfiguration(CapturerConfiguration? newConfig = null)
    {
        try
        {
            // Update configuration if provided
            if (newConfig != null)
            {
                var oldConfig = _capturerConfig;
                _capturerConfig = newConfig;
                Console.WriteLine($"[ActivityDashboard] Configuración actualizada - System tray enabled: {_capturerConfig?.Application.SystemTray.EnableActivityDashboardSystemTray}");
            }

            bool shouldHaveSystemTray = _capturerConfig?.Application.SystemTray.EnableActivityDashboardSystemTray == true;

            if (shouldHaveSystemTray && _notifyIcon == null)
            {
                // System tray was disabled but now should be enabled - recreate it
                Console.WriteLine("[ActivityDashboard] Habilitando system tray...");
                SetupSystemTray();
            }
            else if (!shouldHaveSystemTray && _notifyIcon != null)
            {
                // System tray was enabled but now should be disabled - dispose it
                Console.WriteLine("[ActivityDashboard] Deshabilitando system tray...");
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
                _trayContextMenu?.Dispose();
                _trayContextMenu = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error actualizando configuración system tray: {ex.Message}");
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
            // Only hide to tray if system tray is enabled AND NotifyIcon was created
            if (_notifyIcon != null && _capturerConfig?.Application.SystemTray.EnableActivityDashboardSystemTray == true)
            {
                Hide();
                _notifyIcon.Visible = true;
                
                // Show notification only if enabled
                if (_capturerConfig?.Application.SystemTray.ShowTrayNotifications == true)
                {
                    int duration = _capturerConfig?.Application.SystemTray.NotificationDurationMs ?? 3000;
                    _notifyIcon.ShowBalloonTip(duration, "Dashboard de Actividad", 
                        "El dashboard continúa ejecutándose en segundo plano", ToolTipIcon.Info);
                }
                Console.WriteLine("[ActivityDashboard] Minimizado a bandeja del sistema");
            }
            else
            {
                // Fallback to normal minimize if system tray is disabled
                WindowState = FormWindowState.Minimized;
                Console.WriteLine($"[ActivityDashboard] Minimizado normalmente (system tray deshabilitado) - NotifyIcon: {_notifyIcon != null}, Enabled: {_capturerConfig?.Application.SystemTray.EnableActivityDashboardSystemTray}");
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
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // Check if system tray is enabled and should hide on close
            if (_capturerConfig?.Application.SystemTray.EnableActivityDashboardSystemTray == true &&
                _capturerConfig?.Application.SystemTray.HideOnClose == true &&
                _notifyIcon != null)
            {
                // Automatically hide to tray without asking
                e.Cancel = true;
                HideToTray();
                return;
            }
            else if (_notifyIcon != null && _capturerConfig?.Application.SystemTray.EnableActivityDashboardSystemTray == true)
            {
                // System tray is enabled but HideOnClose is false - ask user
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
            // If system tray is disabled, just close normally
        }

        base.OnFormClosing(e);
    }

    /// <summary>
    /// Maneja el clic en el botón de configuración de reportes diarios
    /// </summary>
    private async void OnDailyReportsConfigClick(object? sender, EventArgs e)
    {
        if (_simplifiedScheduler == null && _schedulerService == null)
        {
            var errorMessage = "El servicio de reportes programados no está disponible.\n\n" +
                              "Causas posibles:\n" +
                              "• El dashboard se inició sin configuración de Capturer\n" +
                              "• Error en la inicialización del servicio\n" +
                              "• Dependencias faltantes\n\n" +
                              "Solución: Reinicie la aplicación principal de Capturer e intente nuevamente.";
            
            MessageBox.Show(errorMessage, "Servicio no disponible", 
                           MessageBoxButtons.OK, MessageBoxIcon.Warning);
            
            Console.WriteLine($"[ActivityDashboard] Scheduler service is null - Dashboard was likely initialized without CapturerConfiguration");
            return;
        }

        try
        {
            Console.WriteLine($"[ActivityDashboard] Abriendo configuración de reportes diarios...");
            
            // Use new SIMPLIFIED configuration form (preferred)
            if (_simplifiedScheduler != null)
            {
                var capturerConfig = GetCapturerConfiguration();
                var newConfig = await _simplifiedScheduler.ShowConfigurationDialogAsync(capturerConfig);
                
                MessageBox.Show("✅ Configuración de reportes guardada exitosamente.\n\n" +
                               $"Modo: {(newConfig.IsDaily ? "Diario (HTML)" : "Semanal (ZIP)")}\n" +
                               $"Hora: {newConfig.EmailTime:hh\\:mm}\n" +
                               $"Destinatarios: {newConfig.SelectedRecipients.Count}\n\n" +
                               "Los reportes se enviarán automáticamente según la configuración.",
                               "Configuración guardada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                Console.WriteLine($"[ActivityDashboard] Nueva configuración simplificada guardada");
            }
            else if (_schedulerService != null)
            {
                // Legacy fallback - get required configuration
                var currentConfig = _schedulerService.GetCurrentConfiguration();
                var capturerConfig = GetCapturerConfiguration();
                
                using var configForm = new ActivityDashboardReportsConfigForm(capturerConfig, currentConfig);
                var result = configForm.ShowDialog(this);
                
                if (result == DialogResult.OK)
                {
                    _schedulerService.ConfigureDailyReports(configForm.ScheduleConfig);
                    MessageBox.Show("✅ Configuración guardada (modo legacy).", "Configuración guardada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Console.WriteLine($"[ActivityDashboard] Configuración legacy actualizada");
                }
            }
            else
            {
                MessageBox.Show("❌ No hay servicio de reportes disponible.\nReinicie la aplicación para habilitar reportes.",
                               "Servicio no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            var detailedError = $"Error al configurar reportes diarios:\n\n" +
                               $"Mensaje: {ex.Message}\n" +
                               $"Tipo: {ex.GetType().Name}\n\n" +
                               "Contacte al soporte técnico si el error persiste.";
            
            MessageBox.Show(detailedError, "Error de configuración", 
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            Console.WriteLine($"[ActivityDashboard] Error configurando reportes diarios: {ex}");
        }
    }

    /// <summary>
    /// Maneja el evento de reporte diario generado
    /// </summary>
    private void OnDailyReportGenerated(object? sender, DailyReportGeneratedEventArgs e)
    {
        try
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDailyReportGenerated(sender, e));
                return;
            }

            // Mostrar notificación en system tray si está minimizado
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                var dayName = System.Globalization.CultureInfo.GetCultureInfo("es-ES")
                    .DateTimeFormat.GetDayName(e.ReportDate.DayOfWeek);
                
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Reporte Diario Generado",
                    $"✅ {dayName} {e.ReportDate:dd/MM/yyyy}\n" +
                    $"📊 {e.QuadrantCount} cuadrantes procesados\n" +
                    $"📁 {e.GeneratedFiles.Count} archivos generados",
                    ToolTipIcon.Info
                );
            }

            Console.WriteLine($"[ActivityDashboard] Notificación de reporte diario: {e.ReportDate:yyyy-MM-dd}, {e.GeneratedFiles.Count} archivos");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error manejando evento de reporte diario: {ex.Message}");
        }
    }

    /// <summary>
    /// Manejador de evento cuando se envía un email de reporte diario
    /// </summary>
    private void OnDailyReportEmailSent(object? sender, DailyReportEmailSentEventArgs e)
    {
        try
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDailyReportEmailSent(sender, e));
                return;
            }

            // Mostrar notificación de email enviado
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                var dayName = System.Globalization.CultureInfo.GetCultureInfo("es-ES")
                    .DateTimeFormat.GetDayName(e.ReportDate.DayOfWeek);
                
                var emailIcon = e.IsTest ? "🧪" : "📧";
                var testPrefix = e.IsTest ? "[PRUEBA] " : "";
                
                _notifyIcon.ShowBalloonTip(
                    5000,
                    $"{testPrefix}Email Enviado",
                    $"{emailIcon} {dayName} {e.ReportDate:dd/MM/yyyy}\n" +
                    $"📁 {e.FilesCount} archivos adjuntos\n" +
                    $"📧 {e.Recipients.Count} destinatarios",
                    ToolTipIcon.Info
                );
            }

            Console.WriteLine($"[ActivityDashboard] Email enviado: {e.ReportDate:yyyy-MM-dd}, {e.FilesCount} archivos, {e.Recipients.Count} destinatarios{(e.IsTest ? " [TEST]" : "")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error manejando evento de email enviado: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja eventos de email del SimplifiedReportsSchedulerService
    /// </summary>
    private void OnSimplifiedEmailSent(object? sender, ReportEmailSentEventArgs e)
    {
        try
        {
            if (InvokeRequired)
            {
                Invoke(() => OnSimplifiedEmailSent(sender, e));
                return;
            }

            // Mostrar notificación simplificada
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                var statusIcon = e.Success ? "✅" : "❌";
                var reportTypeText = e.ReportType == "Daily" ? "Reporte Diario" : "Reporte Semanal";
                
                _notifyIcon.ShowBalloonTip(
                    4000,
                    $"{statusIcon} {reportTypeText}",
                    e.Success 
                        ? $"📧 Enviado a {e.Recipients} destinatarios\n📎 {e.AttachmentCount} archivos adjuntos"
                        : $"❌ Error: {e.ErrorMessage}",
                    e.Success ? ToolTipIcon.Info : ToolTipIcon.Error
                );
            }

            Console.WriteLine($"[ActivityDashboard] Email {e.ReportType}: {(e.Success ? "exitoso" : "falló")} - {e.Recipients} destinatarios, {e.AttachmentCount} archivos");
            
            if (!e.Success && !string.IsNullOrEmpty(e.ErrorMessage))
            {
                Console.WriteLine($"[ActivityDashboard] Error details: {e.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboard] Error manejando evento simplificado: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene la configuración de Capturer actual o una configuración por defecto
    /// </summary>
    private CapturerConfiguration GetCapturerConfiguration()
    {
        // Use the actual configuration if available, otherwise return a default
        if (_capturerConfig != null)
        {
            Console.WriteLine($"[ActivityDashboard] Usando configuración real de Capturer");
            return _capturerConfig;
        }
        
        Console.WriteLine($"[ActivityDashboard] Usando configuración por defecto - configuración real no disponible");
        
        // Fallback to default configuration
        return new CapturerConfiguration
        {
            Schedule = new ScheduleSettings
            {
                EnableAutomaticReports = true,
                ReportTime = TimeSpan.FromHours(9),
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(18),
                IncludeWeekends = false,
                ActiveWeekDays = new List<DayOfWeek> 
                { 
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
                    DayOfWeek.Thursday, DayOfWeek.Friday 
                }
            },
            QuadrantSystem = new QuadrantSystemSettings
            {
                ActiveConfigurationName = "Default",
                Configurations = new List<QuadrantConfiguration>()
            }
        };
    }

    /// <summary>
    /// Maneja el clic en el botón de guardar configuración del scheduler
    /// </summary>
    private void OnSaveSchedulerConfigClick(object? sender, EventArgs e)
    {
        try
        {
            if (_schedulerService == null)
            {
                MessageBox.Show("El servicio de reportes programados no está disponible.",
                    "Servicio no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // La configuración ya se guarda automáticamente en el ActivityDashboardSchedulerService
            // Este botón sirve para confirmar que la configuración actual está guardada
            MessageBox.Show("✅ Configuración del programador de reportes guardada exitosamente.\n\n" +
                           "Los reportes se generarán automáticamente según la programación establecida.\n" +
                           "La configuración se mantiene entre reinicios de la aplicación.",
                           "Configuración guardada", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Console.WriteLine("[ActivityDashboard] Configuración del scheduler confirmada como guardada");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar la configuración: {ex.Message}",
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"[ActivityDashboard] Error guardando configuración: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja el clic en el botón de enviar email de prueba
    /// </summary>
    private async void OnTestEmailClick(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            try
            {
                if (_schedulerService == null)
                {
                    MessageBox.Show("El servicio de reportes programados no está disponible.",
                        "Servicio no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Deshabilitar botón y mostrar progreso
                button.Enabled = false;
                button.Text = "⏳ Enviando...";
                button.BackColor = Color.Gray;

                Console.WriteLine("[ActivityDashboard] Iniciando envío de email de prueba...");

                // Intentar enviar email de prueba
                bool success = await _schedulerService.SendTestEmailAsync();

                if (success)
                {
                    MessageBox.Show("🎉 ¡Email de prueba enviado exitosamente!\n\n" +
                                   "Verifique la bandeja de entrada de los destinatarios configurados.\n" +
                                   "El asunto del email incluirá '[TEST]' para identificarlo como prueba.\n\n" +
                                   "Si no recibe el email, verifique la configuración SMTP en Configuración > Email.",
                                   "Email enviado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("⚠️ No se pudo enviar el email de prueba.\n\n" +
                                   "Posibles causas:\n" +
                                   "• Email deshabilitado o sin destinatarios configurados\n" +
                                   "• No hay reportes generados para enviar\n" +
                                   "• Error en la configuración SMTP\n" +
                                   "• Problema de conectividad\n\n" +
                                   "Configure los reportes diarios y la configuración de email, luego inténtelo nuevamente.",
                                   "Error en envío", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                Console.WriteLine($"[ActivityDashboard] Email de prueba - Resultado: {(success ? "Exitoso" : "Fallido")}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar email de prueba:\n\n{ex.Message}\n\n" +
                               "Verifique la configuración de email y la conectividad de red.",
                               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"[ActivityDashboard] Error enviando email de prueba: {ex}");
            }
            finally
            {
                // Restaurar botón
                button.Enabled = true;
                button.Text = "🧪 Prueba Email";
                button.BackColor = Color.FromArgb(255, 152, 0);
            }
        }
    }

}