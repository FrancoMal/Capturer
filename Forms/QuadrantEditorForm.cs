using System.Drawing;
using System.Drawing.Drawing2D;
using Capturer.Models;
using Capturer.Services;

namespace Capturer.Forms;

public partial class QuadrantEditorForm : Form
{
    private readonly IQuadrantService _quadrantService;
    private readonly IConfigurationManager _configManager;
    private readonly IScreenshotService _screenshotService;
    private CapturerConfiguration _config = new();
    
    // UI Controls
    private ComboBox cmbConfigurations = new();
    private TextBox txtConfigName = new();
    private Button btnNewConfig = new();
    private Button btnDeleteConfig = new();
    private Button btnSaveConfig = new();
    
    // Preview Panel
    private Panel previewPanel = new();
    private PictureBox previewPictureBox = new();
    private Bitmap? _previewImage;
    private Size _screenResolution = new(1920, 1080);
    
    // Quadrant Management
    private ListBox lstQuadrants = new();
    private TextBox txtQuadrantName = new();
    private Button btnAddQuadrant = new();
    private Button btnRemoveQuadrant = new();
    private CheckedListBox chkQuadrants = new();
    
    // Processing Controls
    private ComboBox cmbDateRange = new();
    private DateTimePicker dtpStartDate = new();
    private DateTimePicker dtpEndDate = new();
    
    // Preview Controls
    private ComboBox cmbPreviewImage = new();
    private Button btnRefreshImages = new();
    private Button btnProcessImages = new();
    private ProgressBar progressBar = new();
    private Label lblProgress = new();
    private Button btnCancelProcessing = new();
    
    // Current state
    private QuadrantConfiguration _currentConfiguration = new();
    private QuadrantRegion? _selectedQuadrant;
    private bool _isDragging = false;
    private bool _isResizing = false;
    private Point _dragStart;
    private Rectangle _originalBounds;
    private ResizeHandle _resizeHandle = ResizeHandle.None;
    
    private enum ResizeHandle
    {
        None,
        TopLeft, TopRight, BottomLeft, BottomRight,
        Top, Right, Bottom, Left
    }
    private CancellationTokenSource? _cancellationTokenSource;

    public QuadrantEditorForm(IQuadrantService quadrantService, IConfigurationManager configManager, IScreenshotService screenshotService)
    {
        _quadrantService = quadrantService;
        _configManager = configManager;
        _screenshotService = screenshotService;
        
        InitializeComponent();
        _ = LoadConfigurationAsync(); // Fire and forget initialization
        SetupEventHandlers();
        
        // Initialize with default resolution - will be updated when user selects preview image
        _screenResolution = new Size(1920, 1080);
        
        // Don't auto-detect resolution anymore - let user select preview image
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1200, 800);
        this.Text = "Editor de Cuadrantes - Capturer v3.2.0";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(800, 600);
        this.AutoScroll = true; // Enable automatic scrolling
        
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

        CreateConfigurationPanel();
        CreatePreviewPanel();
        CreateQuadrantManagementPanel();
        CreateProcessingPanel();
        
        this.Load += QuadrantEditorForm_Load;
    }

    private void CreateConfigurationPanel()
    {
        var configPanel = new GroupBox
        {
            Text = "Configuración",
            Location = new Point(10, 10),
            Size = new Size(350, 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        // Configuration selection
        var lblConfig = new Label { Text = "Configuración:", Location = new Point(10, 25), Size = new Size(80, 20) };
        cmbConfigurations = new ComboBox 
        { 
            Location = new Point(100, 23), 
            Size = new Size(150, 23), 
            DropDownStyle = ComboBoxStyle.DropDownList 
        };

        btnNewConfig = new Button { Text = "Nueva", Location = new Point(260, 22), Size = new Size(60, 25) };
        btnDeleteConfig = new Button { Text = "Eliminar", Location = new Point(260, 52), Size = new Size(60, 25) };

        // Configuration name
        var lblConfigName = new Label { Text = "Nombre:", Location = new Point(10, 55), Size = new Size(80, 20) };
        txtConfigName = new TextBox { Location = new Point(100, 53), Size = new Size(150, 23) };

        btnSaveConfig = new Button { Text = "Guardar Config", Location = new Point(100, 85), Size = new Size(100, 25) };

        configPanel.Controls.AddRange(new Control[] 
        { 
            lblConfig, cmbConfigurations, btnNewConfig, btnDeleteConfig,
            lblConfigName, txtConfigName, btnSaveConfig 
        });
        
        this.Controls.Add(configPanel);
    }

    private void CreatePreviewPanel()
    {
        var previewGroup = new GroupBox
        {
            Text = "Vista Previa - Haz clic y arrastra para crear/editar cuadrantes",
            Location = new Point(370, 10),
            Size = new Size(800, 500),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        // Preview image selection controls
        var lblPreviewImage = new Label
        {
            Text = "Imagen de referencia:",
            Location = new Point(10, 20),
            Size = new Size(120, 20)
        };

        cmbPreviewImage = new ComboBox
        {
            Location = new Point(135, 18),
            Size = new Size(300, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        btnRefreshImages = new Button
        {
            Text = "Actualizar",
            Location = new Point(445, 17),
            Size = new Size(80, 25)
        };

        var lblResolution = new Label
        {
            Text = "Resolución:",
            Location = new Point(535, 20),
            Size = new Size(70, 20)
        };

        var lblResolutionValue = new Label
        {
            Name = "lblResolutionValue",
            Text = "Seleccionar imagen...",
            Location = new Point(610, 20),
            Size = new Size(80, 20),
            ForeColor = Color.Blue,
            Font = new Font("Arial", 8, FontStyle.Bold)
        };

        previewPanel = new Panel
        {
            Location = new Point(10, 50),
            Size = new Size(780, 440),
            BorderStyle = BorderStyle.Fixed3D,
            BackColor = Color.Black,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        previewPictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        previewPanel.Controls.Add(previewPictureBox);
        previewGroup.Controls.AddRange(new Control[] 
        { 
            lblPreviewImage, cmbPreviewImage, btnRefreshImages, 
            lblResolution, lblResolutionValue, previewPanel 
        });
        this.Controls.Add(previewGroup);
    }

    private void CreateQuadrantManagementPanel()
    {
        var quadrantPanel = new GroupBox
        {
            Text = "Gestión de Cuadrantes",
            Location = new Point(10, 140),
            Size = new Size(350, 250),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
        };

        // Quadrant list
        var lblQuadrants = new Label { Text = "Cuadrantes:", Location = new Point(10, 25), Size = new Size(80, 20) };
        lstQuadrants = new ListBox 
        { 
            Location = new Point(10, 45), 
            Size = new Size(200, 120),
            DisplayMember = "Name"
        };

        // Add/Remove controls
        var lblNewQuadrant = new Label { Text = "Nombre:", Location = new Point(220, 25), Size = new Size(60, 20) };
        txtQuadrantName = new TextBox { Location = new Point(220, 45), Size = new Size(120, 23) };
        btnAddQuadrant = new Button { Text = "Agregar", Location = new Point(220, 75), Size = new Size(60, 25) };
        btnRemoveQuadrant = new Button { Text = "Eliminar", Location = new Point(285, 75), Size = new Size(55, 25) };

        // Quadrant enable/disable
        var lblEnabled = new Label { Text = "Cuadrantes activos:", Location = new Point(10, 175), Size = new Size(120, 20) };
        chkQuadrants = new CheckedListBox 
        { 
            Location = new Point(10, 195), 
            Size = new Size(330, 45),
            CheckOnClick = true
        };

        quadrantPanel.Controls.AddRange(new Control[] 
        { 
            lblQuadrants, lstQuadrants, lblNewQuadrant, txtQuadrantName, 
            btnAddQuadrant, btnRemoveQuadrant, lblEnabled, chkQuadrants 
        });
        
        this.Controls.Add(quadrantPanel);
    }

    private void CreateProcessingPanel()
    {
        var processingPanel = new GroupBox
        {
            Text = "Procesamiento de Imágenes",
            Location = new Point(10, 400),
            Size = new Size(350, 200),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        // Date range selection
        var lblDateRange = new Label { Text = "Rango:", Location = new Point(10, 25), Size = new Size(50, 20) };
        cmbDateRange = new ComboBox 
        { 
            Location = new Point(70, 23), 
            Size = new Size(120, 23), 
            DropDownStyle = ComboBoxStyle.DropDownList 
        };
        cmbDateRange.Items.AddRange(new[] { "Hoy", "Últimos 7 días", "Últimos 30 días", "Rango personalizado" });
        cmbDateRange.SelectedIndex = 1;

        // Custom date range
        var lblStartDate = new Label { Text = "Desde:", Location = new Point(10, 55), Size = new Size(50, 20) };
        dtpStartDate = new DateTimePicker { Location = new Point(70, 53), Size = new Size(120, 23) };
        
        var lblEndDate = new Label { Text = "Hasta:", Location = new Point(200, 55), Size = new Size(50, 20) };
        dtpEndDate = new DateTimePicker { Location = new Point(250, 53), Size = new Size(90, 23) };

        // Processing controls
        btnProcessImages = new Button 
        { 
            Text = "Procesar Imágenes", 
            Location = new Point(10, 85), 
            Size = new Size(120, 30),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        btnCancelProcessing = new Button 
        { 
            Text = "Cancelar", 
            Location = new Point(140, 85), 
            Size = new Size(80, 30),
            BackColor = Color.Red,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };

        // Progress indicators
        progressBar = new ProgressBar { Location = new Point(10, 125), Size = new Size(330, 20) };
        lblProgress = new Label 
        { 
            Text = "Listo para procesar", 
            Location = new Point(10, 150), 
            Size = new Size(330, 40),
            ForeColor = Color.DarkGreen
        };

        processingPanel.Controls.AddRange(new Control[] 
        { 
            lblDateRange, cmbDateRange, lblStartDate, dtpStartDate, lblEndDate, dtpEndDate,
            btnProcessImages, btnCancelProcessing, progressBar, lblProgress 
        });
        
        this.Controls.Add(processingPanel);
    }

    private void SetupEventHandlers()
    {
        // Configuration events
        cmbConfigurations.SelectedIndexChanged += CmbConfigurations_SelectedIndexChanged;
        btnNewConfig.Click += BtnNewConfig_Click;
        btnDeleteConfig.Click += BtnDeleteConfig_Click;
        btnSaveConfig.Click += BtnSaveConfig_Click;

        // Quadrant management events
        lstQuadrants.SelectedIndexChanged += LstQuadrants_SelectedIndexChanged;
        btnAddQuadrant.Click += BtnAddQuadrant_Click;
        btnRemoveQuadrant.Click += BtnRemoveQuadrant_Click;
        chkQuadrants.ItemCheck += ChkQuadrants_ItemCheck;

        // Preview events
        previewPictureBox.Paint += PreviewPictureBox_Paint;
        previewPictureBox.MouseDown += PreviewPictureBox_MouseDown;
        previewPictureBox.MouseMove += PreviewPictureBox_MouseMove;
        previewPictureBox.MouseUp += PreviewPictureBox_MouseUp;
        
        // Preview image selection events
        cmbPreviewImage.SelectedIndexChanged += CmbPreviewImage_SelectedIndexChanged;
        btnRefreshImages.Click += BtnRefreshImages_Click;

        // Processing events
        cmbDateRange.SelectedIndexChanged += CmbDateRange_SelectedIndexChanged;
        btnProcessImages.Click += BtnProcessImages_Click;
        btnCancelProcessing.Click += BtnCancelProcessing_Click;

        // Service events
        _quadrantService.ProcessingProgressChanged += QuadrantService_ProcessingProgressChanged;
        _quadrantService.ProcessingCompleted += QuadrantService_ProcessingCompleted;
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
            LoadConfigurations();
            UpdateDateRangeVisibility();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadConfigurations()
    {
        var configurations = _quadrantService.GetConfigurations();
        
        cmbConfigurations.Items.Clear();
        foreach (var config in configurations)
        {
            cmbConfigurations.Items.Add(config);
        }

        var activeConfig = _quadrantService.GetActiveConfiguration();
        if (activeConfig != null)
        {
            cmbConfigurations.SelectedItem = activeConfig;
        }
        else if (cmbConfigurations.Items.Count > 0)
        {
            cmbConfigurations.SelectedIndex = 0;
        }
    }

    private async void QuadrantEditorForm_Load(object? sender, EventArgs e)
    {
        await LoadPreviewImageAsync();
        LoadAvailableImages();
    }

    private async Task LoadPreviewImageAsync()
    {
        try
        {
            // This method now just ensures we have a basic preview
            // The actual image selection is handled by the ComboBox
            
            if (_previewImage == null)
            {
                // Create a placeholder if no image is selected yet
                _previewImage = new Bitmap(_screenResolution.Width, _screenResolution.Height);
                using var g = Graphics.FromImage(_previewImage);
                g.Clear(Color.DarkGray);
                using var font = new Font("Arial", 20, FontStyle.Bold);
                using var brush = new SolidBrush(Color.White);
                var text = "Selecciona una imagen de referencia";
                var textSize = g.MeasureString(text, font);
                var x = (_previewImage.Width - textSize.Width) / 2;
                var y = (_previewImage.Height - textSize.Height) / 2;
                g.DrawString(text, font, brush, x, y);
                
                previewPictureBox.Image = new Bitmap(_previewImage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading preview image: {ex.Message}");
        }
    }

    private void CmbConfigurations_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbConfigurations.SelectedItem is QuadrantConfiguration config)
        {
            _currentConfiguration = config;
            txtConfigName.Text = config.Name;
            LoadQuadrantsFromConfiguration();
            previewPictureBox.Invalidate();
        }
    }

    private void LoadQuadrantsFromConfiguration()
    {
        // Update quadrants list
        lstQuadrants.Items.Clear();
        chkQuadrants.Items.Clear();

        foreach (var quadrant in _currentConfiguration.Quadrants)
        {
            lstQuadrants.Items.Add(quadrant);
            chkQuadrants.Items.Add(quadrant.Name, quadrant.IsEnabled);
        }
    }

    private async void BtnNewConfig_Click(object? sender, EventArgs e)
    {
        var newConfig = QuadrantConfiguration.CreateDefault(_screenResolution);
        newConfig.Name = $"Configuración {DateTime.Now:yyyyMMdd_HHmmss}";
        
        if (await _quadrantService.SaveConfigurationAsync(newConfig))
        {
            LoadConfigurations();
            cmbConfigurations.SelectedItem = newConfig;
            MessageBox.Show("Nueva configuración creada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void BtnDeleteConfig_Click(object? sender, EventArgs e)
    {
        if (cmbConfigurations.SelectedItem is QuadrantConfiguration config)
        {
            var result = MessageBox.Show($"¿Está seguro de eliminar la configuración '{config.Name}'?", 
                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                if (await _quadrantService.DeleteConfigurationAsync(config.Name))
                {
                    LoadConfigurations();
                    MessageBox.Show("Configuración eliminada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }

    private async void BtnSaveConfig_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtConfigName.Text))
        {
            MessageBox.Show("Ingrese un nombre para la configuración.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _currentConfiguration.Name = txtConfigName.Text;
        _currentConfiguration.ScreenResolution = _screenResolution;
        _currentConfiguration.LastModified = DateTime.Now;

        if (await _quadrantService.SaveConfigurationAsync(_currentConfiguration))
        {
            LoadConfigurations();
            MessageBox.Show("Configuración guardada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // Preview painting and interaction methods continue in next part...
    // [The file is getting long, continuing with key methods]

    private void PreviewPictureBox_Paint(object? sender, PaintEventArgs e)
    {
        if (_currentConfiguration?.Quadrants == null || !_currentConfiguration.Quadrants.Any())
            return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Calculate scale factor
        var scaleX = (float)previewPictureBox.Width / _screenResolution.Width;
        var scaleY = (float)previewPictureBox.Height / _screenResolution.Height;
        var scale = Math.Min(scaleX, scaleY);

        // Calculate offset for centering
        var offsetX = (previewPictureBox.Width - _screenResolution.Width * scale) / 2;
        var offsetY = (previewPictureBox.Height - _screenResolution.Height * scale) / 2;

        foreach (var quadrant in _currentConfiguration.Quadrants)
        {
            if (!quadrant.IsEnabled) continue;

            // Scale and offset the quadrant bounds
            var scaledBounds = new RectangleF(
                quadrant.Bounds.X * scale + offsetX,
                quadrant.Bounds.Y * scale + offsetY,
                quadrant.Bounds.Width * scale,
                quadrant.Bounds.Height * scale);

            // Draw quadrant boundary
            var isSelected = quadrant == _selectedQuadrant;
            var penWidth = isSelected ? 3f : 2f;
            using var pen = new Pen(quadrant.PreviewColor, penWidth);
            
            if (isSelected)
            {
                pen.DashStyle = DashStyle.Dash;
            }

            g.DrawRectangle(pen, Rectangle.Round(scaledBounds));

            // Fill with semi-transparent color
            using var brush = new SolidBrush(Color.FromArgb(30, quadrant.PreviewColor));
            g.FillRectangle(brush, scaledBounds);

            // Draw label
            using var font = new Font("Arial", 10, FontStyle.Bold);
            using var textBrush = new SolidBrush(quadrant.PreviewColor);
            var labelPos = new PointF(scaledBounds.X + 5, scaledBounds.Y + 5);
            g.DrawString(quadrant.Name, font, textBrush, labelPos);

            // Draw coordinates
            var coordText = $"{quadrant.Bounds.Width}x{quadrant.Bounds.Height} @ ({quadrant.Bounds.X},{quadrant.Bounds.Y})";
            using var coordFont = new Font("Arial", 8);
            var coordPos = new PointF(scaledBounds.X + 5, scaledBounds.Y + 20);
            g.DrawString(coordText, coordFont, textBrush, coordPos);
            
            // Draw resize handles for selected quadrant
            if (isSelected)
            {
                DrawResizeHandles(g, scaledBounds);
            }
        }
    }
    
    private void DrawResizeHandles(Graphics g, RectangleF bounds)
    {
        const int handleSize = 8;
        using var handleBrush = new SolidBrush(Color.White);
        using var handlePen = new Pen(Color.Black, 1);
        
        var handles = new[]
        {
            new PointF(bounds.Left, bounds.Top),           // TopLeft
            new PointF(bounds.Right, bounds.Top),          // TopRight
            new PointF(bounds.Left, bounds.Bottom),        // BottomLeft
            new PointF(bounds.Right, bounds.Bottom),       // BottomRight
            new PointF(bounds.Left + bounds.Width / 2, bounds.Top),     // Top
            new PointF(bounds.Right, bounds.Top + bounds.Height / 2),   // Right
            new PointF(bounds.Left + bounds.Width / 2, bounds.Bottom),  // Bottom
            new PointF(bounds.Left, bounds.Top + bounds.Height / 2)     // Left
        };
        
        foreach (var handle in handles)
        {
            var handleRect = new RectangleF(
                handle.X - handleSize / 2f,
                handle.Y - handleSize / 2f,
                handleSize,
                handleSize);
                
            g.FillRectangle(handleBrush, handleRect);
            g.DrawRectangle(handlePen, Rectangle.Round(handleRect));
        }
    }

    private void UpdateDateRangeVisibility()
    {
        var showCustomRange = cmbDateRange.SelectedIndex == 3; // "Rango personalizado"
        dtpStartDate.Visible = showCustomRange;
        dtpEndDate.Visible = showCustomRange;
    }

    private void CmbDateRange_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateDateRangeVisibility();
    }

    private async void BtnProcessImages_Click(object? sender, EventArgs e)
    {
        if (_currentConfiguration?.GetEnabledQuadrants().Count == 0)
        {
            MessageBox.Show("Debe tener al menos un cuadrante activo para procesar.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Validate that a preview image has been selected
        if (cmbPreviewImage.SelectedItem == null)
        {
            MessageBox.Show("Debe seleccionar una imagen de referencia antes de procesar.\n\n" +
                          "Seleccione una imagen del dropdown 'Vista previa' para establecer la resolución correcta.", 
                          "Imagen de referencia requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var (startDate, endDate) = GetSelectedDateRange();
        
        // Check if there are images in the date range first
        lblProgress.Text = "Verificando imágenes disponibles...";
        lblProgress.ForeColor = Color.Blue;
        Application.DoEvents(); // Force UI update
        
        // Use the resolution from the currently selected preview image
        // Don't auto-detect or change resolution during processing
        _currentConfiguration.ScreenResolution = _screenResolution;
        
        lblProgress.Text = $"Usando resolución de vista previa: {_screenResolution.Width}x{_screenResolution.Height}";
        lblProgress.ForeColor = Color.Blue;
        Application.DoEvents();
        await Task.Delay(1000);
        
        try
        {
            // Get a quick count of available images
            var screenshotFolder = _config.Storage.ScreenshotFolder;
            if (!Directory.Exists(screenshotFolder))
            {
                MessageBox.Show("La carpeta de screenshots no existe. Verifique la configuración.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            var imageFiles = Directory.GetFiles(screenshotFolder, "*.png")
                .Where(f => 
                {
                    if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(f), 
                        "yyyy-MM-dd_HH-mm-ss", null, System.Globalization.DateTimeStyles.None, out var fileDate))
                    {
                        return fileDate >= startDate && fileDate <= endDate;
                    }
                    return false;
                })
                .ToList();
                
            if (imageFiles.Count == 0)
            {
                MessageBox.Show($"No se encontraron imágenes en el rango de fechas seleccionado.\nRango: {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}", 
                    "Sin imágenes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                lblProgress.Text = "Sin imágenes para procesar";
                lblProgress.ForeColor = Color.Orange;
                return;
            }
            
            // Show confirmation with count
            var result = MessageBox.Show(
                $"Se procesarán {imageFiles.Count} imágenes con {_currentConfiguration.GetEnabledQuadrants().Count} cuadrantes activos.\n\n" +
                $"Rango: {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}\n\n" +
                "¿Desea continuar?", 
                "Confirmar procesamiento", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
                
            if (result != DialogResult.Yes)
            {
                lblProgress.Text = "Procesamiento cancelado";
                lblProgress.ForeColor = Color.Gray;
                return;
            }
            
            btnProcessImages.Enabled = false;
            btnCancelProcessing.Enabled = true;
            progressBar.Value = 0;
            progressBar.Visible = true;
            lblProgress.Text = $"Iniciando procesamiento de {imageFiles.Count} imágenes...";
            lblProgress.ForeColor = Color.Blue;

            _cancellationTokenSource = new CancellationTokenSource();

            var task = await _quadrantService.ProcessImagesAsync(
                startDate, 
                endDate, 
                _currentConfiguration.Name,
                null, // Progress handled through events
                _cancellationTokenSource.Token);

            // Processing completed through event handler
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error durante el procesamiento: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ResetProcessingUI();
        }
    }

    private void BtnCancelProcessing_Click(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine("[QuadrantEditorForm] Usuario solicitó cancelación");
            _cancellationTokenSource?.Cancel();
            
            lblProgress.Text = "Cancelando procesamiento...";
            lblProgress.ForeColor = Color.Orange;
            
            btnCancelProcessing.Enabled = false;
            btnCancelProcessing.Text = "Cancelando...";
            
            // Force UI update
            Application.DoEvents();
            
            Console.WriteLine("[QuadrantEditorForm] Token de cancelación enviado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuadrantEditorForm] Error durante cancelación: {ex.Message}");
            MessageBox.Show($"Error cancelando procesamiento: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private (DateTime start, DateTime end) GetSelectedDateRange()
    {
        var end = DateTime.Now;
        var start = cmbDateRange.SelectedIndex switch
        {
            0 => DateTime.Today,                    // Hoy
            1 => DateTime.Today.AddDays(-7),        // Últimos 7 días
            2 => DateTime.Today.AddDays(-30),       // Últimos 30 días
            3 => dtpStartDate.Value.Date,           // Personalizado
            _ => DateTime.Today.AddDays(-7)
        };

        if (cmbDateRange.SelectedIndex == 3)
        {
            end = dtpEndDate.Value.Date.AddDays(1).AddTicks(-1);
        }

        return (start, end);
    }

    private void QuadrantService_ProcessingProgressChanged(object? sender, ProcessingProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => QuadrantService_ProcessingProgressChanged(sender, e));
            return;
        }

        // Update progress bar
        progressBar.Value = Math.Min(100, Math.Max(0, e.Progress.ProgressPercentage));
        
        // Update progress text with detailed information
        var statusText = $"Procesando: {e.Progress.CurrentFileName}";
        if (e.Progress.TotalFiles > 0)
        {
            statusText += $" ({e.Progress.CurrentFile}/{e.Progress.TotalFiles} - {e.Progress.ProgressPercentage}%)";
        }
        
        lblProgress.Text = statusText;
        lblProgress.ForeColor = Color.Blue;
        
        // Force UI update
        Application.DoEvents();
    }

    private void QuadrantService_ProcessingCompleted(object? sender, ProcessingCompletedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => QuadrantService_ProcessingCompleted(sender, e));
            return;
        }

        ResetProcessingUI();

        var message = e.Success 
            ? $"Procesamiento completado exitosamente!\n{e.Task.GetSummary()}"
            : $"Procesamiento completado con errores:\n{e.Task.GetSummary()}\n\nErrores: {e.ErrorMessage}";

        var icon = e.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
        
        MessageBox.Show(message, "Procesamiento Completado", MessageBoxButtons.OK, icon);
        
        lblProgress.Text = e.Success ? "Procesamiento completado exitosamente" : "Procesamiento completado con errores";
        lblProgress.ForeColor = e.Success ? Color.Green : Color.Orange;
    }

    private void ResetProcessingUI()
    {
        Console.WriteLine("[QuadrantEditorForm] Reseteando UI de procesamiento");
        
        btnProcessImages.Enabled = true;
        btnCancelProcessing.Enabled = false;
        btnCancelProcessing.Text = "Cancelar";
        progressBar.Value = 0;
        progressBar.Visible = true; // Keep visible to show 0 progress
        
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        
        Console.WriteLine("[QuadrantEditorForm] UI de procesamiento reseteada");
    }

    // Additional event handlers for quadrant management...
    private void LstQuadrants_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedQuadrant = lstQuadrants.SelectedItem as QuadrantRegion;
        previewPictureBox.Invalidate();
    }

    private void BtnAddQuadrant_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtQuadrantName.Text))
        {
            MessageBox.Show("Ingrese un nombre para el cuadrante.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var colors = new[] { Color.Red, Color.Green, Color.Blue, Color.Orange, Color.Purple, Color.Brown };
        var colorIndex = _currentConfiguration.Quadrants.Count % colors.Length;
        
        var newQuadrant = new QuadrantRegion(
            txtQuadrantName.Text,
            new Rectangle(50 + _currentConfiguration.Quadrants.Count * 20, 50 + _currentConfiguration.Quadrants.Count * 20, 200, 150),
            colors[colorIndex]);

        _currentConfiguration.AddQuadrant(newQuadrant);
        LoadQuadrantsFromConfiguration();
        txtQuadrantName.Clear();
        previewPictureBox.Invalidate();
    }

    private void BtnRemoveQuadrant_Click(object? sender, EventArgs e)
    {
        if (lstQuadrants.SelectedItem is QuadrantRegion quadrant)
        {
            _currentConfiguration.RemoveQuadrant(quadrant.Name);
            LoadQuadrantsFromConfiguration();
            previewPictureBox.Invalidate();
        }
    }

    private void ChkQuadrants_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (e.Index < _currentConfiguration.Quadrants.Count)
        {
            _currentConfiguration.Quadrants[e.Index].IsEnabled = e.NewValue == CheckState.Checked;
            previewPictureBox.Invalidate();
        }
    }

    // Mouse interaction methods for dragging quadrants...
    private void PreviewPictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _previewImage != null)
        {
            _selectedQuadrant = GetQuadrantAtPoint(e.Location);
            if (_selectedQuadrant != null)
            {
                _dragStart = e.Location;
                _originalBounds = _selectedQuadrant.Bounds;
                
                // Check if click is on resize handle
                _resizeHandle = GetResizeHandle(e.Location, _selectedQuadrant);
                
                if (_resizeHandle != ResizeHandle.None)
                {
                    _isResizing = true;
                    previewPictureBox.Cursor = GetResizeCursor(_resizeHandle);
                }
                else
                {
                    _isDragging = true;
                    previewPictureBox.Cursor = Cursors.SizeAll;
                }
                
                // Update selected quadrant in list
                UpdateSelectedQuadrantInList();
                previewPictureBox.Invalidate();
            }
        }
    }

    private void PreviewPictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if ((_isDragging || _isResizing) && _selectedQuadrant != null)
        {
            var scale = GetPreviewScale();
            var deltaX = (int)((e.X - _dragStart.X) / scale);
            var deltaY = (int)((e.Y - _dragStart.Y) / scale);
            
            if (_isDragging)
            {
                // Move quadrant
                var newX = Math.Max(0, Math.Min(_screenResolution.Width - _originalBounds.Width, _originalBounds.X + deltaX));
                var newY = Math.Max(0, Math.Min(_screenResolution.Height - _originalBounds.Height, _originalBounds.Y + deltaY));
                
                _selectedQuadrant.Bounds = new Rectangle(newX, newY, _originalBounds.Width, _originalBounds.Height);
            }
            else if (_isResizing)
            {
                // Resize quadrant
                var newBounds = CalculateResizedBounds(_originalBounds, deltaX, deltaY, _resizeHandle);
                _selectedQuadrant.Bounds = ClampBounds(newBounds);
            }

            previewPictureBox.Invalidate();
            UpdateCoordinateLabels();
        }
        else
        {
            // Update cursor based on hover position
            var quadrant = GetQuadrantAtPoint(e.Location);
            if (quadrant != null)
            {
                var handle = GetResizeHandle(e.Location, quadrant);
                previewPictureBox.Cursor = handle != ResizeHandle.None ? GetResizeCursor(handle) : Cursors.SizeAll;
            }
            else
            {
                previewPictureBox.Cursor = Cursors.Default;
            }
        }
    }

    private void PreviewPictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        previewPictureBox.Cursor = Cursors.Default;
    }

    private QuadrantRegion? GetQuadrantAtPoint(Point point)
    {
        var scale = GetPreviewScale();
        var offsetX = (previewPictureBox.Width - _screenResolution.Width * scale) / 2;
        var offsetY = (previewPictureBox.Height - _screenResolution.Height * scale) / 2;

        var screenPoint = new Point(
            (int)((point.X - offsetX) / scale),
            (int)((point.Y - offsetY) / scale));

        return _currentConfiguration.Quadrants.FirstOrDefault(q => q.Bounds.Contains(screenPoint));
    }

    private float GetPreviewScale()
    {
        var scaleX = (float)previewPictureBox.Width / _screenResolution.Width;
        var scaleY = (float)previewPictureBox.Height / _screenResolution.Height;
        return Math.Min(scaleX, scaleY);
    }
    
    private ResizeHandle GetResizeHandle(Point point, QuadrantRegion quadrant)
    {
        var scale = GetPreviewScale();
        var offsetX = (previewPictureBox.Width - _screenResolution.Width * scale) / 2;
        var offsetY = (previewPictureBox.Height - _screenResolution.Height * scale) / 2;
        
        var scaledBounds = new RectangleF(
            quadrant.Bounds.X * scale + offsetX,
            quadrant.Bounds.Y * scale + offsetY,
            quadrant.Bounds.Width * scale,
            quadrant.Bounds.Height * scale);
            
        const int handleSize = 8;
        
        // Check corners first
        if (IsInHandle(point, new PointF(scaledBounds.Left, scaledBounds.Top), handleSize))
            return ResizeHandle.TopLeft;
        if (IsInHandle(point, new PointF(scaledBounds.Right, scaledBounds.Top), handleSize))
            return ResizeHandle.TopRight;
        if (IsInHandle(point, new PointF(scaledBounds.Left, scaledBounds.Bottom), handleSize))
            return ResizeHandle.BottomLeft;
        if (IsInHandle(point, new PointF(scaledBounds.Right, scaledBounds.Bottom), handleSize))
            return ResizeHandle.BottomRight;
            
        // Check edges
        if (IsInHandle(point, new PointF(scaledBounds.Left + scaledBounds.Width / 2, scaledBounds.Top), handleSize))
            return ResizeHandle.Top;
        if (IsInHandle(point, new PointF(scaledBounds.Right, scaledBounds.Top + scaledBounds.Height / 2), handleSize))
            return ResizeHandle.Right;
        if (IsInHandle(point, new PointF(scaledBounds.Left + scaledBounds.Width / 2, scaledBounds.Bottom), handleSize))
            return ResizeHandle.Bottom;
        if (IsInHandle(point, new PointF(scaledBounds.Left, scaledBounds.Top + scaledBounds.Height / 2), handleSize))
            return ResizeHandle.Left;
            
        return ResizeHandle.None;
    }
    
    private bool IsInHandle(Point point, PointF handleCenter, int handleSize)
    {
        var halfSize = handleSize / 2f;
        return point.X >= handleCenter.X - halfSize && point.X <= handleCenter.X + halfSize &&
               point.Y >= handleCenter.Y - halfSize && point.Y <= handleCenter.Y + halfSize;
    }
    
    private Cursor GetResizeCursor(ResizeHandle handle)
    {
        return handle switch
        {
            ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
            ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
            ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
            ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
            _ => Cursors.Default
        };
    }
    
    private Rectangle CalculateResizedBounds(Rectangle originalBounds, int deltaX, int deltaY, ResizeHandle handle)
    {
        var newBounds = originalBounds;
        
        switch (handle)
        {
            case ResizeHandle.TopLeft:
                newBounds.X += deltaX;
                newBounds.Y += deltaY;
                newBounds.Width -= deltaX;
                newBounds.Height -= deltaY;
                break;
            case ResizeHandle.TopRight:
                newBounds.Y += deltaY;
                newBounds.Width += deltaX;
                newBounds.Height -= deltaY;
                break;
            case ResizeHandle.BottomLeft:
                newBounds.X += deltaX;
                newBounds.Width -= deltaX;
                newBounds.Height += deltaY;
                break;
            case ResizeHandle.BottomRight:
                newBounds.Width += deltaX;
                newBounds.Height += deltaY;
                break;
            case ResizeHandle.Top:
                newBounds.Y += deltaY;
                newBounds.Height -= deltaY;
                break;
            case ResizeHandle.Right:
                newBounds.Width += deltaX;
                break;
            case ResizeHandle.Bottom:
                newBounds.Height += deltaY;
                break;
            case ResizeHandle.Left:
                newBounds.X += deltaX;
                newBounds.Width -= deltaX;
                break;
        }
        
        return newBounds;
    }
    
    private Rectangle ClampBounds(Rectangle bounds)
    {
        // Minimum size constraints
        const int minSize = 50;
        
        var newBounds = bounds;
        
        // Ensure minimum size
        if (newBounds.Width < minSize) newBounds.Width = minSize;
        if (newBounds.Height < minSize) newBounds.Height = minSize;
        
        // Ensure bounds are within screen
        if (newBounds.X < 0)
        {
            newBounds.Width += newBounds.X;
            newBounds.X = 0;
        }
        if (newBounds.Y < 0)
        {
            newBounds.Height += newBounds.Y;
            newBounds.Y = 0;
        }
        if (newBounds.Right > _screenResolution.Width)
            newBounds.Width = _screenResolution.Width - newBounds.X;
        if (newBounds.Bottom > _screenResolution.Height)
            newBounds.Height = _screenResolution.Height - newBounds.Y;
            
        return newBounds;
    }
    
    private void UpdateSelectedQuadrantInList()
    {
        if (_selectedQuadrant != null)
        {
            var index = _currentConfiguration.Quadrants.IndexOf(_selectedQuadrant);
            if (index >= 0 && index < lstQuadrants.Items.Count)
            {
                lstQuadrants.SelectedIndex = index;
            }
        }
    }
    
    private void UpdateCoordinateLabels()
    {
        // This could be enhanced to show real-time coordinates in the UI
        // For now, just update the form title to show coordinates
        if (_selectedQuadrant != null)
        {
            this.Text = $"Editor de Cuadrantes - {_selectedQuadrant.Name} ({_selectedQuadrant.Bounds.X}, {_selectedQuadrant.Bounds.Y}, {_selectedQuadrant.Bounds.Width}x{_selectedQuadrant.Bounds.Height})";
        }
        else
        {
            this.Text = "Editor de Cuadrantes - Capturer v3.2.0";
        }
    }
    
    private async Task UpdateScreenResolutionFromImagesAsync()
    {
        try
        {
            await LoadConfigurationAsync();
            var detectedSize = await DetectImageResolutionAsync();
            
            // Don't auto-update resolution anymore - only use selected preview image resolution
            // This prevents unwanted movement during processing
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error detecting image resolution: {ex.Message}");
        }
    }
    
    private Task<Size?> DetectImageResolutionAsync()
    {
        try
        {
            var screenshotFolder = _config.Storage?.ScreenshotFolder;
            if (string.IsNullOrEmpty(screenshotFolder) || !Directory.Exists(screenshotFolder))
                return Task.FromResult<Size?>(null);
            
            // Get recent image files
            var imageFiles = Directory.GetFiles(screenshotFolder, "*.png")
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .Take(10)
                .ToList();
            
            if (!imageFiles.Any())
                return Task.FromResult<Size?>(null);
            
            // Analyze resolution from multiple images to get the most common one
            var resolutions = new Dictionary<Size, int>();
            
            foreach (var imagePath in imageFiles)
            {
                try
                {
                    using var img = Image.FromFile(imagePath);
                    var size = new Size(img.Width, img.Height);
                    
                    if (resolutions.ContainsKey(size))
                        resolutions[size]++;
                    else
                        resolutions[size] = 1;
                }
                catch
                {
                    // Skip problematic images
                    continue;
                }
            }
            
            if (resolutions.Any())
            {
                // Return the most common resolution
                var mostCommon = resolutions.OrderByDescending(kvp => kvp.Value).First();
                return Task.FromResult<Size?>(mostCommon.Key);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DetectImageResolutionAsync: {ex.Message}");
        }
        
        return Task.FromResult<Size?>(null);
    }
    
    private Task<Size?> AnalyzeImageResolutionForDateRange(DateTime startDate, DateTime endDate)
    {
        try
        {
            var screenshotFolder = _config.Storage?.ScreenshotFolder;
            if (string.IsNullOrEmpty(screenshotFolder) || !Directory.Exists(screenshotFolder))
                return Task.FromResult<Size?>(null);
            
            var imageFiles = Directory.GetFiles(screenshotFolder, "*.png")
                .Where(f => 
                {
                    if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(f), 
                        "yyyy-MM-dd_HH-mm-ss", null, System.Globalization.DateTimeStyles.None, out var fileDate))
                    {
                        return fileDate >= startDate && fileDate <= endDate;
                    }
                    return false;
                })
                .Take(5) // Sample first 5 images for speed
                .ToList();
            
            if (!imageFiles.Any())
                return Task.FromResult<Size?>(null);
            
            // Get resolution from first valid image
            foreach (var imagePath in imageFiles)
            {
                try
                {
                    using var img = Image.FromFile(imagePath);
                    return Task.FromResult<Size?>(new Size(img.Width, img.Height));
                }
                catch
                {
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing image resolution for date range: {ex.Message}");
        }
        
        return Task.FromResult<Size?>(null);
    }
    
    #region Preview Image Selection Methods
    
    private void BtnRefreshImages_Click(object? sender, EventArgs e)
    {
        LoadAvailableImages();
    }
    
    private void CmbPreviewImage_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbPreviewImage.SelectedItem is ImageInfo selectedImage)
        {
            LoadPreviewImage(selectedImage.FilePath);
        }
    }
    
    private void LoadAvailableImages()
    {
        try
        {
            var screenshotFolder = _config.Storage?.ScreenshotFolder;
            if (string.IsNullOrEmpty(screenshotFolder) || !Directory.Exists(screenshotFolder))
            {
                cmbPreviewImage.Items.Clear();
                cmbPreviewImage.Items.Add("No hay imágenes disponibles");
                return;
            }
            
            // Get recent screenshot files
            var imageFiles = Directory.GetFiles(screenshotFolder, "*.png")
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .Take(50) // Limit to most recent 50 images
                .ToList();
                
            cmbPreviewImage.Items.Clear();
            
            if (!imageFiles.Any())
            {
                cmbPreviewImage.Items.Add("No hay imágenes disponibles");
                return;
            }
            
            // Group images by resolution for better organization
            var imageGroups = new Dictionary<Size, List<ImageInfo>>();
            
            foreach (var imagePath in imageFiles)
            {
                try
                {
                    using var img = Image.FromFile(imagePath);
                    var size = new Size(img.Width, img.Height);
                    var fileInfo = new FileInfo(imagePath);
                    
                    var imageInfo = new ImageInfo
                    {
                        FilePath = imagePath,
                        FileName = Path.GetFileName(imagePath),
                        Resolution = size,
                        FileSize = fileInfo.Length,
                        CreationTime = fileInfo.CreationTime
                    };
                    
                    if (!imageGroups.ContainsKey(size))
                        imageGroups[size] = new List<ImageInfo>();
                        
                    imageGroups[size].Add(imageInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading image info for {imagePath}: {ex.Message}");
                }
            }
            
            // Add items grouped by resolution, most common resolutions first
            foreach (var group in imageGroups.OrderByDescending(g => g.Value.Count))
            {
                var resolution = group.Key;
                var images = group.Value.OrderByDescending(i => i.CreationTime).ToList();
                
                // Add a separator for each resolution group
                if (cmbPreviewImage.Items.Count > 0)
                    cmbPreviewImage.Items.Add($"--- {resolution.Width}x{resolution.Height} ({images.Count} imágenes) ---");
                else
                    cmbPreviewImage.Items.Add($"--- {resolution.Width}x{resolution.Height} ({images.Count} imágenes) ---");
                
                foreach (var image in images)
                {
                    cmbPreviewImage.Items.Add(image);
                }
            }
            
            // Select the first actual image (not separator)
            if (cmbPreviewImage.Items.Count > 1)
            {
                for (int i = 0; i < cmbPreviewImage.Items.Count; i++)
                {
                    if (cmbPreviewImage.Items[i] is ImageInfo)
                    {
                        cmbPreviewImage.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading available images: {ex.Message}");
            MessageBox.Show($"Error cargando imágenes disponibles: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    
    private async Task LoadPreviewImage(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                MessageBox.Show("La imagen seleccionada no existe.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            using var sourceImage = new Bitmap(imagePath);
            
            // Dispose previous image
            _previewImage?.Dispose();
            _previewImage = new Bitmap(sourceImage);
            
            // Update screen resolution based on selected image
            _screenResolution = new Size(sourceImage.Width, sourceImage.Height);
            
            // Update resolution display
            var lblResolutionValue = this.Controls.Find("lblResolutionValue", true).FirstOrDefault() as Label;
            if (lblResolutionValue != null)
            {
                lblResolutionValue.Text = $"{_screenResolution.Width}x{_screenResolution.Height}";
            }
            
            // Update window title
            this.Text = $"Editor de Cuadrantes - Resolución: {_screenResolution.Width}x{_screenResolution.Height} - {Path.GetFileName(imagePath)}";
            
            // Update preview image
            previewPictureBox.Image = new Bitmap(_previewImage);
            
            // Update current configuration for new resolution if needed
            if (_currentConfiguration != null && 
                (_currentConfiguration.ScreenResolution.Width != _screenResolution.Width ||
                 _currentConfiguration.ScreenResolution.Height != _screenResolution.Height))
            {
                await HandleResolutionChangeAsync(_currentConfiguration.ScreenResolution, _screenResolution);
                
                // Always update configuration resolution to match the preview image
                _currentConfiguration.ScreenResolution = _screenResolution;
            }
            
            // Refresh the preview
            previewPictureBox.Invalidate();
            
            Console.WriteLine($"[QuadrantEditorForm] Loaded preview image: {imagePath} ({_screenResolution.Width}x{_screenResolution.Height})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading preview image {imagePath}: {ex.Message}");
            MessageBox.Show($"Error cargando la imagen: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private void AdjustQuadrantsToNewResolution(Size oldResolution, Size newResolution)
    {
        if (_currentConfiguration?.Quadrants == null) return;
        
        var scaleX = (float)newResolution.Width / oldResolution.Width;
        var scaleY = (float)newResolution.Height / oldResolution.Height;
        
        foreach (var quadrant in _currentConfiguration.Quadrants)
        {
            var oldBounds = quadrant.Bounds;
            quadrant.Bounds = new Rectangle(
                (int)(oldBounds.X * scaleX),
                (int)(oldBounds.Y * scaleY),
                (int)(oldBounds.Width * scaleX),
                (int)(oldBounds.Height * scaleY)
            );
        }
        
        Console.WriteLine($"[QuadrantEditorForm] Adjusted quadrants from {oldResolution.Width}x{oldResolution.Height} to {newResolution.Width}x{newResolution.Height}");
        Console.WriteLine($"[QuadrantEditorForm] Scale factors: X={scaleX:F2}, Y={scaleY:F2}");
    }
    
    #endregion
    
    #region Helper Classes
    
    private class ImageInfo
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public Size Resolution { get; set; }
        public long FileSize { get; set; }
        public DateTime CreationTime { get; set; }
        
        public override string ToString()
        {
            var fileSizeKB = FileSize / 1024;
            return $"{FileName} - {Resolution.Width}x{Resolution.Height} ({fileSizeKB:N0} KB) - {CreationTime:yyyy-MM-dd HH:mm}";
        }
    }
    
    #endregion

    /// <summary>
    /// Maneja el cambio de resolución respetando las preferencias del usuario
    /// </summary>
    private async Task HandleResolutionChangeAsync(Size oldResolution, Size newResolution)
    {
        try
        {
            // Cargar la configuración actual para verificar las preferencias
            await LoadConfigurationAsync();
            
            var quadrantSettings = _config.QuadrantSystem;
            
            // Si el usuario ha configurado no preguntar más, aplicar la preferencia guardada
            if (quadrantSettings.RememberResolutionChoice && quadrantSettings.ResolutionHandling != "ask")
            {
                switch (quadrantSettings.ResolutionHandling)
                {
                    case "auto-adjust":
                        AdjustQuadrantsToNewResolution(oldResolution, newResolution);
                        ShowNotificationMessage("Cuadrantes ajustados automáticamente según preferencia guardada.", "Ajuste automático");
                        break;
                    case "keep-current":
                        ShowNotificationMessage("Posiciones mantenidas según preferencia guardada. Resolución de referencia actualizada.", "Posiciones mantenidas");
                        break;
                }
                return;
            }
            
            // Si llegamos aquí, necesitamos preguntar al usuario (primera vez o configurado para preguntar siempre)
            var dialogForm = new ResolutionChoiceDialog(oldResolution, newResolution);
            var result = dialogForm.ShowDialog(this);
            
            if (result == DialogResult.OK)
            {
                var choice = dialogForm.UserChoice;
                var rememberChoice = dialogForm.RememberChoice;
                
                // Aplicar la elección del usuario
                switch (choice)
                {
                    case "auto-adjust":
                        AdjustQuadrantsToNewResolution(oldResolution, newResolution);
                        ShowNotificationMessage("Cuadrantes ajustados automáticamente.", "Ajuste completado");
                        break;
                    case "keep-current":
                        ShowNotificationMessage("Posiciones mantenidas. Resolución de referencia actualizada.", "Posiciones mantenidas");
                        break;
                }
                
                // Guardar la preferencia si el usuario lo solicitó
                if (rememberChoice)
                {
                    quadrantSettings.RememberResolutionChoice = true;
                    quadrantSettings.ResolutionHandling = choice;
                    await _configManager.SaveConfigurationAsync(_config);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling resolution change: {ex.Message}");
            // En caso de error, mantener las posiciones actuales
            ShowNotificationMessage("Error procesando cambio de resolución. Posiciones mantenidas.", "Error", true);
        }
    }
    
    /// <summary>
    /// Muestra un mensaje de notificación menos intrusivo
    /// </summary>
    private void ShowNotificationMessage(string message, string title, bool isError = false)
    {
        // Usar un mensaje más sutil en lugar de MessageBox molesto
        var icon = isError ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
        
        // Solo mostrar si realmente es importante
        if (isError || message.Contains("error"))
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
        }
        else
        {
            // Para mensajes informativos, solo log en consola
            Console.WriteLine($"[QuadrantEditor] {title}: {message}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewImage?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        base.Dispose(disposing);
    }
}