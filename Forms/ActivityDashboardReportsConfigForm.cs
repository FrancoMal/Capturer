using System.Drawing;
using System.Windows.Forms;
using Capturer.Services;
using Capturer.Models;

namespace Capturer.Forms;

/// <summary>
/// Formulario de configuración para reportes programados del ActivityDashboard
/// </summary>
public partial class ActivityDashboardReportsConfigForm : Form
{
    private readonly CapturerConfiguration _config;
    private ActivityDashboardScheduleConfig _scheduleConfig;
    
    // UI Controls
    private CheckBox _chkEnableReports = new();
    private ComboBox _cmbFrequency = new();
    private NumericUpDown _numCustomDays = new();
    private DateTimePicker _dtpReportTime = new();
    private DateTimePicker _dtpStartTime = new();
    private DateTimePicker _dtpEndTime = new();
    private CheckBox _chkUseWeekdayFilter = new();
    private CheckBox _chkGeneratePerQuadrant = new();
    private Panel _weekDaysPanel = new();
    private Dictionary<DayOfWeek, CheckBox> _weekDayCheckBoxes = new();
    private ComboBox _cmbQuadrantConfig = new();
    
    // Email controls
    private CheckBox _chkEnableEmail = new();
    private CheckedListBox _clbRecipients = new();
    private TextBox _txtCustomEmail = new();
    private Button _btnAddEmail = new();
    private CheckBox _chkUseZipFormat = new();
    private CheckBox _chkSendSeparateEmails = new();
    private CheckedListBox _clbReportTypes = new();
    private Button _btnTestEmail = new();
    
    private Label _lblPreview = new();
    private Button _btnTestGeneration = new();
    private Button _btnOpenReportsFolder = new();
    
    public ActivityDashboardScheduleConfig ScheduleConfig => _scheduleConfig;

    public ActivityDashboardReportsConfigForm(CapturerConfiguration config, ActivityDashboardScheduleConfig? currentConfig = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _scheduleConfig = currentConfig ?? new ActivityDashboardScheduleConfig();
        
        InitializeComponent();
        LoadQuadrantConfigurations();
        LoadCurrentSettings();
        UpdatePreview();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // Form configuration - INCREASED HEIGHT for better visibility
        Text = "Configuración de Reportes Diarios - ActivityDashboard";
        Size = new Size(680, 800); // Increased both width and height
        FormBorderStyle = FormBorderStyle.Sizable; // Allow resizing
        MaximizeBox = true; // Allow maximizing
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        MinimumSize = new Size(650, 600); // Set minimum size

        // Create main panel with scroll - FIXED SCROLLING
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10),
            BackColor = Color.White
        };

        // Main layout - FIXED SIZE CALCULATION
        var mainPanel = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            Size = new Size(640, 950), // Adjusted width to fit in new form size
            ColumnCount = 1,
            RowCount = 10, // Keep all sections
            Padding = new Padding(15),
            AutoSize = false,
            BackColor = Color.White
        };

        for (int i = 0; i < 9; i++)
        {
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Buttons row
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Section 1: Enable Reports
        var enableGroupBox = CreateEnableSection();
        mainPanel.Controls.Add(enableGroupBox, 0, 0);

        // Section 2: Frequency Configuration
        var frequencyGroupBox = CreateFrequencySection();
        mainPanel.Controls.Add(frequencyGroupBox, 0, 1);

        // Section 3: Timing Configuration
        var timingGroupBox = CreateTimingSection();
        mainPanel.Controls.Add(timingGroupBox, 0, 2);

        // Section 4: Period Configuration
        var periodGroupBox = CreatePeriodSection();
        mainPanel.Controls.Add(periodGroupBox, 0, 3);

        // Section 5: Week Days Filter
        var weekDaysGroupBox = CreateWeekDaysSection();
        mainPanel.Controls.Add(weekDaysGroupBox, 0, 4);

        // Section 6: Quadrant Configuration
        var quadrantGroupBox = CreateQuadrantSection();
        mainPanel.Controls.Add(quadrantGroupBox, 0, 5);

        // Section 7: Email Configuration
        var emailGroupBox = CreateEmailSection();
        mainPanel.Controls.Add(emailGroupBox, 0, 6);

        // Section 8: Output Configuration
        var outputGroupBox = CreateOutputSection();
        mainPanel.Controls.Add(outputGroupBox, 0, 7);

        // Section 9: Preview and Testing
        var previewGroupBox = CreatePreviewSection();
        mainPanel.Controls.Add(previewGroupBox, 0, 8);

        // Buttons
        var buttonPanel = CreateButtonPanel();
        mainPanel.Controls.Add(buttonPanel, 0, 9);

        // CRITICAL FIX: Ensure the scroll panel knows the main panel size
        scrollPanel.Controls.Add(mainPanel);
        
        // Add visual separator before buttons for better UX
        var separatorPanel = new Panel
        {
            Height = 2,
            BackColor = Color.LightGray,
            Margin = new Padding(20, 10, 20, 10)
        };
        
        // Create a fixed button panel at the bottom of the form
        var fixedButtonPanel = new Panel
        {
            Height = 60,
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(15, 10, 15, 10)
        };
        
        var fixedBtnOK = new Button
        {
            Text = "💾 Guardar Configuración",
            Size = new Size(160, 35),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        fixedBtnOK.Location = new Point(fixedButtonPanel.Width - 280, 12);
        fixedBtnOK.FlatAppearance.BorderSize = 0;
        fixedBtnOK.Click += OnOKClick;
        
        var fixedBtnCancel = new Button
        {
            Text = "❌ Cancelar",
            Size = new Size(100, 35),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            DialogResult = DialogResult.Cancel
        };
        fixedBtnCancel.Location = new Point(fixedButtonPanel.Width - 115, 12);
        fixedBtnCancel.FlatAppearance.BorderSize = 0;
        
        fixedButtonPanel.Controls.AddRange(new Control[] { fixedBtnOK, fixedBtnCancel });
        
        // Adjust scroll panel to leave room for fixed buttons
        var adjustedScrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10, 10, 10, 10),
            BackColor = Color.White
        };
        
        adjustedScrollPanel.Controls.Add(mainPanel);
        
        Controls.Add(adjustedScrollPanel);
        Controls.Add(fixedButtonPanel);
        ResumeLayout(false);
    }

    private GroupBox CreateEnableSection()
    {
        var groupBox = new GroupBox
        {
            Text = "🤖 Configuración General",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 80,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _chkEnableReports = new CheckBox
        {
            Text = "Habilitar generación automática de reportes diarios",
            Location = new Point(15, 25),
            Size = new Size(350, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.DarkGreen
        };
        _chkEnableReports.CheckedChanged += OnEnableChanged;

        var infoLabel = new Label
        {
            Text = "Los reportes se generarán automáticamente según la programación configurada",
            Location = new Point(15, 50),
            Size = new Size(500, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { _chkEnableReports, infoLabel });
        return groupBox;
    }

    private GroupBox CreateFrequencySection()
    {
        var groupBox = new GroupBox
        {
            Text = "📅 Frecuencia de Reportes",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 100,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var frequencyLabel = new Label
        {
            Text = "Frecuencia:",
            Location = new Point(15, 30),
            Size = new Size(80, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _cmbFrequency = new ComboBox
        {
            Location = new Point(100, 28),
            Size = new Size(120, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F)
        };
        _cmbFrequency.Items.AddRange(new[] { "Diario", "Semanal", "Personalizado" });
        _cmbFrequency.SelectedIndexChanged += OnFrequencyChanged;

        var customLabel = new Label
        {
            Text = "Cada",
            Location = new Point(240, 30),
            Size = new Size(35, 20),
            Font = new Font("Segoe UI", 9F),
            Visible = false
        };

        _numCustomDays = new NumericUpDown
        {
            Location = new Point(280, 28),
            Size = new Size(60, 23),
            Minimum = 1,
            Maximum = 365,
            Value = 1,
            Font = new Font("Segoe UI", 9F),
            Visible = false
        };

        var daysLabel = new Label
        {
            Text = "días",
            Location = new Point(350, 30),
            Size = new Size(30, 20),
            Font = new Font("Segoe UI", 9F),
            Visible = false
        };

        var infoLabel = new Label
        {
            Text = "Predeterminado: 11:00 PM diario (configurable en siguiente sección)",
            Location = new Point(15, 60),
            Size = new Size(400, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { frequencyLabel, _cmbFrequency, customLabel, _numCustomDays, daysLabel, infoLabel });
        return groupBox;
    }

    private GroupBox CreateTimingSection()
    {
        var groupBox = new GroupBox
        {
            Text = "⏰ Programación de Generación",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 80,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var timeLabel = new Label
        {
            Text = "Hora de generación:",
            Location = new Point(15, 30),
            Size = new Size(120, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _dtpReportTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(140, 28),
            Size = new Size(100, 23),
            Font = new Font("Segoe UI", 9F),
            Value = DateTime.Today.AddHours(23) // Default to 11 PM
        };
        _dtpReportTime.ValueChanged += OnConfigChanged;

        var infoLabel = new Label
        {
            Text = "Predeterminado: 11:00 PM (optimizado para reportes diarios)",
            Location = new Point(250, 30),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { timeLabel, _dtpReportTime, infoLabel });
        return groupBox;
    }

    private GroupBox CreatePeriodSection()
    {
        var groupBox = new GroupBox
        {
            Text = "📊 Período de Análisis",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var startLabel = new Label
        {
            Text = "Hora inicio:",
            Location = new Point(15, 30),
            Size = new Size(80, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _dtpStartTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(100, 28),
            Size = new Size(100, 23),
            Font = new Font("Segoe UI", 9F)
        };
        _dtpStartTime.ValueChanged += OnConfigChanged;

        var endLabel = new Label
        {
            Text = "Hora fin:",
            Location = new Point(220, 30),
            Size = new Size(60, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _dtpEndTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(285, 28),
            Size = new Size(100, 23),
            Font = new Font("Segoe UI", 9F)
        };
        _dtpEndTime.ValueChanged += OnConfigChanged;

        var infoLabel = new Label
        {
            Text = "Define el período del día a incluir en los reportes.\n" +
                   "Ejemplo: 08:00 - 18:00 analizará solo la actividad en horario laboral.",
            Location = new Point(15, 60),
            Size = new Size(550, 40),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { startLabel, _dtpStartTime, endLabel, _dtpEndTime, infoLabel });
        return groupBox;
    }

    private GroupBox CreateWeekDaysSection()
    {
        var groupBox = new GroupBox
        {
            Text = "📅 Días de la Semana",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _chkUseWeekdayFilter = new CheckBox
        {
            Text = "Filtrar por días específicos de la semana",
            Location = new Point(15, 25),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };
        _chkUseWeekdayFilter.CheckedChanged += OnWeekdayFilterChanged;

        // Panel for weekday checkboxes
        _weekDaysPanel = new Panel
        {
            Location = new Point(15, 50),
            Size = new Size(550, 50),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.AliceBlue
        };

        // Create checkboxes for each day
        var days = Enum.GetValues<DayOfWeek>().OrderBy(d => (int)d == 0 ? 7 : (int)d).ToList(); // Monday first
        var dayNames = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
        
        for (int i = 0; i < days.Count; i++)
        {
            var day = days[i];
            var checkbox = new CheckBox
            {
                Text = dayNames[i],
                Location = new Point(10 + (i * 75), 15),
                Size = new Size(70, 20),
                Font = new Font("Segoe UI", 8F)
            };
            checkbox.CheckedChanged += OnConfigChanged;
            
            _weekDayCheckBoxes[day] = checkbox;
            _weekDaysPanel.Controls.Add(checkbox);
        }

        groupBox.Controls.AddRange(new Control[] { _chkUseWeekdayFilter, _weekDaysPanel });
        return groupBox;
    }

    private GroupBox CreateQuadrantSection()
    {
        var groupBox = new GroupBox
        {
            Text = "🎯 Configuración de Cuadrantes",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 80,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var configLabel = new Label
        {
            Text = "Configuración:",
            Location = new Point(15, 30),
            Size = new Size(90, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _cmbQuadrantConfig = new ComboBox
        {
            Location = new Point(110, 28),
            Size = new Size(200, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F)
        };
        _cmbQuadrantConfig.SelectedIndexChanged += OnConfigChanged;

        var refreshButton = new Button
        {
            Text = "🔄",
            Location = new Point(320, 27),
            Size = new Size(30, 25),
            Font = new Font("Segoe UI", 9F)
        };
        refreshButton.Click += (s, e) => LoadQuadrantConfigurations();

        groupBox.Controls.AddRange(new Control[] { configLabel, _cmbQuadrantConfig, refreshButton });
        return groupBox;
    }

    private GroupBox CreateEmailSection()
    {
        var groupBox = new GroupBox
        {
            Text = "📧 Configuración de Email",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 200,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        // Enable email checkbox
        _chkEnableEmail = new CheckBox
        {
            Text = "Habilitar envío automático por email",
            Location = new Point(15, 25),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.DarkGreen
        };
        _chkEnableEmail.CheckedChanged += OnEmailEnabledChanged;

        // Recipients list
        var recipientsLabel = new Label
        {
            Text = "Destinatarios:",
            Location = new Point(15, 55),
            Size = new Size(80, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _clbRecipients = new CheckedListBox
        {
            Location = new Point(100, 50),
            Size = new Size(250, 60),
            Font = new Font("Segoe UI", 8F),
            CheckOnClick = true
        };

        // Add custom email
        _txtCustomEmail = new TextBox
        {
            Location = new Point(100, 120),
            Size = new Size(180, 20),
            PlaceholderText = "nuevo@email.com",
            Font = new Font("Segoe UI", 9F)
        };

        _btnAddEmail = new Button
        {
            Text = "Agregar",
            Location = new Point(290, 119),
            Size = new Size(60, 22),
            Font = new Font("Segoe UI", 8F)
        };
        _btnAddEmail.Click += OnAddEmailClick;

        // Email options
        _chkUseZipFormat = new CheckBox
        {
            Text = "Usar formato ZIP",
            Location = new Point(370, 55),
            Size = new Size(120, 20),
            Font = new Font("Segoe UI", 9F),
            Checked = true
        };

        _chkSendSeparateEmails = new CheckBox
        {
            Text = "Email separado por reporte",
            Location = new Point(370, 80),
            Size = new Size(150, 20),
            Font = new Font("Segoe UI", 9F)
        };

        // Report types
        var reportTypesLabel = new Label
        {
            Text = "Incluir:",
            Location = new Point(370, 105),
            Size = new Size(50, 20),
            Font = new Font("Segoe UI", 9F)
        };

        _clbReportTypes = new CheckedListBox
        {
            Location = new Point(370, 125),
            Size = new Size(120, 40),
            Font = new Font("Segoe UI", 8F),
            CheckOnClick = true
        };
        _clbReportTypes.Items.Add("Consolidado", true);
        _clbReportTypes.Items.Add("Individual", false);

        // Test email button
        _btnTestEmail = new Button
        {
            Text = "🧪 Prueba Email",
            Location = new Point(100, 150),
            Size = new Size(100, 25),
            BackColor = Color.FromArgb(255, 193, 7),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
            Enabled = false
        };
        _btnTestEmail.FlatAppearance.BorderSize = 0;
        _btnTestEmail.Click += OnTestEmailClick;

        groupBox.Controls.AddRange(new Control[] {
            _chkEnableEmail, recipientsLabel, _clbRecipients, _txtCustomEmail, _btnAddEmail,
            _chkUseZipFormat, _chkSendSeparateEmails, reportTypesLabel, _clbReportTypes, _btnTestEmail
        });

        return groupBox;
    }

    private GroupBox CreateOutputSection()
    {
        var groupBox = new GroupBox
        {
            Text = "📁 Configuración de Salida",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 100,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _chkGeneratePerQuadrant = new CheckBox
        {
            Text = "Generar reportes individuales por cuadrante",
            Location = new Point(15, 25),
            Size = new Size(350, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };
        _chkGeneratePerQuadrant.CheckedChanged += OnConfigChanged;

        _btnOpenReportsFolder = new Button
        {
            Text = "📂 Abrir Carpeta de Reportes",
            Location = new Point(15, 55),
            Size = new Size(180, 30),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F)
        };
        _btnOpenReportsFolder.FlatAppearance.BorderSize = 0;
        _btnOpenReportsFolder.Click += OnOpenReportsFolderClick;

        var infoLabel = new Label
        {
            Text = "Los reportes se almacenarán organizados por fecha y cuadrante",
            Location = new Point(210, 65),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange(new Control[] { _chkGeneratePerQuadrant, _btnOpenReportsFolder, infoLabel });
        return groupBox;
    }

    private GroupBox CreatePreviewSection()
    {
        var groupBox = new GroupBox
        {
            Text = "🔍 Vista Previa y Pruebas",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Height = 120,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _lblPreview = new Label
        {
            Location = new Point(15, 25),
            Size = new Size(550, 60),
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.DarkBlue,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.AliceBlue,
            Padding = new Padding(5)
        };

        _btnTestGeneration = new Button
        {
            Text = "🧪 Generar Reporte de Prueba",
            Location = new Point(15, 90),
            Size = new Size(180, 25),
            BackColor = Color.FromArgb(255, 152, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F)
        };
        _btnTestGeneration.FlatAppearance.BorderSize = 0;
        _btnTestGeneration.Click += OnTestGenerationClick;

        groupBox.Controls.AddRange(new Control[] { _lblPreview, _btnTestGeneration });
        return groupBox;
    }

    private Panel CreateButtonPanel()
    {
        var panel = new Panel
        {
            Height = 50,
            Dock = DockStyle.Fill
        };

        var btnOK = new Button
        {
            Text = "Guardar Configuración",
            Size = new Size(140, 30),
            Location = new Point(200, 10),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnOK.FlatAppearance.BorderSize = 0;
        btnOK.Click += OnOKClick;

        var btnCancel = new Button
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
        btnCancel.FlatAppearance.BorderSize = 0;

        panel.Controls.AddRange(new Control[] { btnOK, btnCancel });
        return panel;
    }

    private void LoadQuadrantConfigurations()
    {
        try
        {
            _cmbQuadrantConfig.Items.Clear();
            
            foreach (var config in _config.QuadrantSystem.Configurations)
            {
                _cmbQuadrantConfig.Items.Add(new QuadrantConfigItem(config));
            }

            // Select current configuration
            if (!string.IsNullOrEmpty(_scheduleConfig.QuadrantConfigName))
            {
                for (int i = 0; i < _cmbQuadrantConfig.Items.Count; i++)
                {
                    var item = _cmbQuadrantConfig.Items[i] as QuadrantConfigItem;
                    if (item?.Configuration.Name == _scheduleConfig.QuadrantConfigName)
                    {
                        _cmbQuadrantConfig.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando configuraciones: {ex.Message}", 
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void LoadCurrentSettings()
    {
        _chkEnableReports.Checked = _scheduleConfig.IsEnabled;
        
        // Load frequency
        _cmbFrequency.SelectedIndex = (int)_scheduleConfig.Frequency;
        _numCustomDays.Value = _scheduleConfig.CustomDays;
        
        _dtpReportTime.Value = DateTime.Today.Add(_scheduleConfig.ReportTime);
        _dtpStartTime.Value = DateTime.Today.Add(_scheduleConfig.StartTime);
        _dtpEndTime.Value = DateTime.Today.Add(_scheduleConfig.EndTime);
        _chkUseWeekdayFilter.Checked = _scheduleConfig.UseWeekdayFilter;
        _chkGeneratePerQuadrant.Checked = _scheduleConfig.GeneratePerQuadrantReports;

        // Load active weekdays
        foreach (var (day, checkbox) in _weekDayCheckBoxes)
        {
            checkbox.Checked = _scheduleConfig.ActiveWeekDays.Contains(day);
        }
        
        // Load email settings
        _chkEnableEmail.Checked = _scheduleConfig.EmailConfig.IsEnabled;
        _chkUseZipFormat.Checked = _scheduleConfig.EmailConfig.UseZipFormat;
        _chkSendSeparateEmails.Checked = _scheduleConfig.EmailConfig.SendSeparateEmails;
        
        // Load recipients from main config
        _clbRecipients.Items.Clear();
        foreach (var recipient in _config.Email.Recipients)
        {
            var isSelected = _scheduleConfig.EmailConfig.Recipients.Contains(recipient);
            _clbRecipients.Items.Add(recipient, isSelected);
        }
        
        // Load report types
        for (int i = 0; i < _clbReportTypes.Items.Count; i++)
        {
            var itemText = _clbReportTypes.Items[i].ToString() ?? "";
            _clbReportTypes.SetItemChecked(i, _scheduleConfig.EmailConfig.SelectedReportTypes.Contains(itemText));
        }

        OnEnableChanged(null, EventArgs.Empty);
        OnWeekdayFilterChanged(null, EventArgs.Empty);
        OnEmailEnabledChanged(null, EventArgs.Empty);
        OnFrequencyChanged(null, EventArgs.Empty);
    }

    private void OnEnableChanged(object? sender, EventArgs e)
    {
        var enabled = _chkEnableReports.Checked;
        _cmbFrequency.Enabled = enabled;
        _numCustomDays.Enabled = enabled;
        _dtpReportTime.Enabled = enabled;
        _dtpStartTime.Enabled = enabled;
        _dtpEndTime.Enabled = enabled;
        _chkUseWeekdayFilter.Enabled = enabled;
        _chkGeneratePerQuadrant.Enabled = enabled;
        _cmbQuadrantConfig.Enabled = enabled;
        _chkEnableEmail.Enabled = enabled;
        _btnTestGeneration.Enabled = enabled;
        
        OnWeekdayFilterChanged(sender, e);
        OnEmailEnabledChanged(sender, e);
        UpdatePreview();
    }
    
    private void OnFrequencyChanged(object? sender, EventArgs e)
    {
        var isCustom = _cmbFrequency.SelectedIndex == 2; // "Personalizado"
        var customControls = _numCustomDays.Parent?.Controls.OfType<Control>()
            .Where(c => c.Name?.Contains("custom") == true || c.Text?.Contains("Cada") == true || c.Text?.Contains("días") == true)
            .ToList() ?? new List<Control>();
            
        foreach (var control in customControls)
        {
            control.Visible = isCustom;
        }
        
        UpdatePreview();
    }
    
    private void OnEmailEnabledChanged(object? sender, EventArgs e)
    {
        var enabled = _chkEnableReports.Checked && _chkEnableEmail.Checked;
        
        _clbRecipients.Enabled = enabled;
        _txtCustomEmail.Enabled = enabled;
        _btnAddEmail.Enabled = enabled;
        _chkUseZipFormat.Enabled = enabled;
        _chkSendSeparateEmails.Enabled = enabled;
        _clbReportTypes.Enabled = enabled;
        _btnTestEmail.Enabled = enabled && _clbRecipients.CheckedItems.Count > 0;
        
        UpdatePreview();
    }
    
    private void OnAddEmailClick(object? sender, EventArgs e)
    {
        try
        {
            var email = _txtCustomEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
                return;
                
            // Basic email validation
            if (!email.Contains("@") || !email.Contains("."))
            {
                MessageBox.Show("Por favor ingrese un email válido", "Email inválido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Check if already exists
            for (int i = 0; i < _clbRecipients.Items.Count; i++)
            {
                if (_clbRecipients.Items[i].ToString() == email)
                {
                    MessageBox.Show("Este email ya está en la lista", "Email duplicado", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            
            // Add to list
            _clbRecipients.Items.Add(email, true);
            _txtCustomEmail.Clear();
            
            OnEmailEnabledChanged(sender, e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error agregando email: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private async void OnTestEmailClick(object? sender, EventArgs e)
    {
        try
        {
            _btnTestEmail.Enabled = false;
            _btnTestEmail.Text = "⏳ Enviando...";
            
            MessageBox.Show("La funcionalidad de envío de email de prueba se implementará en el dashboard principal.", 
                "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error enviando email de prueba: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTestEmail.Enabled = _chkEnableEmail.Checked && _clbRecipients.CheckedItems.Count > 0;
            _btnTestEmail.Text = "🧪 Prueba Email";
        }
    }

    private void OnWeekdayFilterChanged(object? sender, EventArgs e)
    {
        var enabled = _chkEnableReports.Checked && _chkUseWeekdayFilter.Checked;
        _weekDaysPanel.Enabled = enabled;
        
        foreach (var checkbox in _weekDayCheckBoxes.Values)
        {
            checkbox.Enabled = enabled;
        }
        
        UpdatePreview();
    }

    private void OnConfigChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        try
        {
            if (!_chkEnableReports.Checked)
            {
                _lblPreview.Text = "❌ Reportes deshabilitados";
                return;
            }

            var preview = "✅ Configuración activa:\n";
            
            // Frequency info
            var freqText = _cmbFrequency.SelectedIndex switch
            {
                0 => "Diario",
                1 => "Semanal", 
                2 => $"Cada {_numCustomDays.Value} días",
                _ => "No seleccionado"
            };
            preview += $"• Frecuencia: {freqText}\n";
            preview += $"• Generación: {_dtpReportTime.Value:HH:mm}\n";
            preview += $"• Período análisis: {_dtpStartTime.Value:HH:mm} - {_dtpEndTime.Value:HH:mm}\n";
            
            if (_chkUseWeekdayFilter.Checked)
            {
                var activeDays = _weekDayCheckBoxes.Where(kvp => kvp.Value.Checked).Select(kvp => kvp.Value.Text);
                preview += $"• Días activos: {string.Join(", ", activeDays)}\n";
            }
            else
            {
                preview += "• Días activos: Todos los días\n";
            }
            
            var selectedConfig = _cmbQuadrantConfig.SelectedItem as QuadrantConfigItem;
            preview += $"• Configuración: {selectedConfig?.Configuration.Name ?? "No seleccionada"}\n";
            preview += $"• Reportes individuales: {(_chkGeneratePerQuadrant.Checked ? "Sí" : "No")}\n";
            
            // Email info
            if (_chkEnableEmail.Checked)
            {
                var recipientCount = _clbRecipients.CheckedItems.Count;
                preview += $"• Email: {recipientCount} destinatarios configurados";
            }
            else
            {
                preview += "• Email: Deshabilitado";
            }

            _lblPreview.Text = preview;
        }
        catch (Exception ex)
        {
            _lblPreview.Text = $"Error en vista previa: {ex.Message}";
        }
    }

    private async void OnTestGenerationClick(object? sender, EventArgs e)
    {
        try
        {
            _btnTestGeneration.Enabled = false;
            _btnTestGeneration.Text = "⏳ Generando...";
            
            // This would require integration with the scheduler service
            MessageBox.Show("Funcionalidad de prueba no implementada en este formulario.\n" +
                           "Use el dashboard principal para generar reportes de prueba.",
                           "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generando reporte de prueba: {ex.Message}",
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTestGeneration.Enabled = true;
            _btnTestGeneration.Text = "🧪 Generar Reporte de Prueba";
        }
    }

    private void OnOpenReportsFolderClick(object? sender, EventArgs e)
    {
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var reportsFolder = Path.Combine(documentsPath, "Capturer", "ActivityDashboard", "Reports");
            
            if (!Directory.Exists(reportsFolder))
            {
                Directory.CreateDirectory(reportsFolder);
            }
            
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = reportsFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error abriendo carpeta de reportes: {ex.Message}",
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnOKClick(object? sender, EventArgs e)
    {
        try
        {
            // Validate configuration
            if (_chkEnableReports.Checked)
            {
                if (_cmbQuadrantConfig.SelectedItem == null)
                {
                    MessageBox.Show("Por favor seleccione una configuración de cuadrantes.",
                                   "Configuración requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_chkUseWeekdayFilter.Checked && !_weekDayCheckBoxes.Values.Any(cb => cb.Checked))
                {
                    MessageBox.Show("Por favor seleccione al menos un día de la semana.",
                                   "Días requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Update configuration
            _scheduleConfig.IsEnabled = _chkEnableReports.Checked;
            _scheduleConfig.ReportTime = _dtpReportTime.Value.TimeOfDay;
            _scheduleConfig.StartTime = _dtpStartTime.Value.TimeOfDay;
            _scheduleConfig.EndTime = _dtpEndTime.Value.TimeOfDay;
            _scheduleConfig.UseWeekdayFilter = _chkUseWeekdayFilter.Checked;
            _scheduleConfig.GeneratePerQuadrantReports = _chkGeneratePerQuadrant.Checked;

            // Update active weekdays
            _scheduleConfig.ActiveWeekDays.Clear();
            foreach (var (day, checkbox) in _weekDayCheckBoxes)
            {
                if (checkbox.Checked)
                {
                    _scheduleConfig.ActiveWeekDays.Add(day);
                }
            }

            // Update quadrant config name
            if (_cmbQuadrantConfig.SelectedItem is QuadrantConfigItem selectedItem)
            {
                _scheduleConfig.QuadrantConfigName = selectedItem.Configuration.Name;
            }
            
            // Update frequency settings
            _scheduleConfig.Frequency = (DashboardReportFrequency)_cmbFrequency.SelectedIndex;
            _scheduleConfig.CustomDays = (int)_numCustomDays.Value;
            
            // Update email settings
            _scheduleConfig.EmailConfig.IsEnabled = _chkEnableEmail.Checked;
            _scheduleConfig.EmailConfig.UseZipFormat = _chkUseZipFormat.Checked;
            _scheduleConfig.EmailConfig.SendSeparateEmails = _chkSendSeparateEmails.Checked;
            
            // Update recipients
            _scheduleConfig.EmailConfig.Recipients.Clear();
            foreach (var checkedItem in _clbRecipients.CheckedItems)
            {
                _scheduleConfig.EmailConfig.Recipients.Add(checkedItem.ToString() ?? "");
            }
            
            // Update report types
            _scheduleConfig.EmailConfig.SelectedReportTypes.Clear();
            for (int i = 0; i < _clbReportTypes.Items.Count; i++)
            {
                if (_clbReportTypes.GetItemChecked(i))
                {
                    _scheduleConfig.EmailConfig.SelectedReportTypes.Add(_clbReportTypes.Items[i].ToString() ?? "");
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error guardando configuración: {ex.Message}",
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

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