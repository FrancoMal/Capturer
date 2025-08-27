using Microsoft.Extensions.DependencyInjection;
using Capturer.Services;
using Capturer.Models;
using Capturer.Forms;
using System.Diagnostics;

namespace Capturer
{
    public partial class Form1 : Form
    {
        private readonly ISchedulerService _schedulerService;
        private readonly IScreenshotService _screenshotService;
        private readonly IEmailService _emailService;
        private readonly IFileService _fileService;
        private readonly IConfigurationManager _configManager;
        private readonly ServiceProvider _serviceProvider;
        private CapturerConfiguration _config = new();

        public Form1(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _schedulerService = serviceProvider.GetRequiredService<ISchedulerService>();
            _screenshotService = serviceProvider.GetRequiredService<IScreenshotService>();
            _emailService = serviceProvider.GetRequiredService<IEmailService>();
            _fileService = serviceProvider.GetRequiredService<IFileService>();
            _configManager = serviceProvider.GetRequiredService<IConfigurationManager>();

            InitializeComponent();
            InitializeApplication();
        }

        private async void InitializeApplication()
        {
            // Load configuration
            _config = await _configManager.LoadConfigurationAsync();

            // Setup ListView columns
            SetupListView();

            // Wire up event handlers
            WireUpEvents();

            // Setup system tray
            SetupSystemTray();

            // Start scheduler service
            await _schedulerService.StartAsync();

            // Update UI
            await UpdateUIAsync();

            // Start update timer
            updateTimer.Start();
        }

        private void SetupListView()
        {
            listViewScreenshots.View = View.Details;
            listViewScreenshots.FullRowSelect = true;
            listViewScreenshots.GridLines = true;
            
            listViewScreenshots.Columns.Add("Archivo", 200);
            listViewScreenshots.Columns.Add("Fecha", 120);
            listViewScreenshots.Columns.Add("Tamaño", 80);
            listViewScreenshots.Columns.Add("Acciones", 120);
        }

        private void WireUpEvents()
        {
            // Button events
            btnStartCapture.Click += BtnStartCapture_Click;
            btnStopCapture.Click += BtnStopCapture_Click;
            btnSettings.Click += BtnSettings_Click;
            btnSendEmail.Click += BtnSendEmail_Click;
            btnCaptureNow.Click += BtnCaptureNow_Click;
            btnOpenFolder.Click += BtnOpenFolder_Click;
            btnMinimizeToTray.Click += BtnMinimizeToTray_Click;
            btnExit.Click += BtnExit_Click;

            // Timer events
            updateTimer.Tick += UpdateTimer_Tick;

            // System tray events
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            showToolStripMenuItem.Click += ShowToolStripMenuItem_Click;
            captureToolStripMenuItem.Click += CaptureToolStripMenuItem_Click;
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;

            // Form events
            this.FormClosing += Form1_FormClosing;
            this.WindowState = FormWindowState.Normal;

            // ListView events
            listViewScreenshots.DoubleClick += ListView_DoubleClick;

            // Service events
            _screenshotService.ScreenshotCaptured += ScreenshotService_ScreenshotCaptured;
            _emailService.EmailSent += EmailService_EmailSent;
        }

        private void SetupSystemTray()
        {
            // Set icon for system tray (you'll need to add an icon resource)
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = "Capturer - Screenshot Manager";
            notifyIcon.Visible = true;
        }

        private async void BtnStartCapture_Click(object? sender, EventArgs e)
        {
            try
            {
                await _schedulerService.ScheduleScreenshotsAsync(_config.Screenshot.CaptureInterval);
                await UpdateUIAsync();
                ShowNotification("Captura iniciada", $"Tomando screenshots cada {_config.Screenshot.CaptureInterval.TotalMinutes} minutos");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar la captura: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnStopCapture_Click(object? sender, EventArgs e)
        {
            try
            {
                await _screenshotService.StopAutomaticCaptureAsync();
                await UpdateUIAsync();
                ShowNotification("Captura detenida", "Se ha detenido la captura automática de screenshots");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al detener la captura: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            try
            {
                var settingsForm = new SettingsForm(_configManager, _screenshotService);
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    // Reload configuration after changes
                    LoadConfigurationAsync();
                    ShowNotification("Configuración", "Configuración actualizada exitosamente");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LoadConfigurationAsync()
        {
            _config = await _configManager.LoadConfigurationAsync();
            
            // Restart scheduler service with new configuration if it's running
            if (_schedulerService.IsRunning)
            {
                await _schedulerService.StopAsync();
                await _schedulerService.StartAsync();
            }
        }

        private void BtnSendEmail_Click(object? sender, EventArgs e)
        {
            try
            {
                var emailForm = new EmailForm(_emailService, _fileService, _configManager);
                if (emailForm.ShowDialog(this) == DialogResult.OK)
                {
                    ShowNotification("Email", "Email enviado exitosamente");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo formulario de email: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void BtnMinimizeToTray_Click(object? sender, EventArgs e)
        {
            MinimizeToTray();
        }

        private async void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            await UpdateUIAsync();
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowForm();
        }

        private void ShowToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            ShowForm();
        }

        private async void CaptureToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                await _screenshotService.CaptureScreenshotAsync();
                ShowNotification("Captura de Pantalla", "Screenshot capturado desde la bandeja del sistema");
            }
            catch (Exception ex)
            {
                ShowNotification("Error de Captura", $"Error al capturar: {ex.Message}");
            }
        }
        
        private async void BtnExit_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("¿Está seguro que desea salir de Capturer?", 
                "Confirmar salida", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                await CleanupAndExit();
            }
        }
        
        private async void ExitToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            await CleanupAndExit();
        }

        private void ListView_DoubleClick(object? sender, EventArgs e)
        {
            if (listViewScreenshots.SelectedItems.Count > 0)
            {
                var selectedItem = listViewScreenshots.SelectedItems[0];
                var filePath = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ScreenshotService_ScreenshotCaptured(object? sender, ScreenshotCapturedEventArgs e)
        {
            if (e.Success && e.Screenshot != null)
            {
                Invoke(() => ShowNotification("Screenshot capturado", $"Guardado: {e.Screenshot.FileName}"));
            }
            else if (!string.IsNullOrEmpty(e.ErrorMessage))
            {
                Invoke(() => ShowNotification("Error de captura", e.ErrorMessage));
            }
        }

        private void EmailService_EmailSent(object? sender, EmailSentEventArgs e)
        {
            var message = e.Success ? 
                $"Email enviado exitosamente a {e.Recipients.Count} destinatarios" :
                $"Error enviando email: {e.ErrorMessage}";
            
            Invoke(() => ShowNotification("Email", message));
        }

        private async Task UpdateUIAsync()
        {
            try
            {
                // Update status
                lblStatus.Text = $"Estado: {(_screenshotService.IsCapturing ? "Ejecutándose" : "Detenido")}";

                // Update screenshot count and storage
                var count = await _fileService.GetScreenshotCountAsync();
                var size = await _fileService.GetFolderSizeAsync();
                lblTotalScreenshots.Text = $"Total Capturas: {count:N0}";
                lblStorageUsed.Text = $"Almacenamiento: {FileService.FormatFileSize(size)}";

                // Update recent screenshots
                await UpdateRecentScreenshotsAsync();

                // Update button states
                btnStartCapture.Enabled = !_screenshotService.IsCapturing;
                btnStopCapture.Enabled = _screenshotService.IsCapturing;

                // Update next capture time using the service's tracked time
                if (_screenshotService.IsCapturing && _screenshotService.NextCaptureTime.HasValue)
                {
                    lblNextCapture.Text = $"Próxima: {_screenshotService.NextCaptureTime.Value:HH:mm:ss}";
                }
                else
                {
                    lblNextCapture.Text = "Próxima: --:--:--";
                }

                // Update email status (simplified)
                lblLastEmail.Text = "Último Email: Configurar"; // TODO: Track last email date
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating UI: {ex.Message}");
            }
        }

        private async Task UpdateRecentScreenshotsAsync()
        {
            try
            {
                var recentScreenshots = await _fileService.GetRecentScreenshotsAsync(10);
                
                listViewScreenshots.Items.Clear();
                
                foreach (var filePath in recentScreenshots)
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists)
                    {
                        var item = new ListViewItem(fileInfo.Name);
                        item.SubItems.Add(fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        item.SubItems.Add(FileService.FormatFileSize(fileInfo.Length));
                        item.SubItems.Add(Path.GetDirectoryName(filePath) ?? "");
                        item.Tag = filePath;
                        
                        listViewScreenshots.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating recent screenshots: {ex.Message}");
            }
        }

        private void MinimizeToTray()
        {
            if (_config.Application.MinimizeToTray)
            {
                Hide();
                ShowNotification("Capturer", "Aplicación minimizada al área de notificación");
            }
            else
            {
                WindowState = FormWindowState.Minimized;
            }
        }

        private void ShowForm()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ShowNotification(string title, string message)
        {
            if (_config.Application.ShowNotifications)
            {
                notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
        }

        private async void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_config.Application.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToTray();
            }
            else
            {
                await CleanupAndExit();
            }
        }

        private async void BtnCaptureNow_Click(object? sender, EventArgs e)
        {
            try
            {
                btnCaptureNow.Enabled = false;
                btnCaptureNow.Text = "Capturando...";
                
                await _screenshotService.CaptureScreenshotAsync();
                ShowNotification("Captura Manual", "Screenshot capturado exitosamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al capturar screenshot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCaptureNow.Enabled = true;
                btnCaptureNow.Text = "Capturar Ahora";
            }
        }
        
        private async void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            try
            {
                var directoryInfo = await _fileService.GetScreenshotDirectoryInfoAsync();
                
                if (Directory.Exists(directoryInfo.FullName))
                {
                    Process.Start("explorer.exe", directoryInfo.FullName);
                }
                else
                {
                    MessageBox.Show("La carpeta de screenshots no existe. Verifique la configuración.", 
                        "Carpeta no encontrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir la carpeta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CleanupAndExit()
        {
            try
            {
                // Stop scheduler service
                if (_schedulerService != null)
                {
                    await _schedulerService.StopAsync();
                    _schedulerService.Dispose();
                }

                // Dispose services
                _serviceProvider?.Dispose();

                // Hide tray icon
                notifyIcon.Visible = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
            finally
            {
                Application.Exit();
            }
        }

    }
}
