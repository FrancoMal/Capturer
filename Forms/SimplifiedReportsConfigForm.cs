using System.Drawing;
using System.Windows.Forms;
using Capturer.Services;
using Capturer.Models;

namespace Capturer.Forms;

/// <summary>
/// Formulario SIMPLIFICADO para configuración de reportes automáticos
/// Elimina complejidad innecesaria y se enfoca en funcionalidad esencial
/// </summary>
public partial class SimplifiedReportsConfigForm : Form
{
    private readonly CapturerConfiguration _config;
    private readonly IEmailService? _emailService;
    
    // Configuración simple
    private SimplifiedReportsConfig _reportsConfig;
    
    // UI Controls - SIMPLIFICADOS
    private RadioButton _rbDaily = new();
    private RadioButton _rbWeekly = new();
    
    // CLARIFICADO: Solo días de recepción de reportes (el sistema registra 24/7)
    private Panel _deliveryDaysPanel = new();
    private Dictionary<DayOfWeek, CheckBox> _deliveryDaysCheckBoxes = new();
    
    // Día de envío semanal
    private ComboBox _cmbWeeklyDeliveryDay = new();
    private Label _lblWeeklyDelivery = new();
    
    // Configuración básica
    private DateTimePicker _dtpEmailTime = new();
    private CheckedListBox _clbRecipients = new();
    
    // NUEVO: Configuración de cuadrantes y monitoreo
    private ComboBox _cmbQuadrantConfig = new();
    private CheckedListBox _clbQuadrants = new();
    private NumericUpDown _numMonitoringIntervalMinutes = new();
    private NumericUpDown _numActivityThreshold = new();
    private NumericUpDown _numPixelTolerance = new();
    
    // Preview y testing
    private Label _lblPreview = new();
    private Label _lblReportPreview = new();
    private Button _btnTestEmail = new();
    private Button _btnPreviewReport = new();
    
    public SimplifiedReportsConfig ReportsConfig => _reportsConfig;

    public SimplifiedReportsConfigForm(CapturerConfiguration config, SimplifiedReportsConfig? currentConfig = null, IEmailService? emailService = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _emailService = emailService;
        _reportsConfig = currentConfig ?? new SimplifiedReportsConfig();
        
        InitializeComponent();
        LoadQuadrantConfigurations();
        LoadRecipients();
        LoadCurrentSettings();
        UpdatePreview();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // Form configuration - EXPANDED for complete configuration
        Text = "📅 Configuración Completa de Reportes Automáticos";
        Size = new Size(650, 850); // Larger to accommodate all sections
        FormBorderStyle = FormBorderStyle.Sizable; // Allow resizing
        MaximizeBox = true;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        MinimumSize = new Size(600, 700);

        // Main scrollable panel
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(15)
        };

        var mainPanel = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            Size = new Size(600, 1000), // Increased size for all sections
            ColumnCount = 1,
            RowCount = 9, // Increased for new sections
            Padding = new Padding(10),
            BackColor = Color.White,
            AutoSize = false
        };

        // Configure rows
        for (int i = 0; i < 8; i++)
        {
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Preview row
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Section 1: Frequency & Days Selection
        var frequencySection = CreateFrequencyAndDaysSection();
        mainPanel.Controls.Add(frequencySection, 0, 0);

        // Section 2: Quadrant Configuration
        var quadrantSection = CreateQuadrantConfigSection();
        mainPanel.Controls.Add(quadrantSection, 0, 1);

        // Section 3: Monitoring Configuration
        var monitoringSection = CreateMonitoringConfigSection();
        mainPanel.Controls.Add(monitoringSection, 0, 2);

        // Section 4: Email Time
        var emailTimeSection = CreateEmailTimeSection();
        mainPanel.Controls.Add(emailTimeSection, 0, 3);

        // Section 5: Recipients
        var recipientsSection = CreateRecipientsSection();
        mainPanel.Controls.Add(recipientsSection, 0, 4);

        // Section 6: Testing
        var testingSection = CreateTestingSection();
        mainPanel.Controls.Add(testingSection, 0, 5);

        // Section 7: Report Preview
        var reportPreviewSection = CreateReportPreviewSection();
        mainPanel.Controls.Add(reportPreviewSection, 0, 6);

        // Section 8: Calendar Preview
        var calendarSection = CreateCalendarPreviewSection();
        mainPanel.Controls.Add(calendarSection, 0, 7);

        // Fixed buttons at bottom
        var buttonPanel = CreateButtonPanel();
        
        scrollPanel.Controls.Add(mainPanel);
        
        Controls.Add(scrollPanel);
        Controls.Add(buttonPanel);
        
        ResumeLayout(false);
    }

    private GroupBox CreateFrequencyAndDaysSection()
    {
        var groupBox = new GroupBox
        {
            Text = "📊 ¿Cuándo quiere recibir reportes?",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 180,
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        var mainPanel = new Panel { Dock = DockStyle.Fill };

        // Frequency selection with clear labels
        var frequencyLabel = new Label
        {
            Text = "Seleccione la frecuencia de reportes:",
            Location = new Point(10, 20),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 58, 64)
        };

        _rbDaily = new RadioButton
        {
            Text = "📅 Diario - Recibir reporte HTML cada día seleccionado",
            Location = new Point(15, 45),
            Size = new Size(400, 25),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 123, 255),
            Checked = true
        };
        _rbDaily.CheckedChanged += OnFrequencyChanged;

        _rbWeekly = new RadioButton
        {
            Text = "📦 Semanal - Recibir ZIP con 7 reportes HTML cada semana",
            Location = new Point(15, 70),
            Size = new Size(400, 25),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(156, 39, 176)
        };
        _rbWeekly.CheckedChanged += OnFrequencyChanged;

        // Days selection panel
        _deliveryDaysPanel = new Panel
        {
            Location = new Point(15, 100),
            Size = new Size(500, 60),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(248, 249, 250)
        };

        var daysLabel = new Label
        {
            Text = "📅 Días de entrega:",
            Location = new Point(10, 10),
            Size = new Size(120, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var dayNames = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
        
        for (int i = 0; i < days.Length; i++)
        {
            var checkbox = new CheckBox
            {
                Text = dayNames[i],
                Location = new Point(10 + (i * 65), 35),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 8F),
                Checked = i < 5, // Default: Monday to Friday
                ForeColor = Color.FromArgb(52, 58, 64)
            };
            checkbox.CheckedChanged += UpdatePreview;
            
            _deliveryDaysCheckBoxes[days[i]] = checkbox;
            _deliveryDaysPanel.Controls.Add(checkbox);
        }

        // Weekly delivery day (initially hidden)
        _lblWeeklyDelivery = new Label
        {
            Text = "📦 Enviar ZIP cada:",
            Location = new Point(140, 10),
            Size = new Size(120, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Visible = false
        };

        _cmbWeeklyDeliveryDay = new ComboBox
        {
            Location = new Point(270, 7),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F),
            Visible = false
        };
        _cmbWeeklyDeliveryDay.Items.AddRange(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" });
        _cmbWeeklyDeliveryDay.SelectedIndex = 0;
        _cmbWeeklyDeliveryDay.SelectedIndexChanged += UpdatePreview;

        _deliveryDaysPanel.Controls.AddRange(new Control[] { daysLabel, _lblWeeklyDelivery, _cmbWeeklyDeliveryDay });

        mainPanel.Controls.AddRange(new Control[] { frequencyLabel, _rbDaily, _rbWeekly, _deliveryDaysPanel });
        groupBox.Controls.Add(mainPanel);
        return groupBox;
    }

    private GroupBox CreateQuadrantConfigSection()
    {
        var groupBox = new GroupBox
        {
            Text = "🧩 ¿Qué cuadrantes incluir en los reportes?",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 140,
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        var configLabel = new Label
        {
            Text = "Configuración de cuadrantes:",
            Location = new Point(10, 25),
            Size = new Size(150, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _cmbQuadrantConfig = new ComboBox
        {
            Location = new Point(170, 22),
            Size = new Size(200, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F)
        };
        _cmbQuadrantConfig.SelectedIndexChanged += OnQuadrantConfigChanged;

        var quadrantsLabel = new Label
        {
            Text = "Cuadrantes a incluir en reportes:",
            Location = new Point(10, 55),
            Size = new Size(200, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _clbQuadrants = new CheckedListBox
        {
            Location = new Point(10, 80),
            Size = new Size(380, 50),
            CheckOnClick = true,
            Font = new Font("Segoe UI", 9F),
            BorderStyle = BorderStyle.FixedSingle
        };
        _clbQuadrants.ItemCheck += (s, e) => UpdatePreview();

        var infoLabel = new Label
        {
            Text = "💡 Solo los cuadrantes seleccionados aparecerán en los reportes HTML",
            Location = new Point(400, 85),
            Size = new Size(150, 40),
            Font = new Font("Segoe UI", 7F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { configLabel, _cmbQuadrantConfig, quadrantsLabel, _clbQuadrants, infoLabel });
        return groupBox;
    }

    private GroupBox CreateMonitoringConfigSection()
    {
        var groupBox = new GroupBox
        {
            Text = "⚙️ ¿Cómo monitorear la actividad?",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120,
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        // Monitoring interval
        var intervalLabel = new Label
        {
            Text = "Intervalo de verificación:",
            Location = new Point(10, 25),
            Size = new Size(130, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _numMonitoringIntervalMinutes = new NumericUpDown
        {
            Location = new Point(150, 22),
            Size = new Size(60, 25),
            Minimum = 1,
            Maximum = 60,
            Value = 5, // 5 minutes default
            Font = new Font("Segoe UI", 9F)
        };
        _numMonitoringIntervalMinutes.ValueChanged += UpdatePreview;

        var minutesLabel = new Label
        {
            Text = "minutos",
            Location = new Point(215, 25),
            Size = new Size(50, 20),
            Font = new Font("Segoe UI", 9F)
        };

        // Activity threshold
        var thresholdLabel = new Label
        {
            Text = "Umbral de actividad:",
            Location = new Point(10, 55),
            Size = new Size(130, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _numActivityThreshold = new NumericUpDown
        {
            Location = new Point(150, 52),
            Size = new Size(60, 25),
            Minimum = 0.1M,
            Maximum = 50.0M,
            DecimalPlaces = 1,
            Value = 2.0M, // 2% default
            Font = new Font("Segoe UI", 9F)
        };
        _numActivityThreshold.ValueChanged += UpdatePreview;

        var percentLabel = new Label
        {
            Text = "% cambio",
            Location = new Point(215, 55),
            Size = new Size(60, 20),
            Font = new Font("Segoe UI", 9F)
        };

        // Pixel tolerance
        var pixelLabel = new Label
        {
            Text = "Tolerancia pixel:",
            Location = new Point(300, 25),
            Size = new Size(100, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _numPixelTolerance = new NumericUpDown
        {
            Location = new Point(410, 22),
            Size = new Size(60, 25),
            Minimum = 1,
            Maximum = 50,
            Value = 10, // 10 pixels default
            Font = new Font("Segoe UI", 9F)
        };
        _numPixelTolerance.ValueChanged += UpdatePreview;

        var pixelsLabel = new Label
        {
            Text = "píxeles",
            Location = new Point(475, 25),
            Size = new Size(50, 20),
            Font = new Font("Segoe UI", 9F)
        };

        var monitoringInfoLabel = new Label
        {
            Text = "💡 El sistema compara screenshots cada [intervalo] y detecta actividad cuando el cambio supera el [umbral]",
            Location = new Point(10, 85),
            Size = new Size(520, 30),
            Font = new Font("Segoe UI", 7F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { 
            intervalLabel, _numMonitoringIntervalMinutes, minutesLabel,
            thresholdLabel, _numActivityThreshold, percentLabel,
            pixelLabel, _numPixelTolerance, pixelsLabel,
            monitoringInfoLabel
        });
        return groupBox;
    }

    private GroupBox CreateReportPreviewSection()
    {
        var groupBox = new GroupBox
        {
            Text = "📊 Vista Previa - ¿Qué contendrán los reportes?",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120,
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        _lblReportPreview = new Label
        {
            Location = new Point(10, 25),
            Size = new Size(520, 60),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(52, 58, 64),
            Text = "Configure cuadrantes y parámetros arriba para ver qué incluirán los reportes",
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(255, 248, 225),
            Padding = new Padding(10)
        };

        _btnPreviewReport = new Button
        {
            Text = "🔍 Generar Vista Previa del Reporte",
            Location = new Point(10, 90),
            Size = new Size(250, 25),
            BackColor = Color.FromArgb(108, 99, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
        };
        _btnPreviewReport.FlatAppearance.BorderSize = 0;
        _btnPreviewReport.Click += OnPreviewReportClick;

        groupBox.Controls.AddRange(new Control[] { _lblReportPreview, _btnPreviewReport });
        return groupBox;
    }

    private GroupBox CreateEmailTimeSection()
    {
        var groupBox = new GroupBox
        {
            Text = "⏰ Hora de Envío",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 80,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var label = new Label
        {
            Text = "Hora:",
            Location = new Point(15, 30),
            Size = new Size(50, 25),
            Font = new Font("Segoe UI", 9F)
        };

        _dtpEmailTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(70, 27),
            Size = new Size(100, 25),
            Value = DateTime.Today.AddHours(23), // Default: 11:00 PM
            Font = new Font("Segoe UI", 9F)
        };
        _dtpEmailTime.ValueChanged += UpdatePreview;

        var infoLabel = new Label
        {
            Text = "💡 Hora local del sistema para envío automático",
            Location = new Point(180, 30),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { label, _dtpEmailTime, infoLabel });
        return groupBox;
    }

    private GroupBox CreateRecipientsSection()
    {
        var groupBox = new GroupBox
        {
            Text = "📧 ¿Quién debe recibir los reportes?",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 140,
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        var instructionLabel = new Label
        {
            Text = "Seleccione destinatarios de la configuración de Capturer:",
            Location = new Point(10, 25),
            Size = new Size(400, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 58, 64)
        };

        _clbRecipients = new CheckedListBox
        {
            Location = new Point(10, 50),
            Size = new Size(350, 80),
            CheckOnClick = true,
            Font = new Font("Segoe UI", 9F),
            BorderStyle = BorderStyle.FixedSingle
        };
        _clbRecipients.ItemCheck += (s, e) => UpdatePreview();

        // Add recipient button
        var btnAddRecipient = new Button
        {
            Text = "➕ Agregar",
            Location = new Point(370, 50),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
        };
        btnAddRecipient.FlatAppearance.BorderSize = 0;
        btnAddRecipient.Click += OnAddRecipientClick;

        // Remove recipient button
        var btnRemoveRecipient = new Button
        {
            Text = "➖ Quitar",
            Location = new Point(370, 85),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
        };
        btnRemoveRecipient.FlatAppearance.BorderSize = 0;
        btnRemoveRecipient.Click += OnRemoveRecipientClick;

        var statusLabel = new Label
        {
            Text = "💡 Los emails se envían con la configuración SMTP de Capturer",
            Location = new Point(10, 135),
            Size = new Size(400, 15),
            Font = new Font("Segoe UI", 7F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { instructionLabel, _clbRecipients, btnAddRecipient, btnRemoveRecipient, statusLabel });
        return groupBox;
    }

    private GroupBox CreateTestingSection()
    {
        var groupBox = new GroupBox
        {
            Text = "🧪 Pruebas de Configuración",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 80,
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        var instructionLabel = new Label
        {
            Text = "Pruebe la configuración antes de activarla:",
            Location = new Point(10, 25),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(52, 58, 64)
        };

        _btnTestEmail = new Button
        {
            Text = "📧 Enviar Email de Prueba AHORA",
            Location = new Point(10, 45),
            Size = new Size(220, 30),
            BackColor = Color.FromArgb(0, 150, 136),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        _btnTestEmail.FlatAppearance.BorderSize = 0;
        _btnTestEmail.Click += OnTestEmailClick;

        var validateButton = new Button
        {
            Text = "✅ Validar Config",
            Location = new Point(240, 45),
            Size = new Size(120, 30),
            BackColor = Color.FromArgb(255, 193, 7),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        validateButton.FlatAppearance.BorderSize = 0;
        validateButton.Click += OnValidateConfigClick;

        groupBox.Controls.AddRange(new Control[] { instructionLabel, _btnTestEmail, validateButton });
        return groupBox;
    }

    private GroupBox CreateCalendarPreviewSection()
    {
        var groupBox = new GroupBox
        {
            Text = "📅 Vista Previa - ¿Cuándo llegarán los reportes?",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120,
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        _lblPreview = new Label
        {
            Location = new Point(10, 25),
            Size = new Size(520, 90),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(52, 58, 64),
            Text = "Configure primero las opciones arriba para ver la vista previa",
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(10)
        };

        groupBox.Controls.Add(_lblPreview);
        return groupBox;
    }

    private Panel CreateButtonPanel()
    {
        var panel = new Panel
        {
            Height = 60,
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(248, 249, 250)
        };

        var btnSave = new Button
        {
            Text = "💾 Guardar Configuración",
            Size = new Size(160, 35),
            Location = new Point(250, 12),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSaveClick;

        var btnCancel = new Button
        {
            Text = "❌ Cancelar",
            Size = new Size(100, 35),
            Location = new Point(420, 12),
            BackColor = Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        panel.Controls.AddRange(new Control[] { btnSave, btnCancel });
        return panel;
    }

    private void OnFrequencyChanged(object? sender, EventArgs e)
    {
        var isDailySelected = _rbDaily.Checked;
        
        if (isDailySelected)
        {
            // Show daily checkboxes, hide weekly combo
            foreach (var checkbox in _deliveryDaysCheckBoxes.Values)
            {
                checkbox.Visible = true;
            }
            _lblWeeklyDelivery.Visible = false;
            _cmbWeeklyDeliveryDay.Visible = false;
            
            // Update days label text
            var daysLabel = _deliveryDaysPanel.Controls.OfType<Label>().FirstOrDefault();
            if (daysLabel != null)
            {
                daysLabel.Text = "📅 Recibir reporte HTML estos días:";
                daysLabel.ForeColor = Color.FromArgb(0, 123, 255);
            }
        }
        else
        {
            // Hide daily checkboxes, show weekly combo
            foreach (var checkbox in _deliveryDaysCheckBoxes.Values)
            {
                checkbox.Visible = false;
            }
            _lblWeeklyDelivery.Visible = true;
            _cmbWeeklyDeliveryDay.Visible = true;
            
            // Update days label text
            var daysLabel = _deliveryDaysPanel.Controls.OfType<Label>().FirstOrDefault();
            if (daysLabel != null)
            {
                daysLabel.Text = "📦 Configuración semanal:";
                daysLabel.ForeColor = Color.FromArgb(156, 39, 176);
            }
        }
        
        UpdatePreview();
    }

    private void LoadRecipients()
    {
        try
        {
            _clbRecipients.Items.Clear();
            
            // Load from Capturer configuration
            Console.WriteLine($"[SimplifiedReportsConfig] Cargando destinatarios desde configuración: {_config.Email.Recipients.Count} encontrados");
            
            foreach (var recipient in _config.Email.Recipients)
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    var isSelected = _reportsConfig.SelectedRecipients.Contains(recipient);
                    _clbRecipients.Items.Add(recipient, isSelected);
                    Console.WriteLine($"[SimplifiedReportsConfig] Destinatario: {recipient} (seleccionado: {isSelected})");
                }
            }

            if (_clbRecipients.Items.Count == 0)
            {
                _clbRecipients.Items.Add("❌ Sin destinatarios configurados en Capturer", false);
                MessageBox.Show("No hay destinatarios configurados en Capturer.\n\n" +
                               "Por favor configure destinatarios en Configuración > Email de Capturer primero.",
                               "Sin destinatarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Console.WriteLine($"[SimplifiedReportsConfig] {_clbRecipients.Items.Count} destinatarios cargados exitosamente");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsConfig] Error cargando destinatarios: {ex.Message}");
            _clbRecipients.Items.Add($"❌ Error cargando destinatarios: {ex.Message}", false);
        }
    }

    private void LoadCurrentSettings()
    {
        try
        {
            // Load frequency
            _rbDaily.Checked = _reportsConfig.IsDaily;
            _rbWeekly.Checked = !_reportsConfig.IsDaily;

            // Load delivery days
            foreach (var (day, checkbox) in _deliveryDaysCheckBoxes)
            {
                checkbox.Checked = _reportsConfig.DeliveryDays.Contains(day);
            }

            // Load email time
            _dtpEmailTime.Value = DateTime.Today.Add(_reportsConfig.EmailTime);

            // Load weekly delivery day
            _cmbWeeklyDeliveryDay.SelectedIndex = (int)_reportsConfig.WeeklyDeliveryDay;

            // Load monitoring configuration
            _numMonitoringIntervalMinutes.Value = _reportsConfig.MonitoringIntervalMinutes;
            _numActivityThreshold.Value = (decimal)_reportsConfig.ActivityThresholdPercentage;
            _numPixelTolerance.Value = _reportsConfig.PixelToleranceThreshold;

            // Load quadrant selection (will be handled by OnQuadrantConfigChanged after LoadQuadrantConfigurations)

            // Update UI state
            OnFrequencyChanged(null, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsConfig] Error cargando configuración: {ex.Message}");
            // Use defaults on error
        }
    }

    private void UpdatePreview(object? sender = null, EventArgs? e = null)
    {
        try
        {
            var preview = new List<string>();
            var today = DateTime.Today;
            var nextDeliveries = new List<string>();
            
            // Generate next 7 days preview
            if (_rbDaily.Checked)
            {
                var deliveryDays = _deliveryDaysCheckBoxes.Where(kvp => kvp.Value.Checked).Select(kvp => kvp.Key).ToList();
                preview.Add("📅 CONFIGURACIÓN: Reportes HTML diarios");
                preview.Add($"⏰ Hora de envío: {_dtpEmailTime.Value:HH:mm}");
                
                if (deliveryDays.Any())
                {
                    preview.Add($"🗓️ Días seleccionados: {string.Join(", ", deliveryDays.Select(GetDayName))}");
                    
                    // Show next 7 days
                    for (int i = 0; i < 7; i++)
                    {
                        var futureDate = today.AddDays(i);
                        if (deliveryDays.Contains(futureDate.DayOfWeek))
                        {
                            var dayStatus = i == 0 ? "HOY" : i == 1 ? "Mañana" : futureDate.ToString("dd/MM");
                            nextDeliveries.Add($"📧 {GetDayName(futureDate.DayOfWeek)} {dayStatus}");
                        }
                    }
                }
                else
                {
                    preview.Add("⚠️ Sin días seleccionados - no se enviarán reportes");
                }
            }
            else
            {
                var weeklyDay = (DayOfWeek)_cmbWeeklyDeliveryDay.SelectedIndex;
                preview.Add("📦 CONFIGURACIÓN: Reportes ZIP semanales");
                preview.Add($"⏰ Hora de envío: {_dtpEmailTime.Value:HH:mm}");
                preview.Add($"🗓️ Día de envío: {GetDayName(weeklyDay)}");
                
                // Find next weekly delivery
                var daysUntilNext = ((int)weeklyDay - (int)today.DayOfWeek + 7) % 7;
                if (daysUntilNext == 0) daysUntilNext = 7; // If today, next week
                
                var nextWeeklyDate = today.AddDays(daysUntilNext);
                var dayDescription = daysUntilNext == 1 ? "Mañana" : daysUntilNext <= 6 ? $"En {daysUntilNext} días" : nextWeeklyDate.ToString("dd/MM");
                nextDeliveries.Add($"📦 Próximo ZIP: {GetDayName(weeklyDay)} {dayDescription}");
            }

            var selectedRecipients = _clbRecipients.CheckedItems.Cast<string>().Where(r => !r.StartsWith("❌")).Count();
            var selectedQuadrants = _clbQuadrants.CheckedItems.Cast<string>().Where(q => !q.StartsWith("❌")).ToList();
            
            preview.Add($"📧 Destinatarios: {selectedRecipients}");
            
            // Add monitoring configuration info
            preview.Add("");
            preview.Add("⚙️ CONFIGURACIÓN DE MONITOREO:");
            preview.Add($"🧩 Cuadrantes: {(selectedQuadrants.Any() ? string.Join(", ", selectedQuadrants) : "Ninguno seleccionado")}");
            preview.Add($"⏱️ Intervalo: Cada {_numMonitoringIntervalMinutes.Value} minutos");
            preview.Add($"🎯 Umbral: {_numActivityThreshold.Value:F1}% cambio de píxeles");
            preview.Add($"📐 Tolerancia: {_numPixelTolerance.Value} píxeles");

            if (nextDeliveries.Any())
            {
                preview.Add("");
                preview.Add("📅 PRÓXIMOS ENVÍOS:");
                preview.AddRange(nextDeliveries);
            }
            
            // Add validation warnings
            if (!selectedQuadrants.Any() && _cmbQuadrantConfig.Enabled)
            {
                preview.Add("");
                preview.Add("⚠️ ADVERTENCIA: Sin cuadrantes seleccionados - reportes estarán vacíos");
            }

            _lblPreview.Text = string.Join("\n", preview);
        }
        catch (Exception ex)
        {
            _lblPreview.Text = $"❌ Error en preview: {ex.Message}";
        }
    }

    private string GetDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Lun",
            DayOfWeek.Tuesday => "Mar",
            DayOfWeek.Wednesday => "Mié",
            DayOfWeek.Thursday => "Jue",
            DayOfWeek.Friday => "Vie",
            DayOfWeek.Saturday => "Sáb",
            DayOfWeek.Sunday => "Dom",
            _ => day.ToString()
        };
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        try
        {
            // Validate basic requirements  
            var deliveryDaysSelected = _deliveryDaysCheckBoxes.Any(kvp => kvp.Value.Checked);
            var recipientsSelected = _clbRecipients.CheckedItems.Cast<string>().Any(r => !r.StartsWith("❌"));

            if (_rbDaily.Checked && !deliveryDaysSelected)
            {
                MessageBox.Show("Para reportes diarios, seleccione al menos un día de entrega.",
                               "Configuración requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!recipientsSelected)
            {
                MessageBox.Show("Por favor seleccione al menos un destinatario.",
                               "Configuración requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate quadrant selection
            var quadrantsSelected = _clbQuadrants.CheckedItems.Cast<string>().Any(q => !q.StartsWith("❌"));
            if (!quadrantsSelected && _cmbQuadrantConfig.Enabled)
            {
                MessageBox.Show("Por favor seleccione al menos un cuadrante para incluir en los reportes.",
                               "Cuadrantes requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // SAFE UPDATE - No parsing, direct assignment  
            _reportsConfig.IsDaily = _rbDaily.Checked;
            _reportsConfig.EmailTime = _dtpEmailTime.Value.TimeOfDay;
            _reportsConfig.WeeklyDeliveryDay = (DayOfWeek)_cmbWeeklyDeliveryDay.SelectedIndex;
            
            // Update delivery days
            _reportsConfig.DeliveryDays.Clear();
            foreach (var (day, checkbox) in _deliveryDaysCheckBoxes)
            {
                if (checkbox.Checked)
                {
                    _reportsConfig.DeliveryDays.Add(day);
                }
            }

            // Update selected recipients
            _reportsConfig.SelectedRecipients.Clear();
            foreach (string recipient in _clbRecipients.CheckedItems)
            {
                if (!recipient.StartsWith("❌"))
                {
                    _reportsConfig.SelectedRecipients.Add(recipient);
                }
            }

            // Update monitoring configuration
            _reportsConfig.MonitoringIntervalMinutes = (int)_numMonitoringIntervalMinutes.Value;
            _reportsConfig.ActivityThresholdPercentage = (double)_numActivityThreshold.Value;
            _reportsConfig.PixelToleranceThreshold = (int)_numPixelTolerance.Value;

            // Update quadrant configuration
            if (_cmbQuadrantConfig.SelectedItem is QuadrantConfigItem selectedConfig)
            {
                _reportsConfig.SelectedQuadrantConfig = selectedConfig.Name;
            }

            _reportsConfig.SelectedQuadrants.Clear();
            foreach (string quadrant in _clbQuadrants.CheckedItems)
            {
                if (!quadrant.StartsWith("❌"))
                {
                    _reportsConfig.SelectedQuadrants.Add(quadrant);
                }
            }

            DialogResult = DialogResult.OK;
            Console.WriteLine("[SimplifiedReportsConfig] Configuración guardada exitosamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsConfig] Error guardando: {ex.Message}");
            MessageBox.Show($"Error guardando configuración: {ex.Message}",
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnTestEmailClick(object? sender, EventArgs e)
    {
        try
        {
            _btnTestEmail.Enabled = false;
            _btnTestEmail.Text = "⏳ Enviando...";

            var selectedRecipients = _clbRecipients.CheckedItems.Cast<string>()
                .Where(r => !r.StartsWith("❌"))
                .ToList();

            if (!selectedRecipients.Any())
            {
                MessageBox.Show("Seleccione al menos un destinatario para la prueba.",
                               "Destinatarios requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_emailService == null)
            {
                MessageBox.Show("Servicio de email no disponible.\nReinicie la aplicación para habilitar pruebas.",
                               "Servicio no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create comprehensive test email body
            var configDescription = _rbDaily.Checked 
                ? $"Diario: {string.Join(", ", _deliveryDaysCheckBoxes.Where(kvp => kvp.Value.Checked).Select(kvp => GetDayName(kvp.Key)))}"
                : $"Semanal cada {_cmbWeeklyDeliveryDay.Text}";

            var testBody = $@"
                <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 600px;'>
                    <h2 style='color: #0066cc;'>🧪 Email de Prueba - Configuración de Reportes</h2>
                    
                    <div style='background: #e3f2fd; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                        <h3 style='color: #1976d2; margin-top: 0;'>📋 Configuración Actual:</h3>
                        <ul style='margin: 10px 0;'>
                            <li><strong>Frecuencia:</strong> {configDescription}</li>
                            <li><strong>Hora de envío:</strong> {_dtpEmailTime.Value:HH:mm}</li>
                            <li><strong>Destinatarios:</strong> {selectedRecipients.Count}</li>
                        </ul>
                    </div>
                    
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                        <p><strong>✅ Si recibe este email, la configuración SMTP está funcionando correctamente.</strong></p>
                        <p>Los reportes automáticos se enviarán según la programación establecida.</p>
                    </div>
                    
                    <hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
                    <p style='color: #6c757d; font-size: 12px;'>
                        🧪 Email de prueba generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br>
                        🖥️ Capturer Dashboard v2.5 - Sistema de Reportes Automáticos
                    </p>
                </div>";

            var subject = $"🧪 Prueba - Configuración Reportes Capturer ({DateTime.Now:dd/MM/yyyy})";

            // Send test email using EmailService
            var success = await _emailService.SendActivityDashboardReportAsync(
                selectedRecipients,
                subject,
                testBody,
                new List<string>(), // No attachments for test
                false
            );

            if (success)
            {
                MessageBox.Show($"✅ Email de prueba enviado exitosamente!\n\n" +
                               $"Destinatarios: {string.Join(", ", selectedRecipients)}\n" +
                               $"Hora: {DateTime.Now:HH:mm:ss}\n\n" +
                               "Revise la bandeja de entrada para confirmar la recepción.",
                               "Prueba exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                Console.WriteLine($"[SimplifiedReportsConfig] Email de prueba enviado exitosamente a {selectedRecipients.Count} destinatarios");
            }
            else
            {
                MessageBox.Show("❌ Error enviando email de prueba.\n\n" +
                               "Verifique:\n" +
                               "• Configuración SMTP en Capturer\n" +
                               "• Conexión a internet\n" +
                               "• Credenciales de email\n\n" +
                               "Consulte los logs para más detalles.",
                               "Error en envío", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                Console.WriteLine($"[SimplifiedReportsConfig] Error enviando email de prueba");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsConfig] Excepción en prueba de email: {ex.Message}");
            MessageBox.Show($"Error inesperado:\n{ex.Message}",
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTestEmail.Enabled = true;
            _btnTestEmail.Text = "📧 Enviar Email de Prueba AHORA";
        }
    }

    private async void OnGenerateTestClick(object? sender, EventArgs e)
    {
        try
        {
            MessageBox.Show("Generaría un reporte de prueba con datos actuales del ActivityDashboard.\n" +
                           "Funcionalidad disponible después de integración completa.",
                           "Función planificada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAddRecipientClick(object? sender, EventArgs e)
    {
        try
        {
            var inputForm = new Form
            {
                Text = "Agregar Destinatario",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "Email del destinatario:",
                Location = new Point(20, 20),
                Size = new Size(150, 20)
            };

            var textBox = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(340, 25),
                Font = new Font("Segoe UI", 9F)
            };

            var btnOK = new Button
            {
                Text = "Agregar",
                Location = new Point(200, 80),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };

            var btnCancel = new Button
            {
                Text = "Cancelar", 
                Location = new Point(290, 80),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            inputForm.Controls.AddRange(new Control[] { label, textBox, btnOK, btnCancel });
            
            if (inputForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var newEmail = textBox.Text.Trim();
                if (IsValidEmail(newEmail))
                {
                    _clbRecipients.Items.Add(newEmail, true);
                    Console.WriteLine($"[SimplifiedReportsConfig] Destinatario agregado: {newEmail}");
                    UpdatePreview();
                }
                else
                {
                    MessageBox.Show("Por favor ingrese un email válido.", "Email inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            inputForm.Dispose();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error agregando destinatario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnRemoveRecipientClick(object? sender, EventArgs e)
    {
        try
        {
            var selectedIndex = _clbRecipients.SelectedIndex;
            if (selectedIndex >= 0)
            {
                var selectedItem = _clbRecipients.Items[selectedIndex].ToString();
                var result = MessageBox.Show($"¿Eliminar destinatario?\n\n{selectedItem}", 
                                           "Confirmar eliminación", 
                                           MessageBoxButtons.YesNo, 
                                           MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    _clbRecipients.Items.RemoveAt(selectedIndex);
                    Console.WriteLine($"[SimplifiedReportsConfig] Destinatario eliminado: {selectedItem}");
                    UpdatePreview();
                }
            }
            else
            {
                MessageBox.Show("Seleccione un destinatario de la lista para eliminar.", 
                               "Selección requerida", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error eliminando destinatario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnValidateConfigClick(object? sender, EventArgs e)
    {
        try
        {
            var validation = ValidateCurrentConfiguration();
            
            if (validation.IsValid)
            {
                MessageBox.Show($"✅ Configuración válida\n\n{validation.Summary}", 
                               "Validación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"❌ Configuración incompleta\n\n{validation.ErrorMessage}", 
                               "Validación falló", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error en validación: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private (bool IsValid, string Summary, string ErrorMessage) ValidateCurrentConfiguration()
    {
        try
        {
            var errors = new List<string>();
            var summary = new List<string>();

            // Check recipients
            var selectedRecipients = _clbRecipients.CheckedItems.Cast<string>()
                .Where(r => !r.StartsWith("❌")).ToList();
            
            if (!selectedRecipients.Any())
            {
                errors.Add("Seleccione al menos un destinatario");
            }
            else
            {
                summary.Add($"📧 {selectedRecipients.Count} destinatarios seleccionados");
            }

            // Check quadrants
            var selectedQuadrants = _clbQuadrants.CheckedItems.Cast<string>()
                .Where(q => !q.StartsWith("❌")).ToList();
            
            if (!selectedQuadrants.Any() && _cmbQuadrantConfig.Enabled)
            {
                errors.Add("Seleccione al menos un cuadrante para los reportes");
            }
            else if (selectedQuadrants.Any())
            {
                summary.Add($"🧩 {selectedQuadrants.Count} cuadrantes: {string.Join(", ", selectedQuadrants)}");
                summary.Add($"⚙️ Monitoreo: Cada {_numMonitoringIntervalMinutes.Value}min, {_numActivityThreshold.Value:F1}% umbral");
            }

            // Check frequency and days
            if (_rbDaily.Checked)
            {
                var selectedDays = _deliveryDaysCheckBoxes.Where(kvp => kvp.Value.Checked).ToList();
                if (!selectedDays.Any())
                {
                    errors.Add("Para reportes diarios, seleccione al menos un día");
                }
                else
                {
                    summary.Add($"📅 Reportes diarios: {string.Join(", ", selectedDays.Select(d => GetDayName(d.Key)))}");
                    summary.Add($"⏰ Envío a las {_dtpEmailTime.Value:HH:mm}");
                }
            }
            else
            {
                summary.Add($"📦 Reportes semanales cada {_cmbWeeklyDeliveryDay.Text}");
                summary.Add($"⏰ Envío a las {_dtpEmailTime.Value:HH:mm}");
            }

            var isValid = !errors.Any();
            var summaryText = string.Join("\n", summary);
            var errorText = errors.Any() ? string.Join("\n", errors) : "";

            return (isValid, summaryText, errorText);
        }
        catch (Exception ex)
        {
            return (false, "", $"Error en validación: {ex.Message}");
        }
    }

    private void LoadQuadrantConfigurations()
    {
        try
        {
            _cmbQuadrantConfig.Items.Clear();
            
            if (_config.QuadrantSystem?.Configurations?.Any() == true)
            {
                foreach (var config in _config.QuadrantSystem.Configurations)
                {
                    _cmbQuadrantConfig.Items.Add(new QuadrantConfigItem(config.Name, config));
                }
                
                // Select first by default or current configuration
                if (!string.IsNullOrEmpty(_reportsConfig.SelectedQuadrantConfig))
                {
                    var existingItem = _cmbQuadrantConfig.Items.Cast<QuadrantConfigItem>()
                        .FirstOrDefault(item => item.Name == _reportsConfig.SelectedQuadrantConfig);
                    if (existingItem != null)
                    {
                        _cmbQuadrantConfig.SelectedItem = existingItem;
                    }
                }
                
                if (_cmbQuadrantConfig.SelectedIndex == -1 && _cmbQuadrantConfig.Items.Count > 0)
                {
                    _cmbQuadrantConfig.SelectedIndex = 0;
                }
            }
            else
            {
                _cmbQuadrantConfig.Items.Add("❌ Sin configuraciones de cuadrantes");
                _cmbQuadrantConfig.SelectedIndex = 0;
                _cmbQuadrantConfig.Enabled = false;
                _clbQuadrants.Enabled = false;
            }
            
            // Load quadrants for selected configuration
            OnQuadrantConfigChanged(null, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsConfig] Error cargando configuraciones de cuadrantes: {ex.Message}");
        }
    }

    private void OnQuadrantConfigChanged(object? sender, EventArgs e)
    {
        try
        {
            _clbQuadrants.Items.Clear();
            
            if (_cmbQuadrantConfig.SelectedItem is QuadrantConfigItem selectedItem)
            {
                var config = selectedItem.Configuration;
                foreach (var region in config.Quadrants.Where(r => r.IsEnabled))
                {
                    var isSelected = _reportsConfig.SelectedQuadrants.Contains(region.Name);
                    _clbQuadrants.Items.Add(region.Name, isSelected);
                }
                
                Console.WriteLine($"[SimplifiedReportsConfig] Cargados {_clbQuadrants.Items.Count} cuadrantes para {config.Name}");
            }
            else
            {
                _clbQuadrants.Items.Add("❌ Seleccione una configuración de cuadrantes");
            }
            
            UpdatePreview();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsConfig] Error cargando cuadrantes: {ex.Message}");
            _clbQuadrants.Items.Clear();
            _clbQuadrants.Items.Add($"❌ Error: {ex.Message}");
        }
    }

    private async void OnPreviewReportClick(object? sender, EventArgs e)
    {
        try
        {
            _btnPreviewReport.Enabled = false;
            _btnPreviewReport.Text = "⏳ Generando...";

            var selectedQuadrants = _clbQuadrants.CheckedItems.Cast<string>()
                .Where(q => !q.StartsWith("❌"))
                .ToList();

            if (!selectedQuadrants.Any())
            {
                MessageBox.Show("Seleccione al menos un cuadrante para generar la vista previa.",
                               "Cuadrantes requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var previewText = $@"
📊 VISTA PREVIA DEL REPORTE

🧩 Cuadrantes incluidos: {selectedQuadrants.Count}
   {string.Join(", ", selectedQuadrants)}

⚙️ Configuración de monitoreo:
   Intervalo: Cada {_numMonitoringIntervalMinutes.Value} minutos
   Umbral actividad: {_numActivityThreshold.Value:F1}% cambio
   Tolerancia pixel: {_numPixelTolerance.Value} píxeles

📈 Contenido del reporte:
   • Timeline de actividad minuto por minuto
   • Gráficos interactivos por cuadrante
   • Estadísticas detalladas de comparaciones
   • Detección de patrones de actividad
   • Análisis de productividad por región

📅 Período: {(_rbDaily.Checked ? "Día actual (8:00 AM - 8:00 PM)" : "Semana completa (7 días)")
            }

🎯 Métricas incluidas:
   • Total de comparaciones por cuadrante
   • Actividades detectadas vs umbral configurado
   • Porcentaje de cambio promedio
   • Duración de actividad por región
   • Eficiencia de monitoreo general";

            _lblReportPreview.Text = previewText;
            _lblReportPreview.BackColor = Color.FromArgb(232, 245, 233); // Light green

        }
        catch (Exception ex)
        {
            _lblReportPreview.Text = $"❌ Error generando preview: {ex.Message}";
            _lblReportPreview.BackColor = Color.FromArgb(248, 215, 218); // Light red
        }
        finally
        {
            _btnPreviewReport.Enabled = true;
            _btnPreviewReport.Text = "🔍 Generar Vista Previa del Reporte";
        }
    }
}

/// <summary>
/// Clase helper para items de configuración de cuadrantes
/// </summary>
public class QuadrantConfigItem
{
    public string Name { get; set; }
    public QuadrantConfiguration Configuration { get; set; }

    public QuadrantConfigItem(string name, QuadrantConfiguration configuration)
    {
        Name = name;
        Configuration = configuration;
    }

    public override string ToString() => Name;
}

/// <summary>
/// Configuración COMPLETA para reportes automáticos  
/// INCLUYE: Entrega + Cuadrantes + Configuración de monitoreo
/// </summary>
public class SimplifiedReportsConfig
{
    // Basic settings
    public bool IsDaily { get; set; } = true;
    public TimeSpan EmailTime { get; set; } = TimeSpan.FromHours(18); // 6 PM default (end of workday)
    
    // Days when reports should be DELIVERED
    public List<DayOfWeek> DeliveryDays { get; set; } = new()
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
        DayOfWeek.Thursday, DayOfWeek.Friday
    };
    
    // Weekly delivery day (for weekly ZIP reports)
    public DayOfWeek WeeklyDeliveryDay { get; set; } = DayOfWeek.Monday;
    
    // Selected recipients for reports
    public List<string> SelectedRecipients { get; set; } = new();
    
    // NUEVO: Configuración de cuadrantes y monitoreo
    public string SelectedQuadrantConfig { get; set; } = "";
    public List<string> SelectedQuadrants { get; set; } = new();
    public int MonitoringIntervalMinutes { get; set; } = 5;
    public double ActivityThresholdPercentage { get; set; } = 2.0;
    public int PixelToleranceThreshold { get; set; } = 10;
    
    // State
    public bool IsEnabled { get; set; } = false;
    public DateTime LastEmailSent { get; set; } = DateTime.MinValue;
    
    /// <summary>
    /// Should send email today based on configuration?
    /// </summary>
    public bool ShouldSendToday(DateTime date)
    {
        if (!IsEnabled) return false;
        
        return IsDaily 
            ? DeliveryDays.Contains(date.DayOfWeek)
            : date.DayOfWeek == WeeklyDeliveryDay;
    }
    
    /// <summary>
    /// Get user-friendly description of complete configuration
    /// </summary>
    public string GetDescription()
    {
        var description = new List<string>();
        
        // Frequency and delivery info
        if (IsDaily)
        {
            var dayNames = DeliveryDays.Select(d => d switch
            {
                DayOfWeek.Monday => "Lun",
                DayOfWeek.Tuesday => "Mar", 
                DayOfWeek.Wednesday => "Mié",
                DayOfWeek.Thursday => "Jue",
                DayOfWeek.Friday => "Vie",
                DayOfWeek.Saturday => "Sáb",
                DayOfWeek.Sunday => "Dom",
                _ => d.ToString()
            });
            description.Add($"📅 Reportes HTML diarios: {string.Join(", ", dayNames)} a las {EmailTime:HH:mm}");
        }
        else
        {
            var weeklyDayName = WeeklyDeliveryDay switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles", 
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => WeeklyDeliveryDay.ToString()
            };
            description.Add($"📦 Reportes ZIP semanales cada {weeklyDayName} a las {EmailTime:HH:mm}");
        }
        
        // Monitoring configuration
        description.Add($"⚙️ Monitoreo: Cada {MonitoringIntervalMinutes}min, umbral {ActivityThresholdPercentage:F1}%");
        
        // Quadrants info
        if (SelectedQuadrants.Any())
        {
            description.Add($"🧩 Cuadrantes: {string.Join(", ", SelectedQuadrants)} ({SelectedQuadrantConfig})");
        }
        else
        {
            description.Add("⚠️ Sin cuadrantes seleccionados");
        }
        
        // Recipients info
        description.Add($"📧 {SelectedRecipients.Count} destinatarios configurados");
        
        return string.Join(" | ", description);
    }
}