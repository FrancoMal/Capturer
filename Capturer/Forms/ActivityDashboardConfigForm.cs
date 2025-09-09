using System.Drawing;
using System.Windows.Forms;
using Capturer.Models;
using Capturer.Services;

namespace Capturer.Forms;

/// <summary>
/// Formulario de configuración para el Dashboard de Actividad
/// </summary>
public partial class ActivityDashboardConfigForm : Form
{
    private readonly QuadrantService _quadrantService;
    private QuadrantActivityConfiguration _configuration;
    
    // UI Controls
    private ComboBox _cmbQuadrantConfig = new();
    private Panel _quadrantPreviewPanel = new();
    private NumericUpDown _numMonitoringIntervalMinutes = new();
    private NumericUpDown _numMonitoringIntervalSeconds = new();
    private NumericUpDown _numPixelToleranceThreshold = new();
    private NumericUpDown _numActivityThreshold = new();
    private CheckBox _chkEnableMonitoring = new();
    private Button _btnOK = new();
    private Button _btnCancel = new();
    private Label _lblConfigStatus = new();
    private Label _lblPreviewInfo = new();
    
    public QuadrantActivityConfiguration Configuration => _configuration;

    public ActivityDashboardConfigForm(QuadrantService quadrantService, QuadrantActivityConfiguration currentConfig)
    {
        _quadrantService = quadrantService ?? throw new ArgumentNullException(nameof(quadrantService));
        _configuration = new QuadrantActivityConfiguration
        {
            SelectedQuadrantConfigName = currentConfig?.SelectedQuadrantConfigName ?? "Default",
            MonitoringIntervalMinutes = currentConfig?.MonitoringIntervalMinutes ?? 1,
            MonitoringIntervalSeconds = currentConfig?.MonitoringIntervalSeconds ?? 0,
            PixelToleranceThreshold = currentConfig?.PixelToleranceThreshold ?? 10,
            ActivityThresholdPercentage = currentConfig?.ActivityThresholdPercentage ?? 2.0,
            IsMonitoringEnabled = currentConfig?.IsMonitoringEnabled ?? false
        };
        
        InitializeComponent();
        LoadQuadrantConfigurations();
        LoadCurrentSettings();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // Form configuration
        Text = "Configuración del Dashboard de Actividad";
        Size = new Size(720, 600);
        FormBorderStyle = FormBorderStyle.Sizable; // Allow resizing
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        MinimumSize = new Size(650, 500); // Set minimum size

        // Create scrollable container
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(5)
        };

        // Main layout panel with fixed height to ensure scrolling
        var mainPanel = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            Size = new Size(680, 700), // Fixed height larger than form
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(15),
            AutoSize = false
        };

        // Configure columns
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // Configure rows
        for (int i = 0; i < 5; i++)
        {
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Buttons row

        // Section 1: Quadrant Configuration Selection
        var quadrantGroupBox = CreateQuadrantConfigSection();
        mainPanel.Controls.Add(quadrantGroupBox, 0, 0);
        mainPanel.SetColumnSpan(quadrantGroupBox, 2);

        // Section 2: Preview Panel
        var previewGroupBox = CreatePreviewSection();
        mainPanel.Controls.Add(previewGroupBox, 0, 1);
        mainPanel.SetColumnSpan(previewGroupBox, 2);

        // Section 3: Monitoring Interval
        var intervalGroupBox = CreateIntervalSection();
        mainPanel.Controls.Add(intervalGroupBox, 0, 2);

        // Section 4: Comparison Settings
        var comparisonGroupBox = CreateComparisonSection();
        mainPanel.Controls.Add(comparisonGroupBox, 1, 2);

        // Section 5: Activity Settings  
        var activityGroupBox = CreateActivitySection();
        mainPanel.Controls.Add(activityGroupBox, 0, 3);

        // Section 6: Status Information
        var statusGroupBox = CreateStatusSection();
        mainPanel.Controls.Add(statusGroupBox, 1, 3);

        // Buttons
        var buttonPanel = CreateButtonPanel();
        mainPanel.Controls.Add(buttonPanel, 0, 4);
        mainPanel.SetColumnSpan(buttonPanel, 2);

        scrollPanel.Controls.Add(mainPanel);
        Controls.Add(scrollPanel);
        ResumeLayout(false);
    }

    private GroupBox CreateQuadrantConfigSection()
    {
        var groupBox = new GroupBox
        {
            Text = "Configuración de Cuadrantes",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 80,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var label = new Label
        {
            Text = "Seleccionar configuración:",
            Location = new Point(15, 25),
            Size = new Size(150, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _cmbQuadrantConfig.Location = new Point(170, 23);
        _cmbQuadrantConfig.Size = new Size(200, 23);
        _cmbQuadrantConfig.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbQuadrantConfig.Font = new Font("Segoe UI", 9F);
        _cmbQuadrantConfig.SelectedIndexChanged += OnQuadrantConfigChanged;

        var refreshButton = new Button
        {
            Text = "🔄",
            Location = new Point(375, 22),
            Size = new Size(30, 25),
            Font = new Font("Segoe UI", 9F)
        };
        refreshButton.Click += (s, e) => LoadQuadrantConfigurations();
        
        // Add tooltip using ToolTip component
        var refreshToolTip = new ToolTip();
        refreshToolTip.SetToolTip(refreshButton, "Actualizar lista de configuraciones");

        groupBox.Controls.AddRange(new Control[] { label, _cmbQuadrantConfig, refreshButton });
        return groupBox;
    }

    private GroupBox CreatePreviewSection()
    {
        var groupBox = new GroupBox
        {
            Text = "Vista Previa de Cuadrantes",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 200,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _quadrantPreviewPanel = new Panel
        {
            Location = new Point(15, 25),
            Size = new Size(400, 120),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.WhiteSmoke
        };

        _lblPreviewInfo = new Label
        {
            Text = "Seleccione una configuración para ver la vista previa",
            Location = new Point(15, 150),
            Size = new Size(400, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft
        };

        groupBox.Controls.AddRange(new Control[] { _quadrantPreviewPanel, _lblPreviewInfo });
        return groupBox;
    }

    private GroupBox CreateIntervalSection()
    {
        var groupBox = new GroupBox
        {
            Text = "Intervalo de Monitoreo",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 100,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var minutesLabel = new Label
        {
            Text = "Minutos:",
            Location = new Point(15, 30),
            Size = new Size(60, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _numMonitoringIntervalMinutes = new NumericUpDown
        {
            Location = new Point(80, 28),
            Size = new Size(60, 23),
            Minimum = 0,
            Maximum = 59,
            Value = 1,
            Font = new Font("Segoe UI", 9F)
        };

        var secondsLabel = new Label
        {
            Text = "Segundos:",
            Location = new Point(150, 30),
            Size = new Size(60, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _numMonitoringIntervalSeconds = new NumericUpDown
        {
            Location = new Point(215, 28),
            Size = new Size(60, 23),
            Minimum = 0,
            Maximum = 59,
            Value = 0,
            Font = new Font("Segoe UI", 9F)
        };

        var infoLabel = new Label
        {
            Text = "Frecuencia de capturas para comparación",
            Location = new Point(15, 60),
            Size = new Size(260, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { 
            minutesLabel, _numMonitoringIntervalMinutes,
            secondsLabel, _numMonitoringIntervalSeconds, infoLabel 
        });
        return groupBox;
    }

    private GroupBox CreateComparisonSection()
    {
        var groupBox = new GroupBox
        {
            Text = "Configuración de Comparación",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 100,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var toleranceLabel = new Label
        {
            Text = "Tolerancia Pixel (0-255):",
            Location = new Point(15, 30),
            Size = new Size(140, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _numPixelToleranceThreshold = new NumericUpDown
        {
            Location = new Point(160, 28),
            Size = new Size(80, 23),
            Minimum = 0,
            Maximum = 255,
            Value = 10,
            Font = new Font("Segoe UI", 9F)
        };

        var infoLabel = new Label
        {
            Text = "Diferencia mínima entre píxeles para considerar cambio",
            Location = new Point(15, 60),
            Size = new Size(250, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { toleranceLabel, _numPixelToleranceThreshold, infoLabel });
        return groupBox;
    }

    private GroupBox CreateActivitySection()
    {
        var groupBox = new GroupBox
        {
            Text = "Detección de Actividad",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 100,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var thresholdLabel = new Label
        {
            Text = "Umbral de actividad (%):",
            Location = new Point(15, 30),
            Size = new Size(130, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _numActivityThreshold = new NumericUpDown
        {
            Location = new Point(150, 28),
            Size = new Size(80, 23),
            Minimum = 0.1M,
            Maximum = 50M,
            DecimalPlaces = 1,
            Increment = 0.5M,
            Value = 2.0M,
            Font = new Font("Segoe UI", 9F)
        };

        var infoLabel = new Label
        {
            Text = "% mínimo de cambio para detectar actividad",
            Location = new Point(15, 60),
            Size = new Size(250, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { thresholdLabel, _numActivityThreshold, infoLabel });
        return groupBox;
    }

    private GroupBox CreateStatusSection()
    {
        var groupBox = new GroupBox
        {
            Text = "Estado del Monitoreo",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 100,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _chkEnableMonitoring = new CheckBox
        {
            Text = "Habilitar monitoreo automático",
            Location = new Point(15, 25),
            Size = new Size(200, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.DarkGreen
        };

        _lblConfigStatus = new Label
        {
            Text = "Estado: Configuración cargada",
            Location = new Point(15, 55),
            Size = new Size(250, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.DarkBlue
        };

        groupBox.Controls.AddRange(new Control[] { _chkEnableMonitoring, _lblConfigStatus });
        return groupBox;
    }

    private Panel CreateButtonPanel()
    {
        var panel = new Panel
        {
            Height = 50,
            Dock = DockStyle.Fill
        };

        _btnOK = new Button
        {
            Text = "Aplicar Configuración",
            Size = new Size(140, 30),
            Location = new Point(200, 10),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        _btnOK.FlatAppearance.BorderSize = 0;
        _btnOK.Click += OnOKClick;

        _btnCancel = new Button
        {
            Text = "Cancelar",
            Size = new Size(100, 30),
            Location = new Point(350, 10),
            BackColor = Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            DialogResult = DialogResult.Cancel
        };
        _btnCancel.FlatAppearance.BorderSize = 0;

        panel.Controls.AddRange(new Control[] { _btnOK, _btnCancel });
        return panel;
    }

    private void LoadQuadrantConfigurations()
    {
        try
        {
            var configurations = _quadrantService.GetConfigurations();
            
            _cmbQuadrantConfig.Items.Clear();
            
            foreach (var config in configurations)
            {
                _cmbQuadrantConfig.Items.Add(new QuadrantConfigItem(config));
            }

            // Select current configuration
            if (!string.IsNullOrEmpty(_configuration.SelectedQuadrantConfigName))
            {
                for (int i = 0; i < _cmbQuadrantConfig.Items.Count; i++)
                {
                    var item = _cmbQuadrantConfig.Items[i] as QuadrantConfigItem;
                    if (item?.Configuration.Name == _configuration.SelectedQuadrantConfigName)
                    {
                        _cmbQuadrantConfig.SelectedIndex = i;
                        break;
                    }
                }
            }

            UpdateConfigStatus($"Configuraciones cargadas: {configurations.Count}");
        }
        catch (Exception ex)
        {
            UpdateConfigStatus($"Error al cargar configuraciones: {ex.Message}");
            MessageBox.Show($"Error al cargar las configuraciones de cuadrantes: {ex.Message}", 
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void LoadCurrentSettings()
    {
        _numMonitoringIntervalMinutes.Value = _configuration.MonitoringIntervalMinutes;
        _numMonitoringIntervalSeconds.Value = _configuration.MonitoringIntervalSeconds;
        _numPixelToleranceThreshold.Value = _configuration.PixelToleranceThreshold;
        _numActivityThreshold.Value = (decimal)_configuration.ActivityThresholdPercentage;
        _chkEnableMonitoring.Checked = _configuration.IsMonitoringEnabled;
    }

    private void OnQuadrantConfigChanged(object? sender, EventArgs e)
    {
        if (_cmbQuadrantConfig.SelectedItem is QuadrantConfigItem selectedItem)
        {
            _configuration.SelectedQuadrantConfigName = selectedItem.Configuration.Name;
            UpdateQuadrantPreview(selectedItem.Configuration);
        }
    }

    private void UpdateQuadrantPreview(QuadrantConfiguration config)
    {
        _quadrantPreviewPanel.Controls.Clear();
        
        if (config?.Quadrants == null || !config.Quadrants.Any())
        {
            var noDataLabel = new Label
            {
                Text = "No hay cuadrantes definidos en esta configuración",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            _quadrantPreviewPanel.Controls.Add(noDataLabel);
            _lblPreviewInfo.Text = "Configuración sin cuadrantes definidos";
            return;
        }

        // Calculate scale to fit preview
        var maxX = config.Quadrants.Max(r => r.Bounds.Right);
        var maxY = config.Quadrants.Max(r => r.Bounds.Bottom);
        var scaleX = (double)_quadrantPreviewPanel.Width / maxX;
        var scaleY = (double)_quadrantPreviewPanel.Height / maxY;
        var scale = Math.Min(scaleX, scaleY) * 0.9; // Leave some margin

        foreach (var region in config.Quadrants.Where(r => r.IsEnabled))
        {
            var previewRect = new Panel
            {
                Location = new Point((int)(region.Bounds.X * scale), (int)(region.Bounds.Y * scale)),
                Size = new Size((int)(region.Bounds.Width * scale), (int)(region.Bounds.Height * scale)),
                BackColor = Color.FromArgb(100, region.PreviewColor),
                BorderStyle = BorderStyle.FixedSingle
            };

            var label = new Label
            {
                Text = region.Name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };

            previewRect.Controls.Add(label);
            _quadrantPreviewPanel.Controls.Add(previewRect);
        }

        var enabledCount = config.Quadrants.Count(r => r.IsEnabled);
        _lblPreviewInfo.Text = $"Configuración: {config.Name} - {enabledCount} cuadrantes activos - Resolución: {maxX}x{maxY}";
    }

    private void OnOKClick(object? sender, EventArgs e)
    {
        // Validate settings
        if (_cmbQuadrantConfig.SelectedItem == null)
        {
            MessageBox.Show("Por favor seleccione una configuración de cuadrantes.", 
                           "Configuración requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_numMonitoringIntervalMinutes.Value == 0 && _numMonitoringIntervalSeconds.Value == 0)
        {
            MessageBox.Show("El intervalo de monitoreo debe ser mayor a 0 segundos.", 
                           "Intervalo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Update configuration
        _configuration.MonitoringIntervalMinutes = (int)_numMonitoringIntervalMinutes.Value;
        _configuration.MonitoringIntervalSeconds = (int)_numMonitoringIntervalSeconds.Value;
        _configuration.PixelToleranceThreshold = (int)_numPixelToleranceThreshold.Value;
        _configuration.ActivityThresholdPercentage = (double)_numActivityThreshold.Value;
        _configuration.IsMonitoringEnabled = _chkEnableMonitoring.Checked;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdateConfigStatus(string message)
    {
        if (_lblConfigStatus != null)
        {
            _lblConfigStatus.Text = $"Estado: {message}";
        }
    }

    /// <summary>
    /// Item wrapper for ComboBox display
    /// </summary>
    private class QuadrantConfigItem
    {
        public QuadrantConfiguration Configuration { get; }

        public QuadrantConfigItem(QuadrantConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override string ToString()
        {
            var activeCount = Configuration.Quadrants?.Count(r => r.IsEnabled) ?? 0;
            var status = Configuration.IsActive ? " (Activa)" : "";
            return $"{Configuration.Name} - {activeCount} cuadrantes{status}";
        }
    }
}

/// <summary>
/// Configuración específica para el Dashboard de Actividad
/// </summary>
public class QuadrantActivityConfiguration
{
    public string SelectedQuadrantConfigName { get; set; } = "Default";
    public int MonitoringIntervalMinutes { get; set; } = 1;
    public int MonitoringIntervalSeconds { get; set; } = 0;
    public int PixelToleranceThreshold { get; set; } = 10;
    public double ActivityThresholdPercentage { get; set; } = 2.0;
    public bool IsMonitoringEnabled { get; set; } = false;
    
    /// <summary>
    /// Obtiene el intervalo total en milisegundos
    /// </summary>
    public int TotalIntervalMilliseconds => (MonitoringIntervalMinutes * 60 + MonitoringIntervalSeconds) * 1000;
    
    /// <summary>
    /// Obtiene el intervalo como TimeSpan
    /// </summary>
    public TimeSpan MonitoringInterval => TimeSpan.FromMinutes(MonitoringIntervalMinutes).Add(TimeSpan.FromSeconds(MonitoringIntervalSeconds));
}