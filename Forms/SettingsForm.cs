using Capturer.Models;
using Capturer.Services;

namespace Capturer.Forms;

public partial class SettingsForm : Form
{
    private readonly IConfigurationManager _configManager;
    private readonly IScreenshotService? _screenshotService;
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
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(Math.Max(0, backgroundColor.R - 20), Math.Max(0, backgroundColor.G - 20), Math.Max(0, backgroundColor.B - 20)) },
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
    }
    
    // Controls
    private NumericUpDown numCaptureInterval;
    private TextBox txtScreenshotFolder;
    private Button btnBrowseFolder;
    private CheckBox chkAutoStart;
    private ComboBox cmbCaptureMode;
    private ComboBox cmbSelectedScreen;
    private Label lblSelectedScreen;
    private Button btnRefreshScreens;
    private CheckBox chkIncludeCursor;
    private ComboBox cmbImageFormat;
    private NumericUpDown numImageQuality;
    private Label lblQuality;
    
    // Privacy blur controls
    private CheckBox chkEnableBlur;
    private NumericUpDown numBlurIntensity;
    private ComboBox cmbBlurMode;
    private Label lblBlurIntensity;
    private Label lblBlurMode;
    
    private TextBox txtSmtpServer;
    private NumericUpDown numSmtpPort;
    private ComboBox cmbSecurityMode;
    private Label lblSecurityMode;
    private TextBox txtUsername;
    private TextBox txtPassword;
    private Button btnTogglePassword;
    private TextBox txtRecipients;
    private Button btnTestConnection;
    private TextBox txtSenderName;
    // Routine email configuration moved to RoutineEmailForm
    
    private NumericUpDown numRetentionDays;
    private NumericUpDown numMaxSizeGB;
    private CheckBox chkAutoCleanup;
    
    // ★ NEW v3.2.1: Simplified background execution controls
    private CheckBox chkEnableBackgroundExecution;
    private CheckBox chkShowSystemTrayIcon;
    private CheckBox chkHideToTrayOnClose;
    private CheckBox chkShowTrayNotifications;
    private NumericUpDown numNotificationDuration;

    // Application controls (general)
    private CheckBox chkShowNotifications;

    // Legacy controls (kept for compatibility)
    private CheckBox chkMinimizeToTray;
    
    private Button btnSave;
    private Button btnCancel;

    public SettingsForm(IConfigurationManager configManager, IScreenshotService? screenshotService = null)
    {
        _configManager = configManager;
        _screenshotService = screenshotService;
        _config = new CapturerConfiguration();
        _helpTooltip = new ToolTip();
        
        InitializeComponent();
        LoadConfigurationAsync();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(720, 700);
        this.Text = "⚙️ Configuración del Sistema - Capturer v3.2.0";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable; // Allow resizing
        this.MaximizeBox = true;
        this.MinimizeBox = true;
        this.MinimumSize = new Size(650, 500); // Set minimum size
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

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point),
            Padding = new Point(15, 8),
            Appearance = TabAppearance.FlatButtons
        };
        
        // Screenshot tab with scrollable content
        var screenshotTab = new TabPage("📷 Capturas")
        {
            BackColor = Color.White,
            AutoScroll = true,
            Padding = new Padding(15)
        };
        CreateScreenshotControls(screenshotTab);
        tabControl.TabPages.Add(screenshotTab);
        
        // Email tab with scrollable content
        var emailTab = new TabPage("📧 Email")
        {
            BackColor = Color.White,
            AutoScroll = true,
            Padding = new Padding(15)
        };
        CreateEmailControls(emailTab);
        tabControl.TabPages.Add(emailTab);
        
        // Storage tab with scrollable content
        var storageTab = new TabPage("💾 Almacenamiento")
        {
            BackColor = Color.White,
            AutoScroll = true,
            Padding = new Padding(15)
        };
        CreateStorageControls(storageTab);
        tabControl.TabPages.Add(storageTab);
        
        // Application tab with scrollable content
        var applicationTab = new TabPage("⚙️ Aplicación")
        {
            BackColor = Color.White,
            AutoScroll = true,
            Padding = new Padding(15)
        };
        CreateApplicationControls(applicationTab);
        tabControl.TabPages.Add(applicationTab);
        
        // Add the TabControl to the form
        this.Controls.Add(tabControl);
        
        // Bottom buttons
        var buttonPanel = new Panel();
        buttonPanel.Height = 50;
        buttonPanel.Dock = DockStyle.Bottom;
        
        btnSave = new Button { Text = "Guardar", Size = new Size(80, 30), Location = new Point(480, 10) };
        btnCancel = new Button { Text = "Cancelar", Size = new Size(80, 30), Location = new Point(570, 10) };
        
        btnSave.Click += BtnSave_Click;
        btnCancel.Click += BtnCancel_Click;
        
        buttonPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });
        this.Controls.Add(buttonPanel);
    }

    private void CreateScreenshotControls(TabPage tab)
    {
        var y = 20;
        
        tab.Controls.Add(new Label { Text = "Intervalo de captura (minutos):", Location = new Point(20, y), Size = new Size(200, 23) });
        numCaptureInterval = new NumericUpDown { Location = new Point(230, y - 3), Width = 120, Minimum = 1, Maximum = 1440 };
        tab.Controls.Add(numCaptureInterval);
        tab.Controls.Add(CreateHelpButton(new Point(360, y), "Frecuencia con la que se tomarán las capturas de pantalla automáticamente. Rango: 1-1440 minutos."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Modo de captura:", Location = new Point(20, y), Size = new Size(200, 23) });
        cmbCaptureMode = new ComboBox 
        {
            Location = new Point(230, y - 3), 
            Width = 200, 
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbCaptureMode.Items.AddRange(new[] { "Todas las pantallas", "Pantalla específica", "Pantalla principal" });
        cmbCaptureMode.SelectedIndexChanged += CmbCaptureMode_SelectedIndexChanged;
        tab.Controls.Add(cmbCaptureMode);
        tab.Controls.Add(CreateHelpButton(new Point(440, y), "Selecciona qué pantallas capturar: todas las pantallas como una imagen grande, una pantalla específica, o solo la pantalla principal."));
        
        y += 35;
        lblSelectedScreen = new Label { Text = "Pantalla seleccionada:", Location = new Point(20, y), Size = new Size(200, 23) };
        tab.Controls.Add(lblSelectedScreen);
        cmbSelectedScreen = new ComboBox 
        {
            Location = new Point(230, y - 3), 
            Width = 280, 
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        btnRefreshScreens = new Button 
        {
            Text = "🔄",
            Location = new Point(520, y - 3),
            Size = new Size(30, 23),
            Font = new Font("Segoe UI", 9)
        };
        btnRefreshScreens.Click += BtnRefreshScreens_Click;
        tab.Controls.AddRange(new Control[] { cmbSelectedScreen, btnRefreshScreens });
        tab.Controls.Add(CreateHelpButton(new Point(560, y), "Selecciona la pantalla específica a capturar cuando el modo es 'Pantalla específica'. Use el botón de actualizar para detectar cambios en monitores."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Carpeta de screenshots:", Location = new Point(20, y), Size = new Size(200, 23) });
        txtScreenshotFolder = new TextBox { Location = new Point(230, y - 3), Width = 280 };
        btnBrowseFolder = new Button { Text = "...", Location = new Point(520, y - 3), Size = new Size(30, 23) };
        btnBrowseFolder.Click += BtnBrowseFolder_Click;
        tab.Controls.AddRange(new Control[] { txtScreenshotFolder, btnBrowseFolder });
        tab.Controls.Add(CreateHelpButton(new Point(560, y), "Carpeta donde se guardarán todos los archivos de capturas de pantalla. Puede usar el botón [...] para seleccionar."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Formato de imagen:", Location = new Point(20, y), Size = new Size(200, 23) });
        cmbImageFormat = new ComboBox 
        {
            Location = new Point(230, y - 3), 
            Width = 120, 
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbImageFormat.Items.AddRange(new[] { "PNG", "JPEG", "BMP", "GIF" });
        cmbImageFormat.SelectedIndexChanged += CmbImageFormat_SelectedIndexChanged;
        tab.Controls.Add(cmbImageFormat);
        
        lblQuality = new Label { Text = "Calidad:", Location = new Point(360, y), Size = new Size(60, 23) };
        tab.Controls.Add(lblQuality);
        numImageQuality = new NumericUpDown 
        {
            Location = new Point(420, y - 3), 
            Width = 60, 
            Minimum = 10, 
            Maximum = 100, 
            Value = 90
        };
        tab.Controls.Add(numImageQuality);
        tab.Controls.Add(CreateHelpButton(new Point(490, y), "Formato de archivo para guardar las imágenes. PNG ofrece mejor calidad sin pérdida, JPEG ocupa menos espacio. La calidad solo aplica a JPEG (10-100)."));
        
        y += 35;
        chkAutoStart = new CheckBox { Text = "Iniciar captura automáticamente", Location = new Point(20, y), AutoSize = true };
        tab.Controls.Add(chkAutoStart);
        tab.Controls.Add(CreateHelpButton(new Point(250, y + 2), "Si está habilitado, la captura de screenshots comenzará automáticamente al abrir la aplicación."));
        
        y += 25;
        chkIncludeCursor = new CheckBox { Text = "Incluir cursor en capturas", Location = new Point(20, y), AutoSize = true };
        tab.Controls.Add(chkIncludeCursor);
        tab.Controls.Add(CreateHelpButton(new Point(200, y + 2), "Si está habilitado, el cursor del mouse aparecerá en las capturas de pantalla."));
        
        // Privacy blur section
        y += 35;
        var lblPrivacySection = new Label { Text = "Configuración de Privacidad:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        tab.Controls.Add(lblPrivacySection);
        
        y += 25;
        chkEnableBlur = new CheckBox { Text = "Activar desenfoque de privacidad", Location = new Point(20, y), AutoSize = true };
        tab.Controls.Add(chkEnableBlur);
        tab.Controls.Add(CreateHelpButton(new Point(240, y + 2), "Aplica un efecto de desenfoque a las capturas para proteger información sensible y mantener privacidad."));
        
        y += 25;
        lblBlurIntensity = new Label { Text = "Intensidad de desenfoque (1-10):", Location = new Point(40, y), Size = new Size(180, 23) };
        numBlurIntensity = new NumericUpDown 
        { 
            Location = new Point(220, y), 
            Size = new Size(80, 23),
            Minimum = 1,
            Maximum = 10,
            Value = 3,
            Enabled = false
        };
        tab.Controls.Add(lblBlurIntensity);
        tab.Controls.Add(numBlurIntensity);
        tab.Controls.Add(CreateHelpButton(new Point(310, y), "Intensidad del desenfoque: 1-3 (ligero), 4-6 (medio), 7-10 (fuerte). Mayor intensidad impacta el rendimiento."));
        
        y += 25;
        lblBlurMode = new Label { Text = "Tipo de desenfoque:", Location = new Point(40, y), Size = new Size(180, 23) };
        cmbBlurMode = new ComboBox 
        { 
            Location = new Point(220, y), 
            Size = new Size(120, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false
        };
        cmbBlurMode.Items.AddRange(new object[] { "Gaussiano", "Cuadrado", "Movimiento" });
        cmbBlurMode.SelectedIndex = 0;
        tab.Controls.Add(lblBlurMode);
        tab.Controls.Add(cmbBlurMode);
        tab.Controls.Add(CreateHelpButton(new Point(350, y), "Gaussiano: mejor calidad pero más lento. Cuadrado: más rápido. Movimiento: simula movimiento de cámara."));
        
        // Wire up blur checkbox event
        chkEnableBlur.CheckedChanged += (s, e) =>
        {
            var enabled = chkEnableBlur.Checked;
            lblBlurIntensity.Enabled = enabled;
            numBlurIntensity.Enabled = enabled;
            lblBlurMode.Enabled = enabled;
            cmbBlurMode.Enabled = enabled;
        };
        
        // Initialize screen selection
        RefreshScreenList();
    }

    private void CreateEmailControls(TabPage tab)
    {
        var y = 20;
        
        tab.Controls.Add(new Label { Text = "Servidor SMTP:", Location = new Point(20, y), Size = new Size(120, 23) });
        txtSmtpServer = new TextBox { Location = new Point(150, y - 3), Width = 280 };
        tab.Controls.Add(txtSmtpServer);
        tab.Controls.Add(CreateHelpButton(new Point(440, y), "Dirección del servidor SMTP para envío de emails. Ej: smtp.gmail.com, smtp.outlook.com"));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Puerto:", Location = new Point(20, y), Size = new Size(120, 23) });
        numSmtpPort = new NumericUpDown { Location = new Point(150, y - 3), Width = 120, Minimum = 1, Maximum = 65535, Value = 587 };
        tab.Controls.Add(numSmtpPort);
        tab.Controls.Add(CreateHelpButton(new Point(280, y), "Puerto del servidor SMTP. Común: 587 (TLS), 465 (SSL), 25 (sin cifrado)."));
        
        y += 35;
        lblSecurityMode = new Label { Text = "Seguridad:", Location = new Point(20, y), Size = new Size(120, 23) };
        tab.Controls.Add(lblSecurityMode);
        cmbSecurityMode = new ComboBox 
        {
            Location = new Point(150, y - 3), 
            Width = 150, 
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbSecurityMode.Items.AddRange(new[] { "Sin seguridad", "STARTTLS", "SSL/TLS", "Automático" });
        cmbSecurityMode.SelectedIndex = 1; // Default to STARTTLS
        cmbSecurityMode.SelectedIndexChanged += CmbSecurityMode_SelectedIndexChanged;
        tab.Controls.Add(cmbSecurityMode);
        tab.Controls.Add(CreateHelpButton(new Point(310, y), "Modo de seguridad SMTP. STARTTLS (587), SSL/TLS (465), Sin seguridad (25), Automático detecta el mejor."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Nombre del remitente:", Location = new Point(20, y), Size = new Size(120, 23) });
        txtSenderName = new TextBox { Location = new Point(150, y - 3), Width = 280 };
        tab.Controls.Add(txtSenderName);
        tab.Controls.Add(CreateHelpButton(new Point(440, y), "Nombre que aparecerá como remitente en los emails enviados."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Usuario:", Location = new Point(20, y), Size = new Size(120, 23) });
        txtUsername = new TextBox { Location = new Point(150, y - 3), Width = 280 };
        tab.Controls.Add(txtUsername);
        tab.Controls.Add(CreateHelpButton(new Point(440, y), "Nombre de usuario o dirección de email para autenticarse en el servidor SMTP."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Contraseña:", Location = new Point(20, y), Size = new Size(120, 23) });
        txtPassword = new TextBox { Location = new Point(150, y - 3), Width = 250, UseSystemPasswordChar = true };
        btnTogglePassword = new Button 
        {
            Text = "👁",
            Location = new Point(405, y - 3),
            Size = new Size(25, 23),
            Font = new Font("Segoe UI", 9),
            FlatStyle = FlatStyle.Flat
        };
        btnTogglePassword.Click += BtnTogglePassword_Click;
        tab.Controls.AddRange(new Control[] { txtPassword, btnTogglePassword });
        tab.Controls.Add(CreateHelpButton(new Point(440, y), "Contraseña de la cuenta de email. Se almacena de forma segura y encriptada. Use el botón del ojo para mostrar/ocultar."));
        
        y += 35;
        btnTestConnection = CreateModernButton("🔧 Probar Conexión", new Point(150, y), new Size(140, 30), Color.FromArgb(40, 167, 69));
        btnTestConnection.Click += BtnTestConnection_Click;
        tab.Controls.Add(btnTestConnection);
        tab.Controls.Add(CreateHelpButton(new Point(300, y + 5), "Prueba la conectividad con el servidor SMTP usando la configuración actual."));
        
        y += 45;
        tab.Controls.Add(new Label { Text = "Destinatarios (separados por ;):", Location = new Point(20, y), AutoSize = true });
        tab.Controls.Add(CreateHelpButton(new Point(250, y), "Lista de emails que recibirán los reportes. Separar múltiples direcciones con punto y coma (;)."));
        y += 25;
        txtRecipients = new TextBox { Location = new Point(20, y), Width = 570, Height = 60, Multiline = true };
        tab.Controls.Add(txtRecipients);
        
        // Note: Routine email configuration moved to dedicated RoutineEmailForm
        // Access via "Reportes Automáticos" button on main form
    }

    private void CreateStorageControls(TabPage tab)
    {
        var y = 20;
        
        tab.Controls.Add(new Label { Text = "Retener archivos por (días):", Location = new Point(20, y), Size = new Size(200, 23) });
        numRetentionDays = new NumericUpDown { Location = new Point(230, y - 3), Width = 120, Minimum = 1, Maximum = 3650, Value = 90 };
        tab.Controls.Add(numRetentionDays);
        tab.Controls.Add(CreateHelpButton(new Point(360, y), "Número de días que se mantendrán los screenshots antes de ser eliminados automáticamente. Rango: 1-3650 días."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Tamaño máximo carpeta (GB):", Location = new Point(20, y), Size = new Size(200, 23) });
        numMaxSizeGB = new NumericUpDown { Location = new Point(230, y - 3), Width = 120, Minimum = 1, Maximum = 1000, Value = 5 };
        tab.Controls.Add(numMaxSizeGB);
        tab.Controls.Add(CreateHelpButton(new Point(360, y), "Límite máximo de espacio en disco que puede ocupar la carpeta de screenshots (en Gigabytes)."));
        
        y += 35;
        chkAutoCleanup = new CheckBox { Text = "Limpieza automática", Location = new Point(20, y), AutoSize = true };
        tab.Controls.Add(chkAutoCleanup);
        tab.Controls.Add(CreateHelpButton(new Point(200, y + 2), "Habilita la eliminación automática de archivos antiguos basado en los criterios de retención y tamaño máximo."));
    }

    private void CreateApplicationControls(TabPage tab)
    {
        var y = 20;

        // ★ NEW v3.2.1: Simplified Background Execution Section
        var groupBackgroundExecution = new GroupBox
        {
            Text = "🔄 Ejecución en Segundo Plano (v3.2.1)",
            Location = new Point(20, y),
            Size = new Size(650, 200),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.DarkGreen
        };

        y = 25;
        chkEnableBackgroundExecution = new CheckBox
        {
            Text = "⭐ Ejecutar siempre en segundo plano (verificable en Administrador de Tareas)",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };
        groupBackgroundExecution.Controls.Add(chkEnableBackgroundExecution);

        y += 30;
        chkShowSystemTrayIcon = new CheckBox
        {
            Text = "🖥️ Mostrar icono en system tray (para fácil acceso)",
            Location = new Point(15, y),
            AutoSize = true
        };
        groupBackgroundExecution.Controls.Add(chkShowSystemTrayIcon);

        y += 25;
        chkHideToTrayOnClose = new CheckBox
        {
            Text = "⬇️ Ocultar al cerrar ventana (en lugar de cerrar completamente)",
            Location = new Point(15, y),
            AutoSize = true
        };
        groupBackgroundExecution.Controls.Add(chkHideToTrayOnClose);

        y += 25;
        chkShowTrayNotifications = new CheckBox
        {
            Text = "🔔 Mostrar notificaciones",
            Location = new Point(15, y),
            AutoSize = true
        };
        groupBackgroundExecution.Controls.Add(chkShowTrayNotifications);

        y += 30;
        groupBackgroundExecution.Controls.Add(new Label { Text = "Duración notificaciones (ms):", Location = new Point(15, y), Size = new Size(200, 23) });
        numNotificationDuration = new NumericUpDown
        {
            Location = new Point(220, y - 3),
            Width = 100,
            Minimum = 1000,
            Maximum = 10000,
            Value = 3000,
            Increment = 500
        };
        groupBackgroundExecution.Controls.Add(numNotificationDuration);

        // Add explanation label
        var lblExplanation = new Label
        {
            Text = "📝 Cuando 'Ejecutar en segundo plano' está habilitado, la app NUNCA se cierra completamente.\n✓ Verificar en Administrador de Tareas > Procesos > Capturer.exe",
            Location = new Point(15, y + 30),
            Size = new Size(620, 40),
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.DarkGreen,
            BackColor = Color.LightGreen
        };
        groupBackgroundExecution.Controls.Add(lblExplanation);

        tab.Controls.Add(groupBackgroundExecution);
        y += 220;

        // Legacy Application Settings Section
        var groupLegacy = new GroupBox
        {
            Text = "⚙️ Configuración General",
            Location = new Point(20, y),
            Size = new Size(650, 100),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var yLegacy = 25;
        chkMinimizeToTray = new CheckBox 
        { 
            Text = "Minimizar a system tray (legacy)", 
            Location = new Point(15, yLegacy), 
            AutoSize = true 
        };
        groupLegacy.Controls.Add(chkMinimizeToTray);

        yLegacy += 25;
        chkShowNotifications = new CheckBox 
        { 
            Text = "Mostrar notificaciones (legacy)", 
            Location = new Point(15, yLegacy), 
            AutoSize = true 
        };
        groupLegacy.Controls.Add(chkShowNotifications);

        tab.Controls.Add(groupLegacy);
    }

    private async void LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
            
            // Screenshot settings
            numCaptureInterval.Value = (decimal)_config.Screenshot.CaptureInterval.TotalMinutes;
            txtScreenshotFolder.Text = _config.Storage.ScreenshotFolder;
            chkAutoStart.Checked = _config.Screenshot.AutoStartCapture;
            chkIncludeCursor.Checked = _config.Screenshot.IncludeCursor;
            
            // Set capture mode
            cmbCaptureMode.SelectedIndex = (int)_config.Screenshot.CaptureMode;
            
            // Set image format
            cmbImageFormat.SelectedIndex = _config.Screenshot.ImageFormat.ToLower() switch
            {
                "jpeg" or "jpg" => 1,
                "bmp" => 2,
                "gif" => 3,
                _ => 0 // PNG
            };
            
            numImageQuality.Value = _config.Screenshot.Quality;
            
            // Privacy blur settings
            chkEnableBlur.Checked = _config.Screenshot.EnablePrivacyBlur;
            numBlurIntensity.Value = _config.Screenshot.BlurIntensity;
            cmbBlurMode.SelectedIndex = (int)_config.Screenshot.BlurMode;
            
            // Update UI visibility based on format and mode
            CmbImageFormat_SelectedIndexChanged(null, EventArgs.Empty);
            CmbCaptureMode_SelectedIndexChanged(null, EventArgs.Empty);
            
            // Set selected screen
            if (_config.Screenshot.CaptureMode == ScreenCaptureMode.SingleScreen)
            {
                cmbSelectedScreen.SelectedIndex = Math.Min(_config.Screenshot.SelectedScreenIndex, cmbSelectedScreen.Items.Count - 1);
            }
            
            // Email settings
            txtSmtpServer.Text = _config.Email.SmtpServer;
            numSmtpPort.Value = _config.Email.SmtpPort;
            cmbSecurityMode.SelectedIndex = (int)_config.Email.SecurityMode;
            txtSenderName.Text = _config.Email.SenderName;
            txtUsername.Text = _config.Email.Username;
            txtPassword.Text = _config.Email.Password;
            txtRecipients.Text = string.Join(";", _config.Email.Recipients);
            // Routine email configuration now handled by RoutineEmailForm
            
            // Storage settings
            numRetentionDays.Value = (decimal)_config.Storage.RetentionPeriod.TotalDays;
            numMaxSizeGB.Value = (decimal)(_config.Storage.MaxFolderSizeBytes / 1024 / 1024 / 1024);
            chkAutoCleanup.Checked = _config.Storage.AutoCleanup;
            
            // Application settings
            // ★ NEW v3.2.1: Load simplified background execution settings
            chkEnableBackgroundExecution.Checked = _config.Application.BackgroundExecution.EnableBackgroundExecution;
            chkShowSystemTrayIcon.Checked = _config.Application.BackgroundExecution.ShowSystemTrayIcon;
            chkHideToTrayOnClose.Checked = _config.Application.BackgroundExecution.HideToTrayOnClose;
            chkShowTrayNotifications.Checked = _config.Application.BackgroundExecution.ShowTrayNotifications;
            numNotificationDuration.Value = _config.Application.BackgroundExecution.NotificationDurationMs;

            // General application settings
            chkShowNotifications.Checked = _config.Application.ShowNotifications;

            // Legacy settings (for backward compatibility)
            chkMinimizeToTray.Checked = _config.Application.MinimizeToTray;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnBrowseFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Seleccionar carpeta para screenshots";
        dialog.SelectedPath = txtScreenshotFolder.Text;
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtScreenshotFolder.Text = dialog.SelectedPath;
        }
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // Update configuration from form
            _config.Screenshot.CaptureInterval = TimeSpan.FromMinutes((double)numCaptureInterval.Value);
            _config.Storage.ScreenshotFolder = txtScreenshotFolder.Text;
            _config.Screenshot.AutoStartCapture = chkAutoStart.Checked;
            _config.Screenshot.IncludeCursor = chkIncludeCursor.Checked;
            _config.Screenshot.CaptureMode = (ScreenCaptureMode)cmbCaptureMode.SelectedIndex;
            _config.Screenshot.SelectedScreenIndex = cmbSelectedScreen.SelectedIndex >= 0 ? cmbSelectedScreen.SelectedIndex : 0;
            _config.Screenshot.ImageFormat = cmbImageFormat.SelectedIndex switch
            {
                1 => "jpeg",
                2 => "bmp", 
                3 => "gif",
                _ => "png"
            };
            _config.Screenshot.Quality = (int)numImageQuality.Value;
            
            // Privacy blur settings
            _config.Screenshot.EnablePrivacyBlur = chkEnableBlur.Checked;
            _config.Screenshot.BlurIntensity = (int)numBlurIntensity.Value;
            _config.Screenshot.BlurMode = (BlurMode)cmbBlurMode.SelectedIndex;
            
            _config.Email.SmtpServer = txtSmtpServer.Text;
            _config.Email.SmtpPort = (int)numSmtpPort.Value;
            _config.Email.SecurityMode = (SmtpSecurityMode)cmbSecurityMode.SelectedIndex;
            _config.Email.SenderName = txtSenderName.Text;
            _config.Email.Username = txtUsername.Text;
            _config.Email.Password = txtPassword.Text;
            _config.Email.Recipients = txtRecipients.Text.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim()).ToList();
            // Routine email configuration now handled by RoutineEmailForm
            
            _config.Storage.RetentionPeriod = TimeSpan.FromDays((double)numRetentionDays.Value);
            _config.Storage.MaxFolderSizeBytes = (long)(numMaxSizeGB.Value * 1024 * 1024 * 1024);
            _config.Storage.AutoCleanup = chkAutoCleanup.Checked;
            
            // Application settings
            // ★ NEW v3.2.1: Save simplified background execution settings
            _config.Application.BackgroundExecution.EnableBackgroundExecution = chkEnableBackgroundExecution.Checked;
            _config.Application.BackgroundExecution.ShowSystemTrayIcon = chkShowSystemTrayIcon.Checked;
            _config.Application.BackgroundExecution.HideToTrayOnClose = chkHideToTrayOnClose.Checked;
            _config.Application.BackgroundExecution.ShowTrayNotifications = chkShowTrayNotifications.Checked;
            _config.Application.BackgroundExecution.NotificationDurationMs = (int)numNotificationDuration.Value;

            // General application settings
            _config.Application.ShowNotifications = chkShowNotifications.Checked;

            // Legacy settings (for backward compatibility)
            _config.Application.MinimizeToTray = chkMinimizeToTray.Checked;
            
            // Save configuration
            await _configManager.SaveConfigurationAsync(_config);
            
            MessageBox.Show("Configuración guardada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    
    private void BtnTogglePassword_Click(object? sender, EventArgs e)
    {
        txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
        btnTogglePassword.Text = txtPassword.UseSystemPasswordChar ? "👁" : "👈";
    }
    
    // CmbReportFrequency_SelectedIndexChanged removed - configuration moved to RoutineEmailForm
    
    private void CmbCaptureMode_SelectedIndexChanged(object? sender, EventArgs e)
    {
        bool showScreenSelection = cmbCaptureMode.SelectedIndex == 1; // Single screen
        lblSelectedScreen.Visible = showScreenSelection;
        cmbSelectedScreen.Visible = showScreenSelection;
        btnRefreshScreens.Visible = showScreenSelection;
    }
    
    private void CmbImageFormat_SelectedIndexChanged(object? sender, EventArgs e)
    {
        bool showQuality = cmbImageFormat.SelectedIndex == 1; // JPEG
        lblQuality.Visible = showQuality;
        numImageQuality.Visible = showQuality;
    }
    
    private void BtnRefreshScreens_Click(object? sender, EventArgs e)
    {
        RefreshScreenList();
    }
    
    private void RefreshScreenList()
    {
        try
        {
            cmbSelectedScreen.Items.Clear();
            
            List<ScreenInfo> screens;
            if (_screenshotService != null)
            {
                screens = _screenshotService.GetAvailableScreens();
            }
            else
            {
                // Fallback if service not available
                screens = new List<ScreenInfo>();
                var allScreens = Screen.AllScreens;
                for (int i = 0; i < allScreens.Length; i++)
                {
                    var screen = allScreens[i];
                    screens.Add(new ScreenInfo
                    {
                        Index = i,
                        DeviceName = screen.DeviceName,
                        DisplayName = $"Monitor {i + 1}",
                        Width = screen.Bounds.Width,
                        Height = screen.Bounds.Height,
                        X = screen.Bounds.X,
                        Y = screen.Bounds.Y,
                        IsPrimary = screen.Primary
                    });
                }
            }
            
            foreach (var screen in screens)
            {
                cmbSelectedScreen.Items.Add(screen.Description);
            }
            
            if (cmbSelectedScreen.Items.Count > 0 && cmbSelectedScreen.SelectedIndex < 0)
            {
                cmbSelectedScreen.SelectedIndex = 0;
            }
            
            // Update available screens in config
            _config.Screenshot.AvailableScreens = screens;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error refrescando lista de pantallas: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    
    private void CmbSecurityMode_SelectedIndexChanged(object? sender, EventArgs e)
    {
        // Auto-adjust port based on security mode
        if (cmbSecurityMode.SelectedIndex == 0) // None
        {
            numSmtpPort.Value = 25;
        }
        else if (cmbSecurityMode.SelectedIndex == 1) // STARTTLS
        {
            numSmtpPort.Value = 587;
        }
        else if (cmbSecurityMode.SelectedIndex == 2) // SSL
        {
            numSmtpPort.Value = 465;
        }
        // Auto mode keeps current port
    }
    
    private async void BtnTestConnection_Click(object? sender, EventArgs e)
    {
        btnTestConnection.Enabled = false;
        btnTestConnection.Text = "🔄 Probando...";
        
        try
        {
            // Update config with current form values
            _config.Email.SmtpServer = txtSmtpServer.Text.Trim();
            _config.Email.SmtpPort = (int)numSmtpPort.Value;
            _config.Email.SecurityMode = (SmtpSecurityMode)cmbSecurityMode.SelectedIndex;
            _config.Email.Username = txtUsername.Text.Trim();
            _config.Email.Password = txtPassword.Text;
            _config.Email.SenderName = txtSenderName.Text.Trim();
            
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_config.Email.SmtpServer))
            {
                MessageBox.Show("⚠️ El servidor SMTP es requerido.", "Configuración incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSmtpServer.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_config.Email.Username))
            {
                MessageBox.Show("⚠️ El usuario es requerido.", "Configuración incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_config.Email.Password))
            {
                MessageBox.Show("⚠️ La contraseña es requerida.", "Configuración incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }
            
            // Create temporary EmailService for testing
            var emailService = new EmailService(_configManager, new FileService(_configManager), null);
            
            // Force reload configuration with current values
            await _configManager.SaveConfigurationAsync(_config);
            
            var success = await emailService.TestConnectionAsync();
            
            if (success)
            {
                MessageBox.Show("✅ ¡Conexión exitosa!\n\nLa configuración SMTP está funcionando correctamente.", "Test de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("❌ Error de conexión\n\nNo se pudo conectar al servidor SMTP.\n\nVerifique:\n• Servidor y puerto correctos\n• Credenciales válidas\n• Configuración de seguridad\n• Conexión a internet", "Test de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Error durante el test:\n\n{ex.Message}", "Error de Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestConnection.Enabled = true;
            btnTestConnection.Text = "🔧 Probar Conexión";
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _helpTooltip?.Dispose();
        }
        base.Dispose(disposing);
    }
}