using Capturer.Models;

namespace Capturer.Forms;

public partial class QuadrantSelectionDialog : Form
{
    private List<string> _availableQuadrants;
    private List<string> _selectedQuadrants;
    private CheckedListBox clbQuadrants;
    private Button btnOk;
    private Button btnCancel;
    private Button btnSelectAll;
    private Button btnSelectNone;
    private Label lblInfo;

    public List<string> SelectedQuadrants => _selectedQuadrants.ToList();

    public QuadrantSelectionDialog(List<string> availableQuadrants, List<string>? preSelectedQuadrants = null)
    {
        _availableQuadrants = availableQuadrants ?? new List<string>();
        _selectedQuadrants = preSelectedQuadrants?.ToList() ?? new List<string>();
        
        InitializeComponent();
        LoadQuadrants();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(450, 350);
        this.Text = "🔲 Selección de Cuadrantes - Capturer v2.4";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading form icon: {ex.Message}");
        }

        // Info label
        lblInfo = new Label
        {
            Text = "Seleccione los cuadrantes que desea incluir en el reporte:",
            Location = new Point(15, 15),
            Size = new Size(420, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 37, 41)
        };
        this.Controls.Add(lblInfo);

        // CheckedListBox for quadrants
        clbQuadrants = new CheckedListBox
        {
            Location = new Point(15, 45),
            Size = new Size(420, 180),
            CheckOnClick = true,
            Font = new Font("Segoe UI", 9F),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        this.Controls.Add(clbQuadrants);

        // Quick selection buttons
        btnSelectAll = CreateModernButton("✓ Seleccionar Todos", new Point(15, 235), new Size(130, 30), Color.FromArgb(40, 167, 69));
        btnSelectNone = CreateModernButton("✗ Deseleccionar Todos", new Point(155, 235), new Size(130, 30), Color.FromArgb(220, 53, 69));
        
        btnSelectAll.Click += (s, e) => SelectAllQuadrants(true);
        btnSelectNone.Click += (s, e) => SelectAllQuadrants(false);
        
        this.Controls.Add(btnSelectAll);
        this.Controls.Add(btnSelectNone);

        // Dialog buttons
        btnOk = CreateModernButton("✅ Aceptar", new Point(255, 275), new Size(85, 35), Color.FromArgb(0, 123, 255));
        btnCancel = CreateModernButton("❌ Cancelar", new Point(350, 275), new Size(85, 35), Color.FromArgb(108, 117, 125));

        btnOk.Click += BtnOk_Click;
        btnCancel.Click += BtnCancel_Click;

        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);

        // Set accept/cancel buttons
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }

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
            FlatAppearance = { 
                BorderSize = 0, 
                MouseOverBackColor = Color.FromArgb(
                    Math.Max(0, backgroundColor.R - 20), 
                    Math.Max(0, backgroundColor.G - 20), 
                    Math.Max(0, backgroundColor.B - 20)
                ) 
            },
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
    }

    private void LoadQuadrants()
    {
        clbQuadrants.Items.Clear();
        
        foreach (var quadrant in _availableQuadrants)
        {
            var isSelected = _selectedQuadrants.Contains(quadrant);
            clbQuadrants.Items.Add(quadrant, isSelected);
        }

        // Update info label with count
        UpdateInfoLabel();
    }

    private void SelectAllQuadrants(bool select)
    {
        for (int i = 0; i < clbQuadrants.Items.Count; i++)
        {
            clbQuadrants.SetItemChecked(i, select);
        }
        UpdateInfoLabel();
    }

    private void UpdateInfoLabel()
    {
        var selectedCount = clbQuadrants.CheckedItems.Count;
        var totalCount = clbQuadrants.Items.Count;
        
        if (selectedCount == 0)
        {
            lblInfo.Text = "Seleccione los cuadrantes que desea incluir en el reporte:";
            lblInfo.ForeColor = Color.FromArgb(33, 37, 41);
        }
        else
        {
            lblInfo.Text = $"Seleccionados: {selectedCount} de {totalCount} cuadrantes disponibles";
            lblInfo.ForeColor = Color.FromArgb(40, 167, 69);
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        _selectedQuadrants.Clear();
        _selectedQuadrants.AddRange(clbQuadrants.CheckedItems.Cast<string>());
        
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // Add item check event after form is loaded
        clbQuadrants.ItemCheck += (s, e) => 
        {
            // Use BeginInvoke to update info after the check state changes
            this.BeginInvoke(new Action(UpdateInfoLabel));
        };
    }
}