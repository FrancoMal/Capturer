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
        private readonly IQuadrantService _quadrantService;
        private readonly IQuadrantSchedulerService _quadrantSchedulerService;
        private readonly ServiceProvider _serviceProvider;
        private CapturerConfiguration _config = new();
        
        // Dashboard de actividad
        private ActivityDashboardForm? _activityDashboard;

        public Form1(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _schedulerService = serviceProvider.GetRequiredService<ISchedulerService>();
            _screenshotService = serviceProvider.GetRequiredService<IScreenshotService>();
            _emailService = serviceProvider.GetRequiredService<IEmailService>();
            _fileService = serviceProvider.GetRequiredService<IFileService>();
            _configManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            _quadrantService = serviceProvider.GetRequiredService<IQuadrantService>();
            _quadrantSchedulerService = serviceProvider.GetRequiredService<IQuadrantSchedulerService>();

            InitializeComponent();
            InitializeApplication();
        }

        private async void InitializeApplication()
        {
            // Load configuration
            _config = await _configManager.LoadConfigurationAsync();

            // Setup form icon
            SetupFormIcon();

            // Setup ListView columns
            SetupListView();

            // Wire up event handlers
            WireUpEvents();
            
            // Configurar integración de monitoreo de actividad
            SetupActivityMonitoring();

            // Setup system tray
            SetupSystemTray();

            // Start scheduler service
            await _schedulerService.StartAsync();

            // Update UI
            await UpdateUIAsync();

            // Start update timer
            updateTimer.Start();
        }

        private void SetupFormIcon()
        {
            try
            {
                if (File.Exists("Capturer_Logo.ico"))
                {
                    this.Icon = new Icon("Capturer_Logo.ico");
                }
                else
                {
                    // Fallback to embedded resource
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
            
            // Setup logo image in PictureBox
            SetupLogoPictureBox();
        }

        private void SetupLogoPictureBox()
        {
            try
            {
                if (File.Exists("Capturer_Logo.png"))
                {
                    pictureBoxLogo.Image = Image.FromFile("Capturer_Logo.png");
                }
                else
                {
                    // Fallback to embedded resource
                    var stream = GetType().Assembly.GetManifestResourceStream("Capturer.Capturer_Logo.png");
                    if (stream != null)
                    {
                        pictureBoxLogo.Image = Image.FromStream(stream);
                    }
                }
                
                // Add a subtle border around the logo
                pictureBoxLogo.BorderStyle = BorderStyle.None;
                pictureBoxLogo.BackColor = this.BackColor;
            }
            catch
            {
                // If loading fails, hide the PictureBox
                pictureBoxLogo.Visible = false;
            }
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
            btnRoutineEmail.Click += BtnRoutineEmail_Click;
            btnCaptureNow.Click += BtnCaptureNow_Click;
            btnOpenFolder.Click += BtnOpenFolder_Click;
            btnQuadrants.Click += BtnQuadrants_Click;
            btnActivityDashboard.Click += BtnActivityDashboard_Click;
            btnMinimizeToTray.Click += BtnMinimizeToTray_Click;
            btnExit.Click += BtnExit_Click;

            // Timer events
            updateTimer.Tick += UpdateTimer_Tick;

            // ★ NEW v3.2.2: Enhanced system tray events with hide option
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            showToolStripMenuItem.Click += ShowToolStripMenuItem_Click;
            captureToolStripMenuItem.Click += CaptureToolStripMenuItem_Click;
            hideTrayToolStripMenuItem.Click += HideTrayToolStripMenuItem_Click; // NEW
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
            // ★ NEW v3.2.1: Simplified system tray setup using new configuration
            // Only setup system tray if background execution is enabled AND tray icon should be shown
            if (!_config.Application.BackgroundExecution.ShouldShowTrayIcon)
            {
                notifyIcon.Visible = false;
                return;
            }

            // Set icon for system tray using our custom logo
            try
            {
                if (File.Exists("Capturer_Logo.ico"))
                {
                    notifyIcon.Icon = new Icon("Capturer_Logo.ico");
                }
                else
                {
                    // Fallback to embedded resource
                    var stream = GetType().Assembly.GetManifestResourceStream("Capturer.Capturer_Logo.ico");
                    if (stream != null)
                    {
                        notifyIcon.Icon = new Icon(stream);
                    }
                    else
                    {
                        notifyIcon.Icon = SystemIcons.Application;
                    }
                }
            }
            catch
            {
                notifyIcon.Icon = SystemIcons.Application;
            }

            notifyIcon.Text = "Capturer v3.2.2 - Background Monitor";
            notifyIcon.Visible = true; // Always visible when background execution is enabled

            // ★ NEW v3.2.2: Enhanced context menu with hide option
            SetupTrayContextMenu();
        }

        /// <summary>
        /// ★ NEW v3.2.2: Setup enhanced context menu with hide system tray option
        /// </summary>
        private void SetupTrayContextMenu()
        {
            // The context menu is already created in Designer, just need to wire up events
            // and update visibility based on current configuration

            // Update context menu visibility based on configuration
            contextMenuStrip.Enabled = true;
            notifyIcon.ContextMenuStrip = contextMenuStrip;

            Console.WriteLine("[Capturer] Context menu configurado con opción 'Ocultar System Tray'");
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
            
            // Update system tray configuration
            UpdateSystemTrayConfiguration();
            
            // Update ActivityDashboard system tray configuration if it exists
            if (_activityDashboard != null && !_activityDashboard.IsDisposed)
            {
                _activityDashboard.UpdateSystemTrayConfiguration(_config);
            }
            
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
                var emailForm = new EmailForm(_emailService, _fileService, _configManager, _quadrantService);
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

        private void BtnRoutineEmail_Click(object? sender, EventArgs e)
        {
            try
            {
                var routineEmailForm = new RoutineEmailForm(_emailService, _fileService, _configManager, _quadrantService);
                if (routineEmailForm.ShowDialog(this) == DialogResult.OK)
                {
                    ShowNotification("Configuración", "Configuración de reportes automáticos guardada exitosamente");
                    
                    // Restart scheduler service to apply new configuration
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (_schedulerService.IsRunning)
                            {
                                await _schedulerService.StopAsync();
                                await _schedulerService.StartAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error restarting scheduler: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo configuración de reportes automáticos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        
        /// <summary>
        /// ★ NEW v3.2.2: Hide system tray icon while keeping background execution
        /// </summary>
        private void HideTrayToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "¿Está seguro de ocultar el icono del system tray?\n\n" +
                "✅ La aplicación seguirá ejecutándose en segundo plano\n" +
                "✅ Verificable en Administrador de Tareas > Capturer.exe\n" +
                "✅ Para volver a mostrar: ejecutar Capturer.exe nuevamente\n\n" +
                "💡 Esta acción resuelve conflictos con código legacy del system tray.",
                "🙈 Ocultar System Tray v3.2.2",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Update configuration to hide tray icon
                _config.Application.BackgroundExecution.ShowSystemTrayIcon = false;

                // Save configuration asynchronously
                _ = Task.Run(async () => await _configManager.SaveConfigurationAsync(_config));

                // Hide tray icon immediately
                notifyIcon.Visible = false;

                // Hide form if visible
                if (Visible)
                {
                    Hide();
                }

                // Show confirmation via Windows notification (since tray is hidden)
                Console.WriteLine("[Capturer] System tray ocultado - Aplicación ejecutándose en segundo plano");

                // Optional: Show Windows 10 toast notification
                try
                {
                    // This will only show if the system supports it
                    var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                    Console.WriteLine($"[Capturer] Proceso {processName} ejecutándose en segundo plano sin icono de tray");
                }
                catch { /* Ignore notification errors */ }
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
            // ★ NEW v3.2.1: Simplified minimize logic
            if (_config.Application.BackgroundExecution.ShouldShowTrayIcon)
            {
                Hide();
                ShowNotification("Capturer v3.2.1", "Ejecutándose en segundo plano - Visible en Administrador de Tareas");
            }
            else if (_config.Application.BackgroundExecution.ShouldRunInBackground)
            {
                // Background execution enabled but no tray icon - just hide window
                Hide();
                Console.WriteLine("[Capturer] Aplicación oculta - Ejecutándose en segundo plano sin icono");
            }
            else
            {
                // No background execution - normal minimize
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
            // ★ NEW v3.2.1: Simplified notification logic
            if (_config.Application.ShowNotifications &&
                _config.Application.BackgroundExecution.ShouldShowTrayIcon &&
                _config.Application.BackgroundExecution.ShowTrayNotifications)
            {
                notifyIcon.ShowBalloonTip(_config.Application.BackgroundExecution.NotificationDurationMs, title, message, ToolTipIcon.Info);
            }
        }

        private async void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // ★ NEW v3.2.1: Clear background execution logic

            // If background execution is enabled, NEVER close the application completely
            if (_config.Application.BackgroundExecution.ShouldRunInBackground && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;

                if (_config.Application.BackgroundExecution.ShouldHideToTrayOnClose)
                {
                    // Hide to tray (or just hide if no tray icon)
                    MinimizeToTray();
                }
                else
                {
                    // Just minimize normally but keep running in background
                    WindowState = FormWindowState.Minimized;
                    Console.WriteLine("[Capturer] Minimizado - Ejecutándose en segundo plano (verificable en Administrador de Tareas)");
                }

                return; // NEVER exit when background execution is enabled
            }

            // Only allow full exit if background execution is disabled OR forced shutdown
            Console.WriteLine($"[Capturer] Cerrando aplicación completamente. Razón: {e.CloseReason}");
            await CleanupAndExit();
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

        private void BtnQuadrants_Click(object? sender, EventArgs e)
        {
            try
            {
                // Check if quadrant system is enabled
                if (!_config.QuadrantSystem.IsEnabled)
                {
                    var result = MessageBox.Show(
                        "El sistema de cuadrantes está deshabilitado.\n\n¿Desea habilitarlo ahora?",
                        "Sistema de Cuadrantes - Beta",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        _config.QuadrantSystem.IsEnabled = true;
                        _configManager.SaveConfigurationAsync(_config);
                        ShowNotification("Cuadrantes", "Sistema de cuadrantes habilitado");
                    }
                    else
                    {
                        return;
                    }
                }

                // Open quadrant editor form
                var quadrantEditorForm = new QuadrantEditorForm(
                    _quadrantService, 
                    _configManager, 
                    _screenshotService);

                quadrantEditorForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo editor de cuadrantes: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnActivityDashboard_Click(object? sender, EventArgs e)
        {
            try
            {
                // Verificar que el sistema de cuadrantes esté habilitado
                if (!_config.QuadrantSystem.IsEnabled)
                {
                    var result = MessageBox.Show(
                        "El dashboard de actividad requiere que el sistema de cuadrantes esté habilitado.\n\n¿Desea habilitar los cuadrantes ahora?",
                        "Dashboard de Actividad",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        _config.QuadrantSystem.IsEnabled = true;
                        _configManager.SaveConfigurationAsync(_config);
                        ShowNotification("Sistema habilitado", "Cuadrantes y monitoreo de actividad habilitados");
                    }
                    else
                    {
                        return;
                    }
                }

                // Verificar que haya al menos una configuración de cuadrantes
                var activeConfig = _quadrantService.GetActiveConfiguration();
                if (activeConfig == null)
                {
                    MessageBox.Show(
                        "Error: No se pudo obtener una configuración de cuadrantes válida.\n\nIntente reiniciar la aplicación o contacte soporte técnico.",
                        "Dashboard de Actividad - Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                
                // Si no hay cuadrantes habilitados, ofrecer configurar
                if (!activeConfig.GetEnabledQuadrants().Any())
                {
                    var result = MessageBox.Show(
                        $"La configuración '{activeConfig.Name}' no tiene cuadrantes habilitados.\n\n¿Desea configurar los cuadrantes ahora?",
                        "Dashboard de Actividad",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        BtnQuadrants_Click(sender, e);
                    }
                    return;
                }

                // Si el dashboard ya está abierto, traerlo al frente
                if (_activityDashboard != null && !_activityDashboard.IsDisposed)
                {
                    _activityDashboard.BringToFront();
                    _activityDashboard.WindowState = FormWindowState.Normal;
                    return;
                }

                // Crear y mostrar el dashboard
                var quadrantService = _quadrantService as QuadrantService;
                if (quadrantService?.ActivityService != null)
                {
                    // Get required services from DI container
                    var reportService = _serviceProvider.GetService<ActivityReportService>();
                    var schedulerService = _serviceProvider.GetService<ActivityDashboardSchedulerService>();
                    var simplifiedScheduler = _serviceProvider.GetService<SimplifiedReportsSchedulerService>();
                    
                    _activityDashboard = new ActivityDashboardForm(quadrantService.ActivityService, quadrantService, reportService!, schedulerService, simplifiedScheduler, _config, _emailService);
                    _activityDashboard.Show();
                    
                    ShowNotification("Dashboard", "Dashboard de actividad abierto");
                }
                else
                {
                    MessageBox.Show("Error: No se pudo acceder al servicio de actividad", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo dashboard de actividad: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupActivityMonitoring()
        {
            try
            {
                // Conectar el servicio de screenshots con el servicio de cuadrantes para monitoreo
                if (_screenshotService is ScreenshotService screenshotService && 
                    _quadrantService is QuadrantService quadrantService)
                {
                    screenshotService.SetQuadrantService(quadrantService);
                    Console.WriteLine("[Form1] Monitoreo de actividad configurado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Form1] Error configurando monitoreo de actividad: {ex.Message}");
            }
        }

        private async Task CleanupAndExit()
        {
            try
            {
                // Stop scheduler services
                if (_schedulerService != null)
                {
                    await _schedulerService.StopAsync();
                    _schedulerService.Dispose();
                }

                if (_quadrantSchedulerService != null)
                {
                    await _quadrantSchedulerService.StopAsync();
                    _quadrantSchedulerService.Dispose();
                }

                // Close activity dashboard if open
                if (_activityDashboard != null && !_activityDashboard.IsDisposed)
                {
                    _activityDashboard.Close();
                    _activityDashboard = null;
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

        /// <summary>
        /// ★ NEW v3.2.1: Updates system tray configuration when settings change
        /// </summary>
        public void UpdateSystemTrayConfiguration()
        {
            // Use simplified configuration logic
            if (_config.Application.BackgroundExecution.ShouldShowTrayIcon)
            {
                // Enable system tray icon
                notifyIcon.Visible = true;
                Console.WriteLine("[Capturer] System tray habilitado - Configuración actualizada");
            }
            else
            {
                // Disable system tray icon (but app can still run in background)
                notifyIcon.Visible = false;
                Console.WriteLine("[Capturer] System tray deshabilitado - App puede seguir ejecutándose en segundo plano");
            }
        }

        /// <summary>
        /// Public method to refresh configuration and update UI
        /// </summary>
        public async Task RefreshConfigurationAsync()
        {
            _config = await _configManager.LoadConfigurationAsync();
            UpdateSystemTrayConfiguration();
        }

    }
}
