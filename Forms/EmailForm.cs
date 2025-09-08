using Capturer.Models;
using Capturer.Services;

namespace Capturer.Forms;

public partial class EmailForm : Form
{
    private readonly IEmailService _emailService;
    private readonly IFileService _fileService;
    private readonly IConfigurationManager _configManager;
    private CapturerConfiguration _config;
    
    // Controls
    private DateTimePicker dtpFromDate;
    private DateTimePicker dtpToDate;
    private CheckedListBox clbRecipients;
    private TextBox txtCustomEmail;
    private Button btnAddCustom;
    private Label lblPreview;
    private Label lblEstimatedSize;
    private RadioButton rbZipFormat;
    private RadioButton rbIndividualFiles;
    private Label lblFormatNote;
    private Button btnSend;
    private Button btnCancel;
    private Button btnPreview;
    private ProgressBar progressBar;
    private Label lblProgress;
    
    // New Quadrant Controls
    private CheckBox chkUseQuadrants;
    private Button btnSelectQuadrants;
    private Label lblSelectedQuadrants;
    private Label lblQuadrantNote;
    private GroupBox groupQuadrants;
    private CheckBox chkSeparateEmailPerQuadrant;
    private Label lblQuadrantProcessingNote;
    private CheckBox chkProcessQuadrantsFirst;
    private ComboBox cmbQuadrantProfile;
    private Label lblQuadrantProfile;
    private List<string> _selectedQuadrantsList = new();
    private readonly IQuadrantService? _quadrantService;
    private ToolTip _helpTooltip;
    private bool _quadrantsAvailable = false;
    
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

    public EmailForm(IEmailService emailService, IFileService fileService, IConfigurationManager configManager, IQuadrantService? quadrantService = null)
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
        this.Size = new Size(650, 700);
        this.Text = "📧 Envío Manual de Capturas - Capturer v3.1.2";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;
        this.MinimumSize = new Size(650, 700);
        this.MaximumSize = new Size(900, 1000);
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
            Padding = new Padding(10),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        this.Controls.Add(mainPanel);

        var y = 20;

        // Date range section with modern styling
        var groupDate = new GroupBox 
        { 
            Text = "📅 Rango de Fechas", 
            Location = new Point(20, y), 
            Size = new Size(590, 90), 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(52, 58, 64),
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(10)
        };
        
        // Add help button for date range
        var helpDateRange = CreateHelpButton(new Point(505, -2), 
            "Rango de Fechas: Seleccione el período de tiempo para incluir las capturas de pantalla.\n\n" +
            "• Use los botones rápidos para períodos comunes\n" +
            "• O seleccione fechas específicas manualmente\n" +
            "• Solo se incluirán las capturas dentro de este rango");
        groupDate.Controls.Add(helpDateRange);
        
        var lblFrom = new Label { Text = "Desde:", Location = new Point(20, 30), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point) };
        groupDate.Controls.Add(lblFrom);
        dtpFromDate = new DateTimePicker { Location = new Point(70, 27), Width = 130, Value = DateTime.Now.AddDays(-7), Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point) };
        groupDate.Controls.Add(dtpFromDate);
        
        var lblTo = new Label { Text = "Hasta:", Location = new Point(220, 30), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point) };
        groupDate.Controls.Add(lblTo);
        dtpToDate = new DateTimePicker { Location = new Point(270, 27), Width = 130, Value = DateTime.Now, Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point) };
        groupDate.Controls.Add(dtpToDate);
        
        var btnToday = CreateModernButton("Hoy", new Point(20, 55), new Size(70, 28), Color.FromArgb(13, 110, 253));
        var btnLast7Days = CreateModernButton("7 días", new Point(100, 55), new Size(70, 28), Color.FromArgb(25, 135, 84));
        var btnLast30Days = CreateModernButton("30 días", new Point(180, 55), new Size(70, 28), Color.FromArgb(111, 66, 193));
        var btnThisMonth = CreateModernButton("Este mes", new Point(260, 55), new Size(80, 28), Color.FromArgb(220, 124, 95));
        
        btnToday.Click += (s, e) => { dtpFromDate.Value = DateTime.Now.Date; dtpToDate.Value = DateTime.Now.Date; UpdatePreview(); };
        btnLast7Days.Click += (s, e) => { dtpFromDate.Value = DateTime.Now.AddDays(-7); dtpToDate.Value = DateTime.Now; UpdatePreview(); };
        btnLast30Days.Click += (s, e) => { dtpFromDate.Value = DateTime.Now.AddDays(-30); dtpToDate.Value = DateTime.Now; UpdatePreview(); };
        btnThisMonth.Click += (s, e) => { dtpFromDate.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); dtpToDate.Value = DateTime.Now; UpdatePreview(); };
        
        groupDate.Controls.AddRange(new Control[] { btnToday, btnLast7Days, btnLast30Days, btnThisMonth });
        mainPanel.Controls.Add(groupDate);
        y += 100;

        // Recipients section
        var groupRecipients = new GroupBox { Text = "Destinatarios", Location = new Point(20, y), Size = new Size(540, 120), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        
        // Add help button for recipients
        var helpRecipients = CreateHelpButton(new Point(505, -2), 
            "Destinatarios: Configure a quién se enviarán los emails con las capturas.\n\n" +
            "• Marque los emails ya configurados que desea incluir\n" +
            "• Agregue emails personalizados temporalmente\n" +
            "• Debe seleccionar al menos un destinatario");
        groupRecipients.Controls.Add(helpRecipients);
        
        clbRecipients = new CheckedListBox { Location = new Point(15, 25), Size = new Size(460, 60), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        groupRecipients.Controls.Add(clbRecipients);
        
        groupRecipients.Controls.Add(new Label { Text = "Email personalizado:", Location = new Point(15, 90), AutoSize = true });
        txtCustomEmail = new TextBox { Location = new Point(130, 87), Width = 240, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        btnAddCustom = new Button { Text = "Agregar", Location = new Point(380, 86), Size = new Size(70, 23), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        btnAddCustom.Click += BtnAddCustom_Click;
        
        groupRecipients.Controls.AddRange(new Control[] { txtCustomEmail, btnAddCustom });
        mainPanel.Controls.Add(groupRecipients);
        y += 140;

        // Attachment format section
        var groupFormat = new GroupBox { Text = "Formato de Adjuntos", Location = new Point(20, y), Size = new Size(540, 70), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        
        // Add help button for format
        var helpFormat = CreateHelpButton(new Point(505, -2), 
            "Formato de Adjuntos: Seleccione cómo se enviarán las imágenes.\n\n" +
            "• ZIP: Recomendado para múltiples archivos (más eficiente)\n" +
            "• Individual: Cada imagen como adjunto separado\n" +
            "• Los emails tienen límite de 25MB por adjunto");
        groupFormat.Controls.Add(helpFormat);
        
        rbZipFormat = new RadioButton 
        { 
            Text = "Archivo ZIP (Recomendado)", 
            Location = new Point(15, 25), 
            AutoSize = true, 
            Checked = true 
        };
        rbIndividualFiles = new RadioButton 
        { 
            Text = "Imágenes individuales", 
            Location = new Point(200, 25), 
            AutoSize = true 
        };
        lblFormatNote = new Label 
        { 
            Text = "Nota: Imágenes individuales pueden tener limitación de tamaño por email", 
            Location = new Point(15, 45), 
            Size = new Size(460, 15), 
            Font = new Font("Microsoft Sans Serif", 7.5F),
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        
        rbZipFormat.CheckedChanged += (s, e) => UpdatePreview();
        rbIndividualFiles.CheckedChanged += (s, e) => UpdatePreview();
        
        groupFormat.Controls.AddRange(new Control[] { rbZipFormat, rbIndividualFiles, lblFormatNote });
        mainPanel.Controls.Add(groupFormat);
        y += 90;

        // Preview section
        var groupPreview = new GroupBox { Text = "Vista Previa", Location = new Point(20, y), Size = new Size(540, 80), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        
        lblPreview = new Label { Location = new Point(15, 25), Size = new Size(460, 20), Text = "0 screenshots encontrados", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        lblEstimatedSize = new Label { Location = new Point(15, 45), Size = new Size(460, 20), Text = "Tamaño estimado: 0 MB", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        
        groupPreview.Controls.AddRange(new Control[] { lblPreview, lblEstimatedSize });
        mainPanel.Controls.Add(groupPreview);
        y += 100;

        // Progress bar (initially hidden)
        progressBar = new ProgressBar { Location = new Point(20, y), Size = new Size(540, 20), Visible = false, Style = ProgressBarStyle.Continuous, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        lblProgress = new Label { Location = new Point(20, y + 25), Size = new Size(540, 20), Text = "", Visible = false, TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        mainPanel.Controls.AddRange(new Control[] { progressBar, lblProgress });
        y += 50;
        
        // Bottom buttons
        btnSend = new Button { Text = "Enviar Ahora", Location = new Point(310, y), Size = new Size(100, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        btnCancel = new Button { Text = "Cancelar", Location = new Point(420, y), Size = new Size(80, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        btnPreview = new Button { Text = "Vista Previa", Location = new Point(200, y), Size = new Size(100, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        
        btnSend.Click += BtnSend_Click;
        btnCancel.Click += BtnCancel_Click;
        btnPreview.Click += BtnPreview_Click;
        
        mainPanel.Controls.AddRange(new Control[] { btnSend, btnCancel, btnPreview });

        // Quadrant Group (positioned after format section)
        groupQuadrants = new GroupBox 
        { 
            Text = "Procesamiento por Cuadrantes", 
            Location = new Point(20, y), 
            Size = new Size(540, 180),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false
        };
        
        // Add help button for quadrants
        var helpQuadrants = CreateHelpButton(new Point(505, -2), 
            "Sistema de Cuadrantes: Funcionalidad avanzada para procesar secciones específicas.\n\n" +
            "• Usar cuadrantes: Incluye cuadrantes en lugar de screenshots normales\n" +
            "• Procesar antes del envío: Regenera automáticamente con el perfil seleccionado\n" +
            "• Emails separados: Envía un email individual por cada cuadrante\n" +
            "\nNota: Solo aparece si hay cuadrantes disponibles en el sistema");
        groupQuadrants.Controls.Add(helpQuadrants);
        
        chkUseQuadrants = new CheckBox 
        { 
            Text = "Usar sistema de cuadrantes", 
            Location = new Point(15, 25), 
            AutoSize = true 
        };
        chkUseQuadrants.CheckedChanged += ChkUseQuadrants_CheckedChanged;
        
        btnSelectQuadrants = CreateModernButton("🔲 Seleccionar Cuadrantes...", new Point(15, 50), new Size(200, 30), Color.FromArgb(0, 123, 255));
        btnSelectQuadrants.Enabled = false;
        btnSelectQuadrants.Click += BtnSelectQuadrants_Click;
        
        lblSelectedQuadrants = new Label
        {
            Text = "Ningún cuadrante seleccionado",
            Location = new Point(220, 55),
            Size = new Size(280, 20),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
        };
        
        chkProcessQuadrantsFirst = new CheckBox 
        { 
            Text = "Procesar cuadrantes antes del envío (regenerar automáticamente)", 
            Location = new Point(15, 95), 
            AutoSize = true,
            Enabled = false,
            Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold),
            ForeColor = Color.DarkGreen
        };
        chkProcessQuadrantsFirst.CheckedChanged += ChkProcessQuadrantsFirst_CheckedChanged;
        
        // Quadrant profile selection (initially hidden)
        lblQuadrantProfile = new Label 
        { 
            Text = "Perfil de procesamiento:", 
            Location = new Point(35, 120), 
            AutoSize = true,
            Visible = false,
            Font = new Font("Microsoft Sans Serif", 8.25F)
        };
        
        cmbQuadrantProfile = new ComboBox 
        { 
            Location = new Point(180, 117), 
            Width = 200, 
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false,
            Visible = false
        };
        
        chkSeparateEmailPerQuadrant = new CheckBox 
        { 
            Text = "Enviar email separado por cada cuadrante", 
            Location = new Point(15, 145), 
            AutoSize = true,
            Enabled = false,
            Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };
        chkSeparateEmailPerQuadrant.CheckedChanged += (s, e) => { UpdateQuadrantSeparateEmailVisibility(); UpdatePreview(); };
        
        lblQuadrantNote = new Label 
        { 
            Text = "Seleccione los cuadrantes que desea incluir en el reporte", 
            Location = new Point(15, 165), 
            Size = new Size(500, 15), 
            Font = new Font("Microsoft Sans Serif", 7.5F),
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        
        lblQuadrantProcessingNote = new Label 
        { 
            Text = "Con emails separados, cada cuadrante se enviará en un email individual", 
            Location = new Point(35, 167), 
            Size = new Size(480, 15), 
            Font = new Font("Microsoft Sans Serif", 7.5F),
            ForeColor = Color.DarkGreen,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false
        };
        
        groupQuadrants.Controls.AddRange(new Control[] { chkUseQuadrants, btnSelectQuadrants, lblSelectedQuadrants, chkProcessQuadrantsFirst, lblQuadrantProfile, cmbQuadrantProfile, chkSeparateEmailPerQuadrant, lblQuadrantNote, lblQuadrantProcessingNote });
        mainPanel.Controls.Add(groupQuadrants);
        y += 200;

        // Wire up events
        dtpFromDate.ValueChanged += (s, e) => UpdatePreview();
        dtpToDate.ValueChanged += (s, e) => UpdatePreview();
        
        // Set up proper scrolling
        mainPanel.AutoScrollMinSize = new Size(540, y + 60);
        
        // Handle form resize events
        this.Resize += (s, e) => 
        {
            if (mainPanel != null)
            {
                mainPanel.Size = new Size(this.ClientSize.Width, this.ClientSize.Height);
                mainPanel.AutoScrollMinSize = new Size(Math.Max(540, this.ClientSize.Width - 40), y + 60);
            }
        };
    }

    private async void LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
            
            // Load recipients
            clbRecipients.Items.Clear();
            foreach (var recipient in _config.Email.Recipients)
            {
                clbRecipients.Items.Add(recipient, true);
            }
            
            LoadAvailableQuadrants();
            await UpdatePreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadAvailableQuadrants()
    {
        try
        {
            var availableQuadrants = _emailService.GetAvailableQuadrantFolders();
            
            if (availableQuadrants.Any())
            {
                _quadrantsAvailable = true;
                groupQuadrants.Visible = true;
            }
            else
            {
                _quadrantsAvailable = false;
                groupQuadrants.Visible = false;
            }
        }
        catch (Exception ex)
        {
            _quadrantsAvailable = false;
            groupQuadrants.Visible = false;
            Console.WriteLine($"Error loading quadrants: {ex.Message}");
        }
    }

    private void ChkUseQuadrants_CheckedChanged(object? sender, EventArgs e)
    {
        if (chkUseQuadrants.Checked)
        {
            btnSelectQuadrants.Enabled = true;
            chkProcessQuadrantsFirst.Enabled = true;
            chkSeparateEmailPerQuadrant.Enabled = true;
            lblQuadrantNote.Text = "Seleccione los cuadrantes que desea incluir en el reporte";
        }
        else
        {
            btnSelectQuadrants.Enabled = false;
            chkProcessQuadrantsFirst.Enabled = false;
            chkProcessQuadrantsFirst.Checked = false;
            chkSeparateEmailPerQuadrant.Enabled = false;
            chkSeparateEmailPerQuadrant.Checked = false;
            lblQuadrantNote.Text = "Sistema de cuadrantes deshabilitado - se procesarán screenshots normales";
            
            // Clear selected quadrants when disabled
            _selectedQuadrantsList.Clear();
            UpdateSelectedQuadrantsLabel();
        }
        
        UpdateQuadrantProcessingVisibility();
        UpdateQuadrantSeparateEmailVisibility();
        UpdatePreview();
    }
    
    private void ChkProcessQuadrantsFirst_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateQuadrantProcessingVisibility();
    }
    
    private void UpdateQuadrantProcessingVisibility()
    {
        // Profile selection is always available when quadrants are enabled (like RoutineEmailForm)
        if (chkUseQuadrants.Checked)
        {
            lblQuadrantProfile.Visible = true;
            cmbQuadrantProfile.Visible = true;
            cmbQuadrantProfile.Enabled = true;
            LoadQuadrantProfiles();
            
            // ProcessQuadrantsFirst checkbox only affects processing order, not profile visibility
            chkProcessQuadrantsFirst.Visible = true;
        }
        else
        {
            lblQuadrantProfile.Visible = false;
            cmbQuadrantProfile.Visible = false;
            cmbQuadrantProfile.Enabled = false;
            chkProcessQuadrantsFirst.Visible = false;
        }
    }
    
    private void LoadQuadrantProfiles()
    {
        try
        {
            cmbQuadrantProfile.Items.Clear();
            
            if (_quadrantService != null)
            {
                // Load actual quadrant configurations (real profiles)
                var configurations = _quadrantService.GetConfigurations();
                
                if (configurations.Any())
                {
                    foreach (var config in configurations.Where(c => c.IsActive))
                    {
                        cmbQuadrantProfile.Items.Add(config.Name);
                    }
                    
                    // Select the active configuration or first one
                    var activeConfig = _quadrantService.GetActiveConfiguration();
                    if (activeConfig != null)
                    {
                        var activeIndex = cmbQuadrantProfile.Items.Cast<string>()
                            .ToList().IndexOf(activeConfig.Name);
                        if (activeIndex >= 0)
                            cmbQuadrantProfile.SelectedIndex = activeIndex;
                    }
                }
                
                // Fallback if no configurations found
                if (cmbQuadrantProfile.Items.Count == 0)
                {
                    cmbQuadrantProfile.Items.Add("Default");
                }
            }
            else
            {
                // Fallback if no quadrant service
                cmbQuadrantProfile.Items.Add("Default");
            }
            
            if (cmbQuadrantProfile.Items.Count > 0 && cmbQuadrantProfile.SelectedIndex < 0)
            {
                cmbQuadrantProfile.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading quadrant profiles: {ex.Message}");
            cmbQuadrantProfile.Items.Clear();
            cmbQuadrantProfile.Items.Add("Default");
            cmbQuadrantProfile.SelectedIndex = 0;
        }
    }
    
    private void UpdateQuadrantSeparateEmailVisibility()
    {
        if (chkSeparateEmailPerQuadrant.Checked && chkUseQuadrants.Checked)
        {
            lblQuadrantProcessingNote.Visible = true;
            lblQuadrantNote.Text = "Seleccione cuadrantes - cada uno se enviará por separado";
        }
        else
        {
            lblQuadrantProcessingNote.Visible = false;
            if (chkUseQuadrants.Checked)
            {
                lblQuadrantNote.Text = "Seleccione los cuadrantes que desea incluir en el reporte";
            }
        }
    }

    private void BtnAddCustom_Click(object? sender, EventArgs e)
    {
        var email = txtCustomEmail.Text.Trim();
        if (!string.IsNullOrEmpty(email) && email.Contains("@"))
        {
            clbRecipients.Items.Add(email, true);
            txtCustomEmail.Clear();
        }
        else
        {
            MessageBox.Show("Ingrese un email válido", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task UpdatePreview()
    {
        try
        {
            var startDate = dtpFromDate.Value.Date;
            var endDate = dtpToDate.Value.Date.AddDays(1).AddSeconds(-1);
            
            // Check if we're using quadrants and have some selected
            if (chkUseQuadrants.Checked && _selectedQuadrantsList.Count > 0)
            {
                var selectedQuadrants = _selectedQuadrantsList;
                var quadrantFiles = new List<string>();
                long totalSize = 0;
                
                // Get files from selected quadrants for the date range
                foreach (var quadrant in selectedQuadrants)
                {
                    try
                    {
                        var quadrantPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "Capturer", "Quadrants", quadrant);
                            
                        if (Directory.Exists(quadrantPath))
                        {
                            var files = Directory.GetFiles(quadrantPath, "*.*", SearchOption.AllDirectories)
                                .Where(f => 
                                {
                                    var fileInfo = new FileInfo(f);
                                    return fileInfo.CreationTime >= startDate && fileInfo.CreationTime <= endDate;
                                })
                                .ToList();
                                
                            quadrantFiles.AddRange(files);
                            totalSize += files.Sum(f => new FileInfo(f).Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error accessing quadrant {quadrant}: {ex.Message}");
                    }
                }
                
                lblPreview.Text = $"{quadrantFiles.Count} archivos de cuadrante encontrados ({selectedQuadrants.Count} cuadrantes)";
                string formatText = rbZipFormat.Checked ? "ZIP" : "archivos individuales";
                lblEstimatedSize.Text = $"Tamaño estimado ({formatText}): {FileService.FormatFileSize(totalSize)}";
                
                btnSend.Enabled = quadrantFiles.Count > 0 && clbRecipients.CheckedItems.Count > 0;
            }
            else
            {
                // Standard screenshot processing
                var screenshots = await _fileService.GetScreenshotsByDateRangeAsync(startDate, endDate);
                
                lblPreview.Text = $"{screenshots.Count} screenshots encontrados";
                
                long totalSize = 0;
                foreach (var path in screenshots)
                {
                    if (File.Exists(path))
                    {
                        totalSize += new FileInfo(path).Length;
                    }
                }
                
                string formatText = rbZipFormat.Checked ? "ZIP" : "archivos individuales";
                lblEstimatedSize.Text = $"Tamaño estimado ({formatText}): {FileService.FormatFileSize(totalSize)}";
                
                btnSend.Enabled = screenshots.Count > 0 && clbRecipients.CheckedItems.Count > 0;
            }
        }
        catch (Exception ex)
        {
            lblPreview.Text = "Error calculando vista previa";
            lblEstimatedSize.Text = ex.Message;
        }
    }

    private void BtnPreview_Click(object? sender, EventArgs e)
    {
        try
        {
            var directoryInfo = _fileService.GetScreenshotDirectoryInfoAsync().Result;
            System.Diagnostics.Process.Start("explorer.exe", directoryInfo.FullName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error abriendo carpeta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnSend_Click(object? sender, EventArgs e)
    {
        if (clbRecipients.CheckedItems.Count == 0)
        {
            MessageBox.Show("Seleccione al menos un destinatario", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string formatMsg = rbZipFormat.Checked ? "en formato ZIP" : "como archivos individuales";
        string contentMsg = "";
        
        if (chkUseQuadrants.Checked && _selectedQuadrantsList.Count > 0)
        {
            var selectedQuadrants = _selectedQuadrantsList;
            if (chkSeparateEmailPerQuadrant.Checked)
            {
                contentMsg = $"{selectedQuadrants.Count} cuadrantes (email separado por cada uno): {string.Join(", ", selectedQuadrants)}";
            }
            else
            {
                contentMsg = $"cuadrantes combinados: {string.Join(", ", selectedQuadrants)}";
            }
        }
        else
        {
            contentMsg = "screenshots normales";
        }
        
        var result = MessageBox.Show(
            $"¿Enviar email con {contentMsg} desde {dtpFromDate.Value:yyyy-MM-dd} hasta {dtpToDate.Value:yyyy-MM-dd} {formatMsg} a {clbRecipients.CheckedItems.Count} destinatarios?\n\nFormato: {(rbZipFormat.Checked ? "Archivo ZIP comprimido" : "Imágenes individuales")}",
            "Confirmar envío",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        try
        {
            btnSend.Enabled = false;
            btnSend.Text = "Enviando...";
            
            // Show progress bar
            progressBar.Visible = true;
            lblProgress.Visible = true;
            progressBar.Value = 0;
            lblProgress.Text = "Preparando envío...";
            
            // Update progress: preparing
            progressBar.Value = 20;
            lblProgress.Text = "Obteniendo screenshots...";
            await Task.Delay(100); // Small delay for UI update

            var recipients = clbRecipients.CheckedItems.Cast<string>().ToList();
            var startDate = dtpFromDate.Value.Date;
            var endDate = dtpToDate.Value.Date.AddDays(1).AddSeconds(-1);

            // Update progress: processing
            progressBar.Value = 40;
            lblProgress.Text = "Procesando archivos...";
            await Task.Delay(100);
            
            bool useZipFormat = rbZipFormat.Checked;
            
            // Update progress: sending
            progressBar.Value = 60;
            lblProgress.Text = "Enviando email...";
            await Task.Delay(100);
            
            bool success;
            
            // Check if we're using quadrants
            if (chkUseQuadrants.Checked && _selectedQuadrantsList.Count > 0)
            {
                var selectedQuadrants = _selectedQuadrantsList;
                
                if (chkSeparateEmailPerQuadrant.Checked)
                {
                    // Send separate email for each quadrant
                    success = true;
                    var totalQuadrants = selectedQuadrants.Count;
                    var currentQuadrant = 0;
                    
                    foreach (var quadrant in selectedQuadrants)
                    {
                        currentQuadrant++;
                        progressBar.Value = 60 + (30 * currentQuadrant / totalQuadrants);
                        lblProgress.Text = $"Enviando email {currentQuadrant}/{totalQuadrants} (cuadrante: {quadrant})...";
                        await Task.Delay(100);
                        
                        var quadrantSuccess = await _emailService.SendQuadrantReportAsync(
                            recipients, startDate, endDate, new List<string> { quadrant }, useZipFormat);
                        
                        if (!quadrantSuccess)
                        {
                            success = false;
                            MessageBox.Show($"Error enviando email para cuadrante '{quadrant}'. Los demás emails podrían haberse enviado correctamente.", 
                                "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                else
                {
                    // Send combined email with all quadrants
                    success = await _emailService.SendQuadrantReportAsync(recipients, startDate, endDate, selectedQuadrants, useZipFormat);
                }
            }
            else
            {
                success = await _emailService.SendManualReportAsync(recipients, startDate, endDate, useZipFormat);
            }
            
            // Update progress: completing
            progressBar.Value = 100;
            lblProgress.Text = success ? "Email enviado exitosamente!" : "Error al enviar email";
            await Task.Delay(500); // Show completion message briefly

            if (success)
            {
                MessageBox.Show("Email enviado exitosamente", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Error enviando email. Revise la configuración.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error enviando email: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSend.Enabled = true;
            btnSend.Text = "Enviar Ahora";
            
            // Hide progress bar after a delay
            await Task.Delay(1000);
            progressBar.Visible = false;
            lblProgress.Visible = false;
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
                UpdatePreview();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error abriendo selector de cuadrantes: {ex.Message}", "Error", 
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

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}