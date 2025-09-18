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
    // ★ v3.2.2: Buttons now created directly in form - no need for class fields
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

        // ★ NEW v3.2.2: SIGNIFICANTLY IMPROVED for better readability
        Text = "⚙️ Configuración Dashboard de Actividad v3.2.2";
        Size = new Size(900, 650); // MUCH LARGER for better visibility
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(248, 249, 250); // Modern light background
        MinimumSize = new Size(850, 600); // Larger minimum size

        // ★ v3.2.2: Enhanced scrollable container with better layout
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10),
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 70) // Leave space for buttons
        };

        // ★ v3.2.2: Improved main layout with better spacing
        var mainPanel = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            Size = new Size(850, 650), // Better proportions
            ColumnCount = 2,
            RowCount = 7, // Added row for better spacing
            Padding = new Padding(20),
            AutoSize = false,
            BackColor = Color.White
        };

        // ★ v3.2.2: Better column configuration with more space
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // ★ v3.2.2: Better row configuration with spacing
        for (int i = 0; i < 6; i++)
        {
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Buttons row with more height

        // ★ v3.2.2: Better organized sections with improved spacing
        var quadrantGroupBox = CreateQuadrantConfigSection();
        quadrantGroupBox.Margin = new Padding(5, 5, 5, 15);
        mainPanel.Controls.Add(quadrantGroupBox, 0, 0);
        mainPanel.SetColumnSpan(quadrantGroupBox, 2);

        var previewGroupBox = CreatePreviewSection();
        previewGroupBox.Margin = new Padding(5, 5, 5, 15);
        mainPanel.Controls.Add(previewGroupBox, 0, 1);
        mainPanel.SetColumnSpan(previewGroupBox, 2);

        var intervalGroupBox = CreateIntervalSection();
        intervalGroupBox.Margin = new Padding(5, 5, 10, 15);
        mainPanel.Controls.Add(intervalGroupBox, 0, 2);

        var comparisonGroupBox = CreateComparisonSection();
        comparisonGroupBox.Margin = new Padding(10, 5, 5, 15);
        mainPanel.Controls.Add(comparisonGroupBox, 1, 2);

        var activityGroupBox = CreateActivitySection();
        activityGroupBox.Margin = new Padding(5, 5, 10, 15);
        mainPanel.Controls.Add(activityGroupBox, 0, 3);

        var statusGroupBox = CreateStatusSection();
        statusGroupBox.Margin = new Padding(10, 5, 5, 15);
        mainPanel.Controls.Add(statusGroupBox, 1, 3);

        // Add a spacer row for better visual separation
        var spacer = new Panel { Height = 20, BackColor = Color.Transparent };
        mainPanel.Controls.Add(spacer, 0, 4);
        mainPanel.SetColumnSpan(spacer, 2);

        scrollPanel.Controls.Add(mainPanel);

        // ★ v3.2.2: Fixed button panel at bottom for consistency
        var fixedButtonPanel = new Panel
        {
            Height = 70,
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(20, 17, 20, 17)
        };

        var btnOK = new Button
        {
            Text = "💾 Aplicar Configuración v3.2.2",
            Size = new Size(210, 35),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.FromArgb(25, 135, 84), // Success green
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnOK.Location = new Point(fixedButtonPanel.Width - 330, 17);
        btnOK.FlatAppearance.BorderSize = 0;
        btnOK.Click += OnOKClick;

        var btnCancel = new Button
        {
            Text = "❌ Cancelar",
            Size = new Size(100, 35),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.FromArgb(220, 53, 69), // Danger red
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.Location = new Point(fixedButtonPanel.Width - 110, 17);
        btnCancel.FlatAppearance.BorderSize = 0;

        fixedButtonPanel.Controls.AddRange(new Control[] { btnOK, btnCancel });

        Controls.Add(scrollPanel);
        Controls.Add(fixedButtonPanel);
        ResumeLayout(false);
    }

    private GroupBox CreateQuadrantConfigSection()
    {
        var groupBox = new GroupBox
        {
            Text = "🎯 Configuración de Cuadrantes",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 90, // ★ v3.2.2: More height for better visibility
            Dock = DockStyle.Fill,
            Padding = new Padding(15), // More padding
            ForeColor = Color.DarkBlue
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
            Text = "🔍 Vista Previa de Cuadrantes",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 220, // ★ v3.2.2: More height for better preview
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ForeColor = Color.DarkGreen
        };

        _quadrantPreviewPanel = new Panel
        {
            Location = new Point(15, 30),
            Size = new Size(450, 130), // ★ v3.2.2: Larger preview area
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.AliceBlue,
            Margin = new Padding(5)
        };

        _lblPreviewInfo = new Label
        {
            Text = "📋 Seleccione una configuración para ver la vista previa v3.2.2",
            Location = new Point(15, 170), // ★ v3.2.2: Adjusted for larger preview
            Size = new Size(450, 25), // Wider and taller
            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
            ForeColor = Color.DarkBlue,
            TextAlign = ContentAlignment.MiddleLeft
        };

        groupBox.Controls.AddRange(new Control[] { _quadrantPreviewPanel, _lblPreviewInfo });
        return groupBox;
    }

    private GroupBox CreateIntervalSection()
    {
        var groupBox = new GroupBox
        {
            Text = "⏰ Intervalo de Monitoreo",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120, // ★ v3.2.2: More height for better layout
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ForeColor = Color.DarkOrange
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
            Text = "🔍 Configuración de Comparación",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120, // ★ v3.2.2: More height for better layout
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ForeColor = Color.DarkRed
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
            Text = "📊 Detección de Actividad",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120, // ★ v3.2.2: More height for better layout
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ForeColor = Color.DarkMagenta
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
            Text = "⚙️ Estado del Monitoreo",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120, // ★ v3.2.2: More height for better layout
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ForeColor = Color.DarkCyan
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

    // ★ v3.2.2: CreateButtonPanel() method removed - buttons now integrated in form

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