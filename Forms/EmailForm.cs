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
    private CheckedListBox clbQuadrants;
    private Label lblQuadrantNote;
    private GroupBox groupQuadrants;
    private bool _quadrantsAvailable = false;

    public EmailForm(IEmailService emailService, IFileService fileService, IConfigurationManager configManager)
    {
        _emailService = emailService;
        _fileService = fileService;
        _configManager = configManager;
        _config = new CapturerConfiguration();
        
        InitializeComponent();
        LoadConfigurationAsync();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(600, 650);
        this.Text = "Enviar Screenshots por Email - Capturer";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;
        this.MinimumSize = new Size(600, 650);
        this.MaximumSize = new Size(800, 900);
        
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

        // Create main scrollable panel
        var mainPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(this.ClientSize.Width, this.ClientSize.Height),
            AutoScroll = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        this.Controls.Add(mainPanel);

        var y = 20;

        // Date range section
        var groupDate = new GroupBox { Text = "Rango de Fechas", Location = new Point(20, y), Size = new Size(540, 80), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        
        groupDate.Controls.Add(new Label { Text = "Desde:", Location = new Point(15, 25), AutoSize = true });
        dtpFromDate = new DateTimePicker { Location = new Point(60, 22), Width = 120, Value = DateTime.Now.AddDays(-7) };
        groupDate.Controls.Add(dtpFromDate);
        
        groupDate.Controls.Add(new Label { Text = "Hasta:", Location = new Point(200, 25), AutoSize = true });
        dtpToDate = new DateTimePicker { Location = new Point(245, 22), Width = 120, Value = DateTime.Now };
        groupDate.Controls.Add(dtpToDate);
        
        var btnToday = new Button { Text = "Hoy", Location = new Point(15, 50), Size = new Size(60, 23) };
        var btnLast7Days = new Button { Text = "Últimos 7 días", Location = new Point(85, 50), Size = new Size(90, 23) };
        var btnLast30Days = new Button { Text = "Últimos 30 días", Location = new Point(185, 50), Size = new Size(90, 23) };
        var btnThisMonth = new Button { Text = "Este mes", Location = new Point(285, 50), Size = new Size(90, 23) };
        
        btnToday.Click += (s, e) => { dtpFromDate.Value = DateTime.Now.Date; dtpToDate.Value = DateTime.Now.Date; UpdatePreview(); };
        btnLast7Days.Click += (s, e) => { dtpFromDate.Value = DateTime.Now.AddDays(-7); dtpToDate.Value = DateTime.Now; UpdatePreview(); };
        btnLast30Days.Click += (s, e) => { dtpFromDate.Value = DateTime.Now.AddDays(-30); dtpToDate.Value = DateTime.Now; UpdatePreview(); };
        btnThisMonth.Click += (s, e) => { dtpFromDate.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); dtpToDate.Value = DateTime.Now; UpdatePreview(); };
        
        groupDate.Controls.AddRange(new Control[] { btnToday, btnLast7Days, btnLast30Days, btnThisMonth });
        mainPanel.Controls.Add(groupDate);
        y += 100;

        // Recipients section
        var groupRecipients = new GroupBox { Text = "Destinatarios", Location = new Point(20, y), Size = new Size(540, 120), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        
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
            Size = new Size(540, 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false
        };
        
        chkUseQuadrants = new CheckBox 
        { 
            Text = "Usar sistema de cuadrantes", 
            Location = new Point(15, 25), 
            AutoSize = true 
        };
        chkUseQuadrants.CheckedChanged += ChkUseQuadrants_CheckedChanged;
        
        clbQuadrants = new CheckedListBox 
        { 
            Location = new Point(15, 50), 
            Size = new Size(500, 40),
            CheckOnClick = true,
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        clbQuadrants.ItemCheck += (s, e) => 
        {
            // Use BeginInvoke to ensure the checkbox state is updated before UpdatePreview
            this.BeginInvoke(new Action(async () => await UpdatePreview()));
        };
        
        lblQuadrantNote = new Label 
        { 
            Text = "Seleccione los cuadrantes que desea incluir en el reporte", 
            Location = new Point(15, 95), 
            Size = new Size(500, 15), 
            Font = new Font("Microsoft Sans Serif", 7.5F),
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        
        groupQuadrants.Controls.AddRange(new Control[] { chkUseQuadrants, clbQuadrants, lblQuadrantNote });
        mainPanel.Controls.Add(groupQuadrants);
        y += 140;

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
                
                clbQuadrants.Items.Clear();
                foreach (var quadrant in availableQuadrants)
                {
                    clbQuadrants.Items.Add(quadrant, false);
                }
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
            clbQuadrants.Enabled = true;
            lblQuadrantNote.Text = "Seleccione los cuadrantes que desea incluir en el reporte";
        }
        else
        {
            clbQuadrants.Enabled = false;
            lblQuadrantNote.Text = "Sistema de cuadrantes deshabilitado - se procesarán screenshots normales";
            
            // Uncheck all quadrants when disabled
            for (int i = 0; i < clbQuadrants.Items.Count; i++)
            {
                clbQuadrants.SetItemChecked(i, false);
            }
        }
        
        UpdatePreview();
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
            if (chkUseQuadrants.Checked && clbQuadrants.CheckedItems.Count > 0)
            {
                var selectedQuadrants = clbQuadrants.CheckedItems.Cast<string>().ToList();
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
        
        if (chkUseQuadrants.Checked && clbQuadrants.CheckedItems.Count > 0)
        {
            var selectedQuadrants = clbQuadrants.CheckedItems.Cast<string>().ToList();
            contentMsg = $"cuadrantes seleccionados: {string.Join(", ", selectedQuadrants)}";
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
            if (chkUseQuadrants.Checked && clbQuadrants.CheckedItems.Count > 0)
            {
                var selectedQuadrants = clbQuadrants.CheckedItems.Cast<string>().ToList();
                success = await _emailService.SendQuadrantReportAsync(recipients, startDate, endDate, selectedQuadrants, useZipFormat);
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

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}