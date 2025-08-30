using Capturer.Models;
using Capturer.Services;

namespace Capturer.Forms;

public partial class RoutineEmailForm : Form
{
    private readonly IEmailService _emailService;
    private readonly IFileService _fileService;
    private readonly IConfigurationManager _configManager;
    private readonly IQuadrantService? _quadrantService;
    private CapturerConfiguration _config;
    private ToolTip _helpTooltip;
    
    private Button CreateModernButton(string text, Point location, Size size, Color backgroundColor)
    {
        return new Button
        {
            Text = text,
            Location = location,
            Size = size,
            BackColor = backgroundColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(Math.Max(0, backgroundColor.R - 20), Math.Max(0, backgroundColor.G - 20), Math.Max(0, backgroundColor.B - 20)) },
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
    }
    
    // Main Configuration Controls
    private GroupBox groupMain;
    private CheckBox chkEnableRoutineReports;
    private ComboBox cmbReportFrequency;
    private NumericUpDown numCustomDays;
    private ComboBox cmbReportTime;
    private ComboBox cmbWeeklyDay;
    
    // New Time Filter Controls
    private GroupBox groupTimeFilter;
    private CheckBox chkUseTimeFilter;
    private ComboBox cmbStartTime;
    private ComboBox cmbEndTime;
    private Label lblStartTime;
    private Label lblEndTime;
    private Label lblTimeFilterNote;
    
    // Week Days Filter Controls
    private CheckBox chkIncludeWeekends;
    private CheckedListBox clbWeekDays;
    
    // Recipient Selection Controls
    private GroupBox groupRecipients;
    private CheckedListBox clbRecipients;
    private TextBox txtCustomEmail;
    private Button btnAddCustom;
    private Button btnRemoveSelected;
    private Label lblRecipientsCount;
    
    // Email Format Controls
    private GroupBox groupFormat;
    private RadioButton rbZipFormat;
    private RadioButton rbIndividualFiles;
    private Label lblFormatNote;
    
    // Quadrant Processing Controls
    private GroupBox groupQuadrants;
    private CheckBox chkUseQuadrants;
    private Button btnSelectQuadrants;
    private CheckedListBox clbQuadrants;
    private Label lblSelectedQuadrants;
    private List<string> _selectedQuadrantsList = new();
    private CheckBox chkProcessQuadrantsFirst;
    private ComboBox cmbQuadrantProfile;
    private Label lblQuadrantProfile;
    private CheckBox chkSeparateEmailPerQuadrant;
    private Label lblQuadrantProcessingNote;
    private Button btnTestQuadrantProcessing;
    
    // Control Buttons
    private Button btnSave;
    private Button btnCancel;
    private Button btnTestEmail;
    private Button btnPreviewSchedule;
    
    // Status and Preview
    private GroupBox groupPreview;
    private Label lblCurrentStatus;
    private Label lblNextScheduledEmail;
    private Label lblPreviewInfo;
    private ProgressBar progressBar;
    private Label lblProgress;

    public RoutineEmailForm(IEmailService emailService, IFileService fileService, IConfigurationManager configManager, IQuadrantService? quadrantService = null)
    {
        _emailService = emailService;
        _fileService = fileService;
        _configManager = configManager;
        _quadrantService = quadrantService;
        _config = new CapturerConfiguration();
        _helpTooltip = new ToolTip();
        
        InitializeComponent();
        LoadConfigurationAsync();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(750, 800);
        this.Text = "📊 Configuración de Reportes Automáticos - Capturer v2.4";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;
        this.MinimumSize = new Size(750, 800);
        this.MaximumSize = new Size(950, 1000);
        this.BackColor = Color.FromArgb(248, 249, 250);
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        
        // Set form icon
        try
        {
            if (File.Exists("Capturer_Logo.ico"))
            {
                this.Icon = new Icon("Capturer_Logo.ico");
            }
            else
            {
                var stream = GetType().Assembly.GetManifestResourceStream("Capturer.Capturer_Logo.ico");
                if (stream != null)
                {
                    this.Icon = new Icon(stream);
                }
            }
        }
        catch
        {
            // Keep default icon if loading fails
        }

        // Create main scrollable panel with modern styling
        var mainPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(this.ClientSize.Width, this.ClientSize.Height),
            AutoScroll = true,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(15),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        this.Controls.Add(mainPanel);

        var y = 20;

        // === MAIN CONFIGURATION SECTION ===
        groupMain = new GroupBox 
        { 
            Text = "Configuración de Reportes Automáticos", 
            Location = new Point(20, y), 
            Size = new Size(640, 140), 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
        };
        
        // Add help button for main configuration
        var helpMain = CreateHelpButton(new Point(605, -2), 
            "Configuración Principal: Configure cuándo y cómo se envían los reportes automáticos.\n\n" +
            "• Habilite reportes para activar el envío automático\n" +
            "• Seleccione frecuencia: diaria, semanal, mensual o personalizada\n" +
            "• Configure la hora exacta de envío\n" +
            "• Los reportes se envían automáticamente según esta configuración");
        groupMain.Controls.Add(helpMain);
        
        // Enable routine reports
        chkEnableRoutineReports = new CheckBox 
        { 
            Text = "Habilitar reportes automáticos por email", 
            Location = new Point(15, 25), 
            AutoSize = true,
            Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold)
        };
        chkEnableRoutineReports.CheckedChanged += ChkEnableRoutineReports_CheckedChanged;
        groupMain.Controls.Add(chkEnableRoutineReports);
        
        // Frequency
        groupMain.Controls.Add(new Label { Text = "Frecuencia:", Location = new Point(15, 55), AutoSize = true });
        cmbReportFrequency = new ComboBox 
        { 
            Location = new Point(85, 52), 
            Width = 120, 
            DropDownStyle = ComboBoxStyle.DropDownList 
        };
        cmbReportFrequency.Items.AddRange(new[] { "Diario", "Semanal", "Mensual", "Personalizado" });
        cmbReportFrequency.SelectedIndexChanged += CmbReportFrequency_SelectedIndexChanged;
        groupMain.Controls.Add(cmbReportFrequency);
        
        // Custom days (initially hidden)
        groupMain.Controls.Add(new Label { Text = "Cada", Location = new Point(220, 55), AutoSize = true });
        numCustomDays = new NumericUpDown 
        { 
            Location = new Point(250, 52), 
            Width = 60, 
            Minimum = 1, 
            Maximum = 365, 
            Value = 7,
            Visible = false
        };
        groupMain.Controls.Add(numCustomDays);
        groupMain.Controls.Add(new Label { Text = "días", Location = new Point(315, 55), AutoSize = true, Visible = false });
        
        // Weekly day selection (initially hidden)
        groupMain.Controls.Add(new Label { Text = "Día:", Location = new Point(220, 55), AutoSize = true, Visible = false });
        cmbWeeklyDay = new ComboBox 
        { 
            Location = new Point(250, 52), 
            Width = 100, 
            DropDownStyle = ComboBoxStyle.DropDownList,
            Visible = false
        };
        cmbWeeklyDay.Items.AddRange(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" });
        groupMain.Controls.Add(cmbWeeklyDay);
        
        // Report time
        groupMain.Controls.Add(new Label { Text = "Hora del envío:", Location = new Point(15, 85), AutoSize = true });
        cmbReportTime = new ComboBox 
        { 
            Location = new Point(105, 82), 
            Width = 120, 
            DropDownStyle = ComboBoxStyle.DropDownList 
        };
        for (int i = 0; i < 24; i++)
        {
            cmbReportTime.Items.Add($"{i:D2}:00");
        }
        groupMain.Controls.Add(cmbReportTime);
        
        // Current status
        lblCurrentStatus = new Label 
        { 
            Location = new Point(15, 110), 
            Size = new Size(600, 20), 
            Text = "Estado: Reportes deshabilitados",
            Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold),
            ForeColor = Color.DarkRed,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        groupMain.Controls.Add(lblCurrentStatus);
        
        mainPanel.Controls.Add(groupMain);
        y += 160;

        // === TIME FILTER SECTION ===
        groupTimeFilter = new GroupBox 
        { 
            Text = "Filtros de Horario", 
            Location = new Point(20, y), 
            Size = new Size(640, 120), 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
        };
        
        // Add help button for time filter
        var helpTimeFilter = CreateHelpButton(new Point(605, -2), 
            "Filtros de Horario: Configure qué capturas incluir según horario y días.\n\n" +
            "• Activar filtro de horario: Solo capturas en rango específico\n" +
            "• Hora inicio/fin: Defina ventana de tiempo (ej: 8:00 AM - 11:00 PM)\n" +
            "• Días de la semana: Seleccione qué días incluir\n" +
            "• Útil para monitoreo de horario laboral únicamente");
        groupTimeFilter.Controls.Add(helpTimeFilter);

        chkUseTimeFilter = new CheckBox 
        { 
            Text = "Usar filtro de horario", 
            Location = new Point(15, 25), 
            AutoSize = true 
        };
        chkUseTimeFilter.CheckedChanged += ChkUseTimeFilter_CheckedChanged;
        groupTimeFilter.Controls.Add(chkUseTimeFilter);

        lblStartTime = new Label { Text = "Desde:", Location = new Point(15, 55), AutoSize = true };
        groupTimeFilter.Controls.Add(lblStartTime);
        
        cmbStartTime = new ComboBox 
        { 
            Location = new Point(60, 52), 
            Width = 80, 
            DropDownStyle = ComboBoxStyle.DropDownList 
        };
        for (int i = 0; i < 24; i++)
        {
            cmbStartTime.Items.Add($"{i:D2}:00");
        }
        cmbStartTime.SelectedIndex = 8; // Default 8:00 AM
        groupTimeFilter.Controls.Add(cmbStartTime);

        lblEndTime = new Label { Text = "Hasta:", Location = new Point(160, 55), AutoSize = true };
        groupTimeFilter.Controls.Add(lblEndTime);
        
        cmbEndTime = new ComboBox 
        { 
            Location = new Point(200, 52), 
            Width = 80, 
            DropDownStyle = ComboBoxStyle.DropDownList 
        };
        for (int i = 0; i < 24; i++)
        {
            cmbEndTime.Items.Add($"{i:D2}:00");
        }
        cmbEndTime.SelectedIndex = 23; // Default 11:00 PM
        groupTimeFilter.Controls.Add(cmbEndTime);

        chkIncludeWeekends = new CheckBox 
        { 
            Text = "Incluir fines de semana", 
            Location = new Point(300, 28), 
            AutoSize = true,
            Checked = true
        };
        chkIncludeWeekends.CheckedChanged += ChkIncludeWeekends_CheckedChanged;
        groupTimeFilter.Controls.Add(chkIncludeWeekends);

        clbWeekDays = new CheckedListBox 
        { 
            Location = new Point(300, 52), 
            Size = new Size(320, 60), 
            CheckOnClick = true,
            MultiColumn = true,
            ColumnWidth = 90,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        clbWeekDays.Items.AddRange(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" });
        // Check weekdays by default
        for (int i = 0; i < 5; i++)
        {
            clbWeekDays.SetItemChecked(i, true);
        }
        groupTimeFilter.Controls.Add(clbWeekDays);

        lblTimeFilterNote = new Label 
        { 
            Location = new Point(15, 85), 
            Size = new Size(600, 20), 
            Text = "Nota: Los filtros se aplicarán solo a reportes automáticos. Reportes manuales no se ven afectados.",
            Font = new Font("Microsoft Sans Serif", 7.5F, FontStyle.Italic),
            ForeColor = Color.Gray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        groupTimeFilter.Controls.Add(lblTimeFilterNote);

        mainPanel.Controls.Add(groupTimeFilter);
        y += 140;

        // === RECIPIENT SELECTION SECTION ===
        groupRecipients = new GroupBox 
        { 
            Text = "Destinatarios", 
            Location = new Point(20, y), 
            Size = new Size(640, 160), 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
        };
        
        // Add help button for recipients
        var helpRecipients = CreateHelpButton(new Point(605, -2), 
            "Destinatarios: Configure quiénes recibirán los reportes automáticos.\n\n" +
            "• Marque los destinatarios ya configurados\n" +
            "• Agregue nuevos emails temporales si es necesario\n" +
            "• Use 'Remover' para eliminar emails de la lista\n" +
            "• Los reportes se enviarán a todos los destinatarios marcados");
        groupRecipients.Controls.Add(helpRecipients);
        
        clbRecipients = new CheckedListBox 
        { 
            Location = new Point(15, 25), 
            Size = new Size(480, 90), 
            CheckOnClick = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        clbRecipients.ItemCheck += ClbRecipients_ItemCheck;
        groupRecipients.Controls.Add(clbRecipients);
        
        btnRemoveSelected = new Button 
        { 
            Text = "Remover", 
            Location = new Point(505, 25), 
            Size = new Size(80, 25),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnRemoveSelected.Click += BtnRemoveSelected_Click;
        groupRecipients.Controls.Add(btnRemoveSelected);
        
        groupRecipients.Controls.Add(new Label { Text = "Agregar email:", Location = new Point(15, 125), AutoSize = true });
        txtCustomEmail = new TextBox 
        { 
            Location = new Point(100, 122), 
            Width = 280, 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        txtCustomEmail.KeyDown += TxtCustomEmail_KeyDown;
        groupRecipients.Controls.Add(txtCustomEmail);
        
        btnAddCustom = new Button 
        { 
            Text = "Agregar", 
            Location = new Point(390, 121), 
            Size = new Size(70, 25),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnAddCustom.Click += BtnAddCustom_Click;
        groupRecipients.Controls.Add(btnAddCustom);
        
        lblRecipientsCount = new Label 
        { 
            Location = new Point(470, 125), 
            Size = new Size(120, 20), 
            Text = "0 seleccionados",
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        groupRecipients.Controls.Add(lblRecipientsCount);
        
        mainPanel.Controls.Add(groupRecipients);
        y += 180;

        // === EMAIL FORMAT SECTION ===
        groupFormat = new GroupBox 
        { 
            Text = "Formato de Email", 
            Location = new Point(20, y), 
            Size = new Size(640, 100), 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
        };
        
        // Add help button for format
        var helpFormat = CreateHelpButton(new Point(605, -2), 
            "Formato de Email: Configure cómo se envían los archivos en los reportes.\n\n" +
            "• ZIP: Recomendado - todos los archivos en un solo adjunto comprimido\n" +
            "• Individual: Cada archivo como adjunto separado\n" +
            "• Límite de 25MB por email - ZIP es más eficiente para múltiples archivos");
        groupFormat.Controls.Add(helpFormat);
        
        rbZipFormat = new RadioButton 
        { 
            Text = "Archivo ZIP (Recomendado para múltiples archivos)", 
            Location = new Point(15, 25), 
            AutoSize = true, 
            Checked = true 
        };
        groupFormat.Controls.Add(rbZipFormat);
        
        rbIndividualFiles = new RadioButton 
        { 
            Text = "Archivos individuales (Recomendado para pocos archivos)", 
            Location = new Point(15, 45), 
            AutoSize = true 
        };
        groupFormat.Controls.Add(rbIndividualFiles);
        
        // Removed chkSeparateEmails - now handled per quadrant
        
        lblFormatNote = new Label 
        { 
            Text = "Nota: Los archivos individuales pueden tener limitaciones de tamaño por email (25MB máximo)",
            Location = new Point(15, 70), 
            Size = new Size(600, 20), 
            Font = new Font("Microsoft Sans Serif", 7.5F),
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        groupFormat.Controls.Add(lblFormatNote);
        
        mainPanel.Controls.Add(groupFormat);
        y += 120;

        // === QUADRANT PROCESSING SECTION ===
        groupQuadrants = new GroupBox 
        { 
            Text = "Sistema de Cuadrantes (Beta)", 
            Location = new Point(20, y), 
            Size = new Size(640, 200),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false // Initially hidden, shown if quadrants are available
        };
        
        // Add help button for quadrants
        var helpQuadrants = CreateHelpButton(new Point(605, -2), 
            "Sistema de Cuadrantes: Funcionalidad avanzada para reportes especializados.\n\n" +
            "• Usar cuadrantes: Incluye cuadrantes procesados en lugar de screenshots\n" +
            "• Procesar antes del envío: Regenera automáticamente con el perfil seleccionado\n" +
            "• Email por cuadrante: Envía un reporte individual para cada cuadrante\n" +
            "\nNota: Solo disponible si hay cuadrantes configurados en el sistema");
        groupQuadrants.Controls.Add(helpQuadrants);
        
        chkUseQuadrants = new CheckBox 
        { 
            Text = "Usar sistema de cuadrantes en reportes automáticos", 
            Location = new Point(15, 25), 
            AutoSize = true 
        };
        chkUseQuadrants.CheckedChanged += ChkUseQuadrants_CheckedChanged;
        groupQuadrants.Controls.Add(chkUseQuadrants);
        
        btnSelectQuadrants = CreateModernButton("🔲 Seleccionar Cuadrantes...", new Point(15, 50), new Size(200, 30), Color.FromArgb(0, 123, 255));
        btnSelectQuadrants.Enabled = false;
        btnSelectQuadrants.Click += BtnSelectQuadrants_Click;
        groupQuadrants.Controls.Add(btnSelectQuadrants);
        
        clbQuadrants = new CheckedListBox
        {
            Location = new Point(15, 85),
            Size = new Size(280, 80),
            CheckOnClick = true,
            Enabled = false,
            Visible = false  // Hide the checkbox list - now using dialog selection
        };
        groupQuadrants.Controls.Add(clbQuadrants);
        
        lblSelectedQuadrants = new Label
        {
            Text = "Ningún cuadrante seleccionado",
            Location = new Point(220, 55),
            Size = new Size(280, 20),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        groupQuadrants.Controls.Add(lblSelectedQuadrants);
        
        chkProcessQuadrantsFirst = new CheckBox 
        { 
            Text = "Procesar cuadrantes antes del envío (regenerar automáticamente)", 
            Location = new Point(15, 85), 
            AutoSize = true,
            Enabled = false,
            Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold),
            ForeColor = Color.DarkGreen
        };
        chkProcessQuadrantsFirst.CheckedChanged += ChkProcessQuadrantsFirst_CheckedChanged;
        groupQuadrants.Controls.Add(chkProcessQuadrantsFirst);
        
        // Quadrant profile selection (initially hidden)
        lblQuadrantProfile = new Label 
        { 
            Text = "Perfil de procesamiento:", 
            Location = new Point(35, 110), 
            AutoSize = true,
            Visible = false,
            Font = new Font("Microsoft Sans Serif", 8.25F)
        };
        groupQuadrants.Controls.Add(lblQuadrantProfile);
        
        cmbQuadrantProfile = new ComboBox 
        { 
            Location = new Point(180, 107), 
            Width = 200, 
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false,
            Visible = false
        };
        groupQuadrants.Controls.Add(cmbQuadrantProfile);
        
        // Separate email per quadrant option
        chkSeparateEmailPerQuadrant = new CheckBox 
        { 
            Text = "Enviar email separado por cada cuadrante", 
            Location = new Point(15, 135), 
            AutoSize = true,
            Enabled = false,
            Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };
        groupQuadrants.Controls.Add(chkSeparateEmailPerQuadrant);
        
        btnTestQuadrantProcessing = new Button 
        { 
            Text = "Test", 
            Location = new Point(505, 50), 
            Size = new Size(60, 25),
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnTestQuadrantProcessing.Click += BtnTestQuadrantProcessing_Click;
        groupQuadrants.Controls.Add(btnTestQuadrantProcessing);
        
        lblQuadrantProcessingNote = new Label 
        { 
            Text = "Los cuadrantes seleccionados se incluirán en cada reporte automático", 
            Location = new Point(15, 155), 
            Size = new Size(600, 15), 
            Font = new Font("Microsoft Sans Serif", 7.5F),
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        groupQuadrants.Controls.Add(lblQuadrantProcessingNote);
        
        mainPanel.Controls.Add(groupQuadrants);
        y += 220;

        // === PREVIEW AND CONTROL SECTION ===
        groupPreview = new GroupBox 
        { 
            Text = "Vista Previa y Control", 
            Location = new Point(20, y), 
            Size = new Size(640, 80), 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
        };
        
        lblNextScheduledEmail = new Label 
        { 
            Location = new Point(15, 25), 
            Size = new Size(600, 20), 
            Text = "Próximo reporte programado: No programado",
            Font = new Font("Microsoft Sans Serif", 8.25F),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        groupPreview.Controls.Add(lblNextScheduledEmail);
        
        lblPreviewInfo = new Label 
        { 
            Location = new Point(15, 45), 
            Size = new Size(600, 20), 
            Text = "Vista previa: 0 destinatarios, formato ZIP, sin cuadrantes",
            Font = new Font("Microsoft Sans Serif", 8.25F),
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        groupPreview.Controls.Add(lblPreviewInfo);
        
        mainPanel.Controls.Add(groupPreview);
        y += 100;

        // Progress bar (initially hidden)
        progressBar = new ProgressBar 
        { 
            Location = new Point(20, y), 
            Size = new Size(640, 20), 
            Visible = false, 
            Style = ProgressBarStyle.Continuous, 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
        };
        lblProgress = new Label 
        { 
            Location = new Point(20, y + 25), 
            Size = new Size(640, 20), 
            Text = "", 
            Visible = false, 
            TextAlign = ContentAlignment.MiddleCenter, 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
        };
        mainPanel.Controls.AddRange(new Control[] { progressBar, lblProgress });
        y += 50;
        
        // === BOTTOM BUTTONS ===
        btnSave = new Button 
        { 
            Text = "Guardar Configuración", 
            Location = new Point(350, y), 
            Size = new Size(140, 30), 
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnSave.Click += BtnSave_Click;
        
        btnTestEmail = new Button 
        { 
            Text = "Enviar Test", 
            Location = new Point(240, y), 
            Size = new Size(100, 30),
            BackColor = Color.FromArgb(255, 193, 7),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnTestEmail.Click += BtnTestEmail_Click;
        
        btnPreviewSchedule = new Button 
        { 
            Text = "Vista Horarios", 
            Location = new Point(130, y), 
            Size = new Size(100, 30),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnPreviewSchedule.Click += BtnPreviewSchedule_Click;
        
        btnCancel = new Button 
        { 
            Text = "Cancelar", 
            Location = new Point(500, y), 
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnCancel.Click += BtnCancel_Click;
        
        mainPanel.Controls.AddRange(new Control[] { btnSave, btnTestEmail, btnPreviewSchedule, btnCancel });
        
        // Set up proper scrolling
        mainPanel.AutoScrollMinSize = new Size(640, y + 60);
        
        // Handle form resize events
        this.Resize += (s, e) => 
        {
            if (mainPanel != null)
            {
                mainPanel.Size = new Size(this.ClientSize.Width, this.ClientSize.Height);
                mainPanel.AutoScrollMinSize = new Size(Math.Max(640, this.ClientSize.Width - 40), y + 60);
            }
        };
    }

    private async void LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
            
            // Load main settings
            chkEnableRoutineReports.Checked = _config.Schedule.EnableAutomaticReports;
            cmbReportFrequency.SelectedIndex = (int)_config.Schedule.Frequency switch
            {
                1 => 0,  // Daily
                7 => 1,  // Weekly  
                30 => 2, // Monthly
                _ => 3   // Custom
            };
            
            numCustomDays.Value = _config.Schedule.CustomDays;
            cmbReportTime.SelectedIndex = _config.Schedule.ReportTime.Hours;
            cmbWeeklyDay.SelectedIndex = (int)_config.Schedule.WeeklyReportDay;
            
            // Load time filter settings
            chkUseTimeFilter.Checked = _config.Schedule.UseTimeFilter;
            cmbStartTime.SelectedIndex = _config.Schedule.StartTime.Hours;
            cmbEndTime.SelectedIndex = _config.Schedule.EndTime.Hours;
            chkIncludeWeekends.Checked = _config.Schedule.IncludeWeekends;
            
            // Load week days selection
            for (int i = 0; i < clbWeekDays.Items.Count && i < _config.Schedule.ActiveWeekDays.Count; i++)
            {
                var dayOfWeek = (DayOfWeek)(i == 6 ? 0 : i + 1); // Convert to DayOfWeek (Sunday=0, Monday=1, etc.)
                clbWeekDays.SetItemChecked(i, _config.Schedule.ActiveWeekDays.Contains(dayOfWeek));
            }
            
            // Load recipients
            clbRecipients.Items.Clear();
            foreach (var recipient in _config.Email.Recipients)
            {
                clbRecipients.Items.Add(recipient, true);
            }
            
            // Load format settings
            rbZipFormat.Checked = _config.Email.UseZipFormat;
            rbIndividualFiles.Checked = !_config.Email.UseZipFormat;
            
            // Load quadrants if available
            LoadAvailableQuadrants();
            
            // Update UI state
            UpdateUIState();
            UpdateQuadrantProfileVisibility();
            UpdatePreviewInfo();
            
            // Reorganize layout based on visible elements (with slight delay to ensure all controls are ready)
            this.BeginInvoke(new Action(() => ReorganizeLayout()));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReorganizeLayout()
    {
        try
        {
            var y = 20;
            
            // Reposition elements based on visibility using direct references
            if (groupMain != null)
            {
                groupMain.Location = new Point(20, y);
                y += 160;
            }
            
            if (groupTimeFilter != null)
            {
                groupTimeFilter.Location = new Point(20, y);
                y += 140;
            }
            
            if (groupRecipients != null)
            {
                groupRecipients.Location = new Point(20, y);
                y += 180;
            }
            
            if (groupFormat != null)
            {
                groupFormat.Location = new Point(20, y);
                y += 120;
            }
            
            // Only add space for quadrants if they are visible
            if (groupQuadrants != null && groupQuadrants.Visible)
            {
                groupQuadrants.Location = new Point(20, y);
                y += 220;
            }
            
            if (groupPreview != null)
            {
                groupPreview.Location = new Point(20, y);
                y += 100;
            }
            
            // Update buttons and progress bar position
            var mainPanel = this.Controls.Cast<Control>().FirstOrDefault(c => c is Panel) as Panel;
            if (mainPanel != null)
            {
                // Position progress bar and buttons
                if (progressBar != null)
                {
                    progressBar.Location = new Point(20, y + 10);
                    y += 30;
                }
                
                if (lblProgress != null)
                {
                    lblProgress.Location = new Point(20, y + 5);
                    y += 30;
                }
                
                // Reposition control buttons
                var buttonY = y + 10;
                if (btnSave != null) btnSave.Location = new Point(20, buttonY);
                if (btnTestEmail != null) btnTestEmail.Location = new Point(120, buttonY);
                if (btnPreviewSchedule != null) btnPreviewSchedule.Location = new Point(220, buttonY);
                if (btnCancel != null) btnCancel.Location = new Point(320, buttonY);
                
                y += 50; // Height of buttons
                
                // Update scroll size
                mainPanel.AutoScrollMinSize = new Size(640, y + 20);
            }
        }
        catch (Exception ex)
        {
            // If reorganization fails, just log it and continue
            Console.WriteLine($"Error reorganizing layout: {ex.Message}");
        }
    }

    private void LoadAvailableQuadrants()
    {
        try
        {
            var availableQuadrants = _emailService.GetAvailableQuadrantFolders();
            
            if (availableQuadrants.Any())
            {
                groupQuadrants.Visible = true;
                
                clbQuadrants.Items.Clear();
                foreach (var quadrant in availableQuadrants)
                {
                    clbQuadrants.Items.Add(quadrant, false);
                }
                
                // Load quadrant system configuration if exists
                chkUseQuadrants.Checked = _config.Email.QuadrantSettings.UseQuadrantsInRoutineEmails;
                chkProcessQuadrantsFirst.Checked = _config.Email.QuadrantSettings.ProcessQuadrantsFirst;
                chkSeparateEmailPerQuadrant.Checked = _config.Email.QuadrantSettings.SendSeparateEmailPerQuadrant;
                
                // Load selected quadrants
                foreach (var selectedQuadrant in _config.Email.QuadrantSettings.SelectedQuadrants)
                {
                    var index = clbQuadrants.Items.IndexOf(selectedQuadrant);
                    if (index >= 0)
                    {
                        clbQuadrants.SetItemChecked(index, true);
                    }
                }
                
                // Load available quadrant profiles
                LoadQuadrantProfiles();
            }
            else
            {
                groupQuadrants.Visible = false;
                // Reorganize layout when quadrants are not available
                this.BeginInvoke(new Action(() => ReorganizeLayout()));
            }
        }
        catch (Exception ex)
        {
            groupQuadrants.Visible = false;
            this.BeginInvoke(new Action(() => ReorganizeLayout()));
            Console.WriteLine($"Error loading quadrants: {ex.Message}");
        }
    }

    #region Event Handlers

    private void ChkEnableRoutineReports_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateUIState();
        UpdatePreviewInfo();
    }

    private void CmbReportFrequency_SelectedIndexChanged(object? sender, EventArgs e)
    {
        // Show/hide custom controls based on frequency selection
        bool isCustom = cmbReportFrequency.SelectedIndex == 3;
        bool isWeekly = cmbReportFrequency.SelectedIndex == 1;
        
        numCustomDays.Visible = isCustom;
        cmbWeeklyDay.Visible = isWeekly;
        
        UpdatePreviewInfo();
    }

    private void ChkUseQuadrants_CheckedChanged(object? sender, EventArgs e)
    {
        clbQuadrants.Enabled = chkUseQuadrants.Checked;
        chkProcessQuadrantsFirst.Enabled = chkUseQuadrants.Checked;
        chkSeparateEmailPerQuadrant.Enabled = chkUseQuadrants.Checked;
        btnTestQuadrantProcessing.Enabled = chkUseQuadrants.Checked;
        
        if (!chkUseQuadrants.Checked)
        {
            for (int i = 0; i < clbQuadrants.Items.Count; i++)
            {
                clbQuadrants.SetItemChecked(i, false);
            }
            chkProcessQuadrantsFirst.Checked = false;
            chkSeparateEmailPerQuadrant.Checked = false;
        }
        
        UpdateQuadrantProfileVisibility();
        UpdatePreviewInfo();
    }

    private void ChkProcessQuadrantsFirst_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateQuadrantProfileVisibility();
    }

    private void ClbRecipients_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        // Use BeginInvoke to ensure the checkbox state is updated before UpdatePreviewInfo
        this.BeginInvoke(new Action(UpdatePreviewInfo));
    }

    private void TxtCustomEmail_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            BtnAddCustom_Click(sender, e);
        }
    }

    private void BtnAddCustom_Click(object? sender, EventArgs e)
    {
        var email = txtCustomEmail.Text.Trim();
        if (!string.IsNullOrEmpty(email) && email.Contains("@"))
        {
            if (!clbRecipients.Items.Cast<string>().Contains(email))
            {
                clbRecipients.Items.Add(email, true);
                txtCustomEmail.Clear();
                UpdatePreviewInfo();
            }
            else
            {
                MessageBox.Show("Este email ya está en la lista", "Email duplicado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        else
        {
            MessageBox.Show("Ingrese un email válido", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnRemoveSelected_Click(object? sender, EventArgs e)
    {
        var selectedIndices = clbRecipients.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToArray();
        
        if (selectedIndices.Length == 0)
        {
            MessageBox.Show("Seleccione emails para remover", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        var result = MessageBox.Show($"¿Remover {selectedIndices.Length} email(s) seleccionado(s)?", 
            "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        
        if (result == DialogResult.Yes)
        {
            foreach (int index in selectedIndices)
            {
                clbRecipients.Items.RemoveAt(index);
            }
            UpdatePreviewInfo();
        }
    }

    private async void BtnTestEmail_Click(object? sender, EventArgs e)
    {
        if (clbRecipients.CheckedItems.Count == 0)
        {
            MessageBox.Show("Seleccione al menos un destinatario para el test", "Sin destinatarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show($"¿Enviar email de test a {clbRecipients.CheckedItems.Count} destinatarios?\n\nEste será un reporte de prueba con capturas de los últimos 7 días.", 
            "Confirmar test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        try
        {
            btnTestEmail.Enabled = false;
            btnTestEmail.Text = "Enviando...";
            
            ShowProgress("Enviando email de test...", 0);

            var recipients = clbRecipients.CheckedItems.Cast<string>().ToList();
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now;
            var useZipFormat = rbZipFormat.Checked;
            
            bool success;
            
            if (chkUseQuadrants.Checked && clbQuadrants.CheckedItems.Count > 0)
            {
                var selectedQuadrants = clbQuadrants.CheckedItems.Cast<string>().ToList();
                bool separateEmails = chkSeparateEmailPerQuadrant.Checked;
                success = await _emailService.SendRoutineQuadrantReportsAsync(recipients, startDate, endDate, selectedQuadrants, useZipFormat, separateEmails);
            }
            else
            {
                success = await _emailService.SendManualReportAsync(recipients, startDate, endDate, useZipFormat);
            }
            
            ShowProgress(success ? "Test enviado exitosamente!" : "Error en el test", 100);
            await Task.Delay(1000);

            if (success)
            {
                MessageBox.Show("Email de test enviado exitosamente", "Test exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Error enviando email de test. Revise la configuración de SMTP.", "Error en test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error enviando test: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestEmail.Enabled = true;
            btnTestEmail.Text = "Enviar Test";
            HideProgress();
        }
    }

    private void BtnSelectQuadrants_Click(object? sender, EventArgs e)
    {
        try
        {
            var availableQuadrants = _emailService.GetAvailableQuadrantFolders();
            
            if (!availableQuadrants.Any())
            {
                MessageBox.Show("No hay cuadrantes disponibles en el sistema.", "Información", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            using var dialog = new QuadrantSelectionDialog(availableQuadrants, _selectedQuadrantsList);
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _selectedQuadrantsList = dialog.SelectedQuadrants;
                UpdateSelectedQuadrantsLabel();
                
                // Update the checkbox list to match dialog selection (for backwards compatibility)
                UpdateQuadrantsCheckboxFromSelection();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir la selección de cuadrantes: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateSelectedQuadrantsLabel()
    {
        if (_selectedQuadrantsList.Count == 0)
        {
            lblSelectedQuadrants.Text = "Ningún cuadrante seleccionado";
            lblSelectedQuadrants.ForeColor = Color.Gray;
        }
        else
        {
            lblSelectedQuadrants.Text = $"Seleccionados: {string.Join(", ", _selectedQuadrantsList)}";
            lblSelectedQuadrants.ForeColor = Color.FromArgb(40, 167, 69);
        }
    }

    private void UpdateQuadrantsCheckboxFromSelection()
    {
        // Update checkbox list to match the dialog selection for backwards compatibility
        for (int i = 0; i < clbQuadrants.Items.Count; i++)
        {
            var item = clbQuadrants.Items[i].ToString();
            clbQuadrants.SetItemChecked(i, _selectedQuadrantsList.Contains(item));
        }
    }

    private void BtnTestQuadrantProcessing_Click(object? sender, EventArgs e)
    {
        if (clbQuadrants.CheckedItems.Count == 0)
        {
            MessageBox.Show("Seleccione al menos un cuadrante para el test", "Sin cuadrantes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedQuadrants = clbQuadrants.CheckedItems.Cast<string>().ToList();
        var quadrantList = string.Join(", ", selectedQuadrants);
        
        MessageBox.Show($"Test de procesamiento de cuadrantes:\n\nCuadrantes seleccionados: {quadrantList}\n\nEn un reporte real, estos cuadrantes se procesarían automáticamente antes del envío si la opción 'Procesar cuadrantes primero' está habilitada.", 
            "Test de Cuadrantes", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnPreviewSchedule_Click(object? sender, EventArgs e)
    {
        var previewText = GetSchedulePreview();
        MessageBox.Show(previewText, "Vista Previa de Horarios", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            if (chkEnableRoutineReports.Checked && clbRecipients.CheckedItems.Count == 0)
            {
                MessageBox.Show("Debe seleccionar al menos un destinatario para habilitar los reportes automáticos", 
                    "Sin destinatarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Update configuration
            _config.Schedule.EnableAutomaticReports = chkEnableRoutineReports.Checked;
            
            _config.Schedule.Frequency = cmbReportFrequency.SelectedIndex switch
            {
                0 => ReportFrequency.Daily,
                1 => ReportFrequency.Weekly,
                2 => ReportFrequency.Monthly,
                3 => ReportFrequency.Custom,
                _ => ReportFrequency.Weekly
            };
            
            _config.Schedule.CustomDays = (int)numCustomDays.Value;
            _config.Schedule.ReportTime = TimeSpan.FromHours(cmbReportTime.SelectedIndex);
            _config.Schedule.WeeklyReportDay = (DayOfWeek)cmbWeeklyDay.SelectedIndex;
            
            // Update time filter settings
            _config.Schedule.UseTimeFilter = chkUseTimeFilter.Checked;
            _config.Schedule.StartTime = TimeSpan.FromHours(cmbStartTime.SelectedIndex);
            _config.Schedule.EndTime = TimeSpan.FromHours(cmbEndTime.SelectedIndex);
            _config.Schedule.IncludeWeekends = chkIncludeWeekends.Checked;
            
            // Update active week days
            _config.Schedule.ActiveWeekDays.Clear();
            for (int i = 0; i < clbWeekDays.Items.Count; i++)
            {
                if (clbWeekDays.GetItemChecked(i))
                {
                    var dayOfWeek = (DayOfWeek)(i == 6 ? 0 : i + 1); // Convert from UI index to DayOfWeek
                    _config.Schedule.ActiveWeekDays.Add(dayOfWeek);
                }
            }
            
            // Update recipients - only save checked items
            _config.Email.Recipients = clbRecipients.CheckedItems.Cast<string>().ToList();
            
            // Update format preferences
            _config.Email.UseZipFormat = rbZipFormat.Checked;
            
            // Update quadrant settings
            _config.Email.QuadrantSettings.UseQuadrantsInRoutineEmails = chkUseQuadrants.Checked;
            _config.Email.QuadrantSettings.ProcessQuadrantsFirst = chkProcessQuadrantsFirst.Checked;
            _config.Email.QuadrantSettings.SendSeparateEmailPerQuadrant = chkSeparateEmailPerQuadrant.Checked;
            
            if (chkUseQuadrants.Checked)
            {
                _config.Email.QuadrantSettings.SelectedQuadrants = 
                    clbQuadrants.CheckedItems.Cast<string>().ToList();
                
                // Save selected profile
                if (cmbQuadrantProfile.SelectedItem != null)
                {
                    _config.Email.QuadrantSettings.ProcessingProfile = cmbQuadrantProfile.SelectedItem.ToString() ?? "Default";
                }
            }
            else
            {
                _config.Email.QuadrantSettings.SelectedQuadrants.Clear();
            }
            
            await _configManager.SaveConfigurationAsync(_config);
            
            MessageBox.Show("Configuración de reportes automáticos guardada exitosamente.\n\nLos cambios se aplicarán automáticamente.", 
                "Configuración guardada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error guardando configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void ChkUseTimeFilter_CheckedChanged(object? sender, EventArgs e)
    {
        bool enabled = chkUseTimeFilter.Checked;
        
        lblStartTime.Enabled = enabled;
        cmbStartTime.Enabled = enabled;
        lblEndTime.Enabled = enabled;
        cmbEndTime.Enabled = enabled;
        
        UpdatePreviewInfo();
    }

    private void ChkIncludeWeekends_CheckedChanged(object? sender, EventArgs e)
    {
        bool includeWeekends = chkIncludeWeekends.Checked;
        
        // Update weekend checkboxes in day list
        if (includeWeekends)
        {
            clbWeekDays.SetItemChecked(5, true); // Saturday
            clbWeekDays.SetItemChecked(6, true); // Sunday
        }
        else
        {
            clbWeekDays.SetItemChecked(5, false); // Saturday
            clbWeekDays.SetItemChecked(6, false); // Sunday
        }
        
        UpdatePreviewInfo();
    }

    #endregion

    #region Helper Methods
    
    private void LoadQuadrantProfiles()
    {
        try
        {
            cmbQuadrantProfile.Items.Clear();
            
            // Load from quadrant system if available
            if (_quadrantService != null)
            {
                var configurations = _config.QuadrantSystem.Configurations;
                foreach (var config in configurations)
                {
                    cmbQuadrantProfile.Items.Add(config.Name);
                }
            }
            
            // Add default profile if no profiles exist
            if (cmbQuadrantProfile.Items.Count == 0)
            {
                cmbQuadrantProfile.Items.Add("Default");
            }
            
            // Select the configured profile or first available
            var profileToSelect = _config.Email.QuadrantSettings.ProcessingProfile;
            var index = cmbQuadrantProfile.Items.IndexOf(profileToSelect);
            cmbQuadrantProfile.SelectedIndex = index >= 0 ? index : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading quadrant profiles: {ex.Message}");
            cmbQuadrantProfile.Items.Clear();
            cmbQuadrantProfile.Items.Add("Default");
            cmbQuadrantProfile.SelectedIndex = 0;
        }
    }
    
    private void UpdateQuadrantProfileVisibility()
    {
        bool showProfile = chkUseQuadrants.Checked && chkProcessQuadrantsFirst.Checked;
        lblQuadrantProfile.Visible = showProfile;
        cmbQuadrantProfile.Visible = showProfile;
        cmbQuadrantProfile.Enabled = showProfile;
    }

    private void UpdateUIState()
    {
        bool enabled = chkEnableRoutineReports.Checked;
        
        // Enable/disable controls based on main checkbox
        cmbReportFrequency.Enabled = enabled;
        numCustomDays.Enabled = enabled;
        cmbReportTime.Enabled = enabled;
        cmbWeeklyDay.Enabled = enabled;
        groupFormat.Enabled = enabled;
        groupTimeFilter.Enabled = enabled;
        
        if (groupQuadrants.Visible)
        {
            groupQuadrants.Enabled = enabled;
        }
        
        // Enable/disable time filter controls based on checkbox
        bool timeFilterEnabled = enabled && chkUseTimeFilter.Checked;
        lblStartTime.Enabled = timeFilterEnabled;
        cmbStartTime.Enabled = timeFilterEnabled;
        lblEndTime.Enabled = timeFilterEnabled;
        cmbEndTime.Enabled = timeFilterEnabled;
        
        // Update status label
        lblCurrentStatus.Text = enabled ? "Estado: Reportes automáticos habilitados" : "Estado: Reportes automáticos deshabilitados";
        lblCurrentStatus.ForeColor = enabled ? Color.DarkGreen : Color.DarkRed;
    }

    private void UpdatePreviewInfo()
    {
        var recipientCount = clbRecipients.CheckedItems.Count;
        var format = rbZipFormat.Checked ? "ZIP" : "archivos individuales";
        var quadrantInfo = "";
        
        if (chkUseQuadrants.Checked && clbQuadrants.CheckedItems.Count > 0)
        {
            var quadrantCount = clbQuadrants.CheckedItems.Count;
            var separateEmails = chkSeparateEmailPerQuadrant.Checked ? " (email por cuadrante)" : "";
            quadrantInfo = $", {quadrantCount} cuadrantes{separateEmails}";
        }
        else
        {
            quadrantInfo = ", sin cuadrantes";
        }
            
        lblPreviewInfo.Text = $"Vista previa: {recipientCount} destinatarios, formato {format}{quadrantInfo}";
        lblRecipientsCount.Text = $"{recipientCount} seleccionados";
        
        // Update next scheduled email
        if (chkEnableRoutineReports.Checked && recipientCount > 0)
        {
            var nextDate = CalculateNextReportDate();
            lblNextScheduledEmail.Text = $"Próximo reporte programado: {nextDate:yyyy-MM-dd HH:mm}";
            lblNextScheduledEmail.ForeColor = Color.DarkGreen;
        }
        else
        {
            lblNextScheduledEmail.Text = "Próximo reporte programado: No programado";
            lblNextScheduledEmail.ForeColor = Color.DarkRed;
        }
    }

    private DateTime CalculateNextReportDate()
    {
        var now = DateTime.Now;
        var reportTime = TimeSpan.FromHours(cmbReportTime.SelectedIndex);
        
        return cmbReportFrequency.SelectedIndex switch
        {
            0 => now.Date.Add(reportTime).AddDays(now.TimeOfDay > reportTime ? 1 : 0), // Daily
            1 => GetNextWeekday((DayOfWeek)cmbWeeklyDay.SelectedIndex, reportTime), // Weekly
            2 => new DateTime(now.Year, now.Month, 1).AddMonths(1).Add(reportTime), // Monthly
            3 => now.Date.Add(reportTime).AddDays(now.TimeOfDay > reportTime ? (int)numCustomDays.Value : 0), // Custom
            _ => now.AddDays(7)
        };
    }

    private DateTime GetNextWeekday(DayOfWeek dayOfWeek, TimeSpan time)
    {
        var now = DateTime.Now;
        var daysUntilTarget = ((int)dayOfWeek - (int)now.DayOfWeek + 7) % 7;
        
        if (daysUntilTarget == 0 && now.TimeOfDay > time)
            daysUntilTarget = 7;
        
        return now.Date.AddDays(daysUntilTarget).Add(time);
    }

    private string GetSchedulePreview()
    {
        if (!chkEnableRoutineReports.Checked)
            return "Los reportes automáticos están deshabilitados.";
            
        var frequency = cmbReportFrequency.SelectedItem?.ToString() ?? "No especificado";
        var time = $"{cmbReportTime.SelectedIndex:D2}:00";
        var recipients = string.Join(", ", clbRecipients.CheckedItems.Cast<string>());
        
        var preview = $"CONFIGURACIÓN DE REPORTES AUTOMÁTICOS\n";
        preview += $"=====================================\n\n";
        preview += $"Frecuencia: {frequency}\n";
        
        if (cmbReportFrequency.SelectedIndex == 1) // Weekly
        {
            preview += $"Día de la semana: {cmbWeeklyDay.SelectedItem}\n";
        }
        else if (cmbReportFrequency.SelectedIndex == 3) // Custom
        {
            preview += $"Cada {numCustomDays.Value} días\n";
        }
        
        preview += $"Hora: {time}\n";
        preview += $"Destinatarios ({clbRecipients.CheckedItems.Count}): {recipients}\n";
        preview += $"Formato: {(rbZipFormat.Checked ? "ZIP" : "Archivos individuales")}\n";
        
        if (chkUseQuadrants.Checked && clbQuadrants.CheckedItems.Count > 0)
        {
            var quadrants = string.Join(", ", clbQuadrants.CheckedItems.Cast<string>());
            preview += $"Cuadrantes: {quadrants}\n";
            preview += $"Procesamiento previo: {(chkProcessQuadrantsFirst.Checked ? "Sí" : "No")}\n";
            
            if (chkProcessQuadrantsFirst.Checked && cmbQuadrantProfile.SelectedItem != null)
            {
                preview += $"Perfil de procesamiento: {cmbQuadrantProfile.SelectedItem}\n";
            }
            
            preview += $"Email por cuadrante: {(chkSeparateEmailPerQuadrant.Checked ? "Sí" : "No")}\n";
        }
        
        preview += $"\nPróximo envío programado:\n{CalculateNextReportDate():yyyy-MM-dd HH:mm}";
        
        return preview;
    }

    private void ShowProgress(string message, int percentage)
    {
        progressBar.Value = percentage;
        progressBar.Visible = true;
        lblProgress.Text = message;
        lblProgress.Visible = true;
        Application.DoEvents();
    }

    private void HideProgress()
    {
        progressBar.Visible = false;
        lblProgress.Visible = false;
    }
    
    private Button CreateHelpButton(Point location, string helpText)
    {
        var helpButton = new Button
        {
            Text = "?",
            Size = new Size(20, 20),
            Location = location,
            Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold),
            ForeColor = Color.Blue,
            BackColor = Color.LightBlue,
            FlatStyle = FlatStyle.Popup,
            Cursor = Cursors.Help
        };
        
        _helpTooltip.SetToolTip(helpButton, helpText);
        
        return helpButton;
    }

    #endregion
}