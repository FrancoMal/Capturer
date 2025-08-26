using Capturer.Models;
using Capturer.Services;
using MailKit.Net.Smtp;

namespace Capturer.Forms;

public partial class SettingsForm : Form
{
    private readonly IConfigurationManager _configManager;
    private CapturerConfiguration _config;
    private ToolTip _helpTooltip;
    
    // Controls
    private NumericUpDown numCaptureInterval;
    private TextBox txtScreenshotFolder;
    private Button btnBrowseFolder;
    private CheckBox chkAutoStart;
    
    private TextBox txtSmtpServer;
    private NumericUpDown numSmtpPort;
    private TextBox txtUsername;
    private TextBox txtPassword;
    private Button btnTogglePassword;
    private TextBox txtRecipients;
    private ComboBox cmbReportFrequency;
    private NumericUpDown numCustomDays;
    private Label lblCustomDays;
    private ComboBox cmbReportTime;
    private CheckBox chkEnableReports;
    
    private NumericUpDown numRetentionDays;
    private NumericUpDown numMaxSizeGB;
    private CheckBox chkAutoCleanup;
    
    private Button btnSave;
    private Button btnCancel;
    private Button btnTest;

    public SettingsForm(IConfigurationManager configManager)
    {
        _configManager = configManager;
        _config = new CapturerConfiguration();
        _helpTooltip = new ToolTip();
        
        InitializeComponent();
        LoadConfigurationAsync();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(650, 600);
        this.Text = "Configuración - Capturer";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var tabControl = new TabControl();
        tabControl.Dock = DockStyle.Fill;
        
        // Screenshot tab
        var screenshotTab = new TabPage("Screenshots");
        CreateScreenshotControls(screenshotTab);
        tabControl.TabPages.Add(screenshotTab);
        
        // Email tab
        var emailTab = new TabPage("Email");
        CreateEmailControls(emailTab);
        tabControl.TabPages.Add(emailTab);
        
        // Storage tab
        var storageTab = new TabPage("Almacenamiento");
        CreateStorageControls(storageTab);
        tabControl.TabPages.Add(storageTab);
        
        this.Controls.Add(tabControl);
        
        // Bottom buttons
        var buttonPanel = new Panel();
        buttonPanel.Height = 50;
        buttonPanel.Dock = DockStyle.Bottom;
        
        btnSave = new Button { Text = "Guardar", Size = new Size(80, 30), Location = new Point(470, 10) };
        btnCancel = new Button { Text = "Cancelar", Size = new Size(80, 30), Location = new Point(560, 10) };
        btnTest = new Button { Text = "Probar Email", Size = new Size(100, 30), Location = new Point(350, 10) };
        
        btnSave.Click += BtnSave_Click;
        btnCancel.Click += BtnCancel_Click;
        btnTest.Click += BtnTest_Click;
        
        buttonPanel.Controls.AddRange(new Control[] { btnSave, btnCancel, btnTest });
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
        tab.Controls.Add(new Label { Text = "Carpeta de screenshots:", Location = new Point(20, y), Size = new Size(200, 23) });
        txtScreenshotFolder = new TextBox { Location = new Point(230, y - 3), Width = 280 };
        btnBrowseFolder = new Button { Text = "...", Location = new Point(520, y - 3), Size = new Size(30, 23) };
        btnBrowseFolder.Click += BtnBrowseFolder_Click;
        tab.Controls.AddRange(new Control[] { txtScreenshotFolder, btnBrowseFolder });
        tab.Controls.Add(CreateHelpButton(new Point(560, y), "Carpeta donde se guardarán todos los archivos de capturas de pantalla. Puede usar el botón [...] para seleccionar."));
        
        y += 35;
        chkAutoStart = new CheckBox { Text = "Iniciar captura automáticamente", Location = new Point(20, y), AutoSize = true };
        tab.Controls.Add(chkAutoStart);
        tab.Controls.Add(CreateHelpButton(new Point(250, y + 2), "Si está habilitado, la captura de screenshots comenzará automáticamente al abrir la aplicación."));
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
        tab.Controls.Add(new Label { Text = "Destinatarios (separados por ;):", Location = new Point(20, y), AutoSize = true });
        tab.Controls.Add(CreateHelpButton(new Point(250, y), "Lista de emails que recibirán los reportes. Separar múltiples direcciones con punto y coma (;)."));
        y += 25;
        txtRecipients = new TextBox { Location = new Point(20, y), Width = 570, Height = 60, Multiline = true };
        tab.Controls.Add(txtRecipients);
        
        y += 75;
        chkEnableReports = new CheckBox { Text = "Habilitar reportes automáticos", Location = new Point(20, y), AutoSize = true };
        tab.Controls.Add(chkEnableReports);
        tab.Controls.Add(CreateHelpButton(new Point(220, y + 2), "Envía automáticamente reportes de screenshots según la programación configurada."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Frecuencia:", Location = new Point(20, y), Size = new Size(80, 23) });
        cmbReportFrequency = new ComboBox 
        {
            Location = new Point(110, y - 3), 
            Width = 120, 
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbReportFrequency.Items.AddRange(new[] { "Diario", "Semanal", "Mensual", "Personalizado" });
        cmbReportFrequency.SelectedIndexChanged += CmbReportFrequency_SelectedIndexChanged;
        tab.Controls.Add(cmbReportFrequency);
        
        lblCustomDays = new Label { Text = "Días:", Location = new Point(250, y), Size = new Size(40, 23), Visible = false };
        numCustomDays = new NumericUpDown 
        {
            Location = new Point(290, y - 3), 
            Width = 60, 
            Minimum = 1, 
            Maximum = 365, 
            Value = 7,
            Visible = false
        };
        tab.Controls.AddRange(new Control[] { lblCustomDays, numCustomDays });
        tab.Controls.Add(CreateHelpButton(new Point(360, y), "Frecuencia con la que se enviarán los reportes automáticos."));
        
        y += 35;
        tab.Controls.Add(new Label { Text = "Hora de envío:", Location = new Point(20, y), Size = new Size(100, 23) });
        cmbReportTime = new ComboBox 
        {
            Location = new Point(130, y - 3), 
            Width = 100, 
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        // Populate time options (every hour)
        for (int hour = 0; hour < 24; hour++)
        {
            cmbReportTime.Items.Add($"{hour:D2}:00");
        }
        cmbReportTime.SelectedIndex = 9; // Default to 9:00 AM
        tab.Controls.Add(cmbReportTime);
        tab.Controls.Add(CreateHelpButton(new Point(240, y), "Hora del día a la que se enviarán los reportes automáticos (formato 24 horas)."));
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

    private async void LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
            
            // Screenshot settings
            numCaptureInterval.Value = (decimal)_config.Screenshot.CaptureInterval.TotalMinutes;
            txtScreenshotFolder.Text = _config.Storage.ScreenshotFolder;
            chkAutoStart.Checked = _config.Screenshot.AutoStartCapture;
            
            // Email settings
            txtSmtpServer.Text = _config.Email.SmtpServer;
            numSmtpPort.Value = _config.Email.SmtpPort;
            txtUsername.Text = _config.Email.Username;
            txtPassword.Text = _config.Email.Password;
            txtRecipients.Text = string.Join(";", _config.Email.Recipients);
            chkEnableReports.Checked = _config.Schedule.EnableAutomaticReports;
            
            // Set frequency
            switch (_config.Schedule.Frequency)
            {
                case ReportFrequency.Daily:
                    cmbReportFrequency.SelectedIndex = 0;
                    break;
                case ReportFrequency.Weekly:
                    cmbReportFrequency.SelectedIndex = 1;
                    break;
                case ReportFrequency.Monthly:
                    cmbReportFrequency.SelectedIndex = 2;
                    break;
                case ReportFrequency.Custom:
                    cmbReportFrequency.SelectedIndex = 3;
                    numCustomDays.Value = _config.Schedule.CustomDays;
                    break;
            }
            
            // Set report time
            cmbReportTime.SelectedIndex = _config.Schedule.ReportTime.Hours;
            
            // Storage settings
            numRetentionDays.Value = (decimal)_config.Storage.RetentionPeriod.TotalDays;
            numMaxSizeGB.Value = (decimal)(_config.Storage.MaxFolderSizeBytes / 1024 / 1024 / 1024);
            chkAutoCleanup.Checked = _config.Storage.AutoCleanup;
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
            
            _config.Email.SmtpServer = txtSmtpServer.Text;
            _config.Email.SmtpPort = (int)numSmtpPort.Value;
            _config.Email.Username = txtUsername.Text;
            _config.Email.Password = txtPassword.Text;
            _config.Email.Recipients = txtRecipients.Text.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim()).ToList();
            _config.Schedule.EnableAutomaticReports = chkEnableReports.Checked;
            
            // Save frequency settings
            switch (cmbReportFrequency.SelectedIndex)
            {
                case 0: // Daily
                    _config.Schedule.Frequency = ReportFrequency.Daily;
                    break;
                case 1: // Weekly
                    _config.Schedule.Frequency = ReportFrequency.Weekly;
                    break;
                case 2: // Monthly
                    _config.Schedule.Frequency = ReportFrequency.Monthly;
                    break;
                case 3: // Custom
                    _config.Schedule.Frequency = ReportFrequency.Custom;
                    _config.Schedule.CustomDays = (int)numCustomDays.Value;
                    break;
            }
            
            _config.Schedule.ReportTime = TimeSpan.FromHours(cmbReportTime.SelectedIndex);
            
            _config.Storage.RetentionPeriod = TimeSpan.FromDays((double)numRetentionDays.Value);
            _config.Storage.MaxFolderSizeBytes = (long)(numMaxSizeGB.Value * 1024 * 1024 * 1024);
            _config.Storage.AutoCleanup = chkAutoCleanup.Checked;
            
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

    private async void BtnTest_Click(object? sender, EventArgs e)
    {
        try
        {
            btnTest.Enabled = false;
            btnTest.Text = "Probando...";
            
            // Validate required fields
            if (string.IsNullOrEmpty(txtSmtpServer.Text) || string.IsNullOrEmpty(txtUsername.Text) || 
                string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Complete los campos de SMTP, usuario y contraseña antes de probar.", 
                    "Campos requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Test SMTP connection
            await TestSmtpConnection(txtSmtpServer.Text, (int)numSmtpPort.Value, 
                txtUsername.Text, txtPassword.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error probando conexión: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTest.Enabled = true;
            btnTest.Text = "Probar Email";
        }
    }
    
    private async Task TestSmtpConnection(string server, int port, string username, string password)
    {
        try
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(server, port, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
            await client.AuthenticateAsync(username, password);
            await client.DisconnectAsync(true);
            
            MessageBox.Show("✅ Conexión SMTP exitosa!", "Prueba exitosa", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (MailKit.Security.AuthenticationException)
        {
            MessageBox.Show("❌ Error de autenticación. Verifique usuario y contraseña.", 
                "Error de autenticación", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (System.Net.Sockets.SocketException)
        {
            MessageBox.Show("❌ No se pudo conectar al servidor SMTP. Verifique servidor y puerto.", 
                "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Error de conexión: {ex.Message}", 
                "Error SMTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
    
    private void CmbReportFrequency_SelectedIndexChanged(object? sender, EventArgs e)
    {
        bool showCustomDays = cmbReportFrequency.SelectedIndex == 3; // Custom
        lblCustomDays.Visible = showCustomDays;
        numCustomDays.Visible = showCustomDays;
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