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
        this.Size = new Size(550, 590);
        this.Text = "Enviar Screenshots por Email - Capturer";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var y = 20;

        // Date range section
        var groupDate = new GroupBox { Text = "Rango de Fechas", Location = new Point(20, y), Size = new Size(500, 80) };
        
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
        this.Controls.Add(groupDate);
        y += 100;

        // Recipients section
        var groupRecipients = new GroupBox { Text = "Destinatarios", Location = new Point(20, y), Size = new Size(500, 120) };
        
        clbRecipients = new CheckedListBox { Location = new Point(15, 25), Size = new Size(420, 60) };
        groupRecipients.Controls.Add(clbRecipients);
        
        groupRecipients.Controls.Add(new Label { Text = "Email personalizado:", Location = new Point(15, 90), AutoSize = true });
        txtCustomEmail = new TextBox { Location = new Point(130, 87), Width = 200 };
        btnAddCustom = new Button { Text = "Agregar", Location = new Point(340, 86), Size = new Size(70, 23) };
        btnAddCustom.Click += BtnAddCustom_Click;
        
        groupRecipients.Controls.AddRange(new Control[] { txtCustomEmail, btnAddCustom });
        this.Controls.Add(groupRecipients);
        y += 140;

        // Attachment format section
        var groupFormat = new GroupBox { Text = "Formato de Adjuntos", Location = new Point(20, y), Size = new Size(500, 70) };
        
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
            Size = new Size(420, 15), 
            Font = new Font("Microsoft Sans Serif", 7.5F),
            ForeColor = Color.DarkBlue
        };
        
        rbZipFormat.CheckedChanged += (s, e) => UpdatePreview();
        rbIndividualFiles.CheckedChanged += (s, e) => UpdatePreview();
        
        groupFormat.Controls.AddRange(new Control[] { rbZipFormat, rbIndividualFiles, lblFormatNote });
        this.Controls.Add(groupFormat);
        y += 90;

        // Preview section
        var groupPreview = new GroupBox { Text = "Vista Previa", Location = new Point(20, y), Size = new Size(500, 80) };
        
        lblPreview = new Label { Location = new Point(15, 25), Size = new Size(420, 20), Text = "0 screenshots encontrados" };
        lblEstimatedSize = new Label { Location = new Point(15, 45), Size = new Size(420, 20), Text = "Tamaño estimado: 0 MB" };
        
        groupPreview.Controls.AddRange(new Control[] { lblPreview, lblEstimatedSize });
        this.Controls.Add(groupPreview);
        y += 100;

        // Progress bar (initially hidden)
        progressBar = new ProgressBar { Location = new Point(20, y), Size = new Size(500, 20), Visible = false, Style = ProgressBarStyle.Continuous };
        lblProgress = new Label { Location = new Point(20, y + 25), Size = new Size(500, 20), Text = "", Visible = false, TextAlign = ContentAlignment.MiddleCenter };
        this.Controls.AddRange(new Control[] { progressBar, lblProgress });
        y += 50;
        
        // Bottom buttons
        btnSend = new Button { Text = "Enviar Ahora", Location = new Point(270, y), Size = new Size(100, 30) };
        btnCancel = new Button { Text = "Cancelar", Location = new Point(380, y), Size = new Size(80, 30) };
        btnPreview = new Button { Text = "Vista Previa", Location = new Point(160, y), Size = new Size(100, 30) };
        
        btnSend.Click += BtnSend_Click;
        btnCancel.Click += BtnCancel_Click;
        btnPreview.Click += BtnPreview_Click;
        
        this.Controls.AddRange(new Control[] { btnSend, btnCancel, btnPreview });

        // Wire up events
        dtpFromDate.ValueChanged += (s, e) => UpdatePreview();
        dtpToDate.ValueChanged += (s, e) => UpdatePreview();
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
            
            await UpdatePreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        var result = MessageBox.Show(
            $"¿Enviar email con screenshots desde {dtpFromDate.Value:yyyy-MM-dd} hasta {dtpToDate.Value:yyyy-MM-dd} {formatMsg} a {clbRecipients.CheckedItems.Count} destinatarios?\n\nFormato: {(rbZipFormat.Checked ? "Archivo ZIP comprimido" : "Imágenes individuales")}",
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
            
            var success = await _emailService.SendManualReportAsync(recipients, startDate, endDate, useZipFormat);
            
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