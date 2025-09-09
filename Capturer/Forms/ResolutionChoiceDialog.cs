using System.Drawing;
using System.Windows.Forms;

namespace Capturer.Forms;

/// <summary>
/// Diálogo mejorado para manejar cambios de resolución sin ser molesto
/// </summary>
public partial class ResolutionChoiceDialog : Form
{
    private readonly Size _oldResolution;
    private readonly Size _newResolution;
    
    public string UserChoice { get; private set; } = "keep-current";
    public bool RememberChoice { get; private set; } = false;
    
    // UI Controls
    private RadioButton _radioAutoAdjust = new();
    private RadioButton _radioKeepCurrent = new();
    private CheckBox _checkRememberChoice = new();
    private Button _btnOK = new();
    private Button _btnCancel = new();
    private Label _lblTitle = new();
    private Label _lblDescription = new();
    
    public ResolutionChoiceDialog(Size oldResolution, Size newResolution)
    {
        _oldResolution = oldResolution;
        _newResolution = newResolution;
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        SuspendLayout();
        
        // Form configuration
        Text = "Resolución Diferente Detectada";
        Size = new Size(500, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        
        // Title label
        _lblTitle.Text = "¿Cómo desea manejar el cambio de resolución?";
        _lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _lblTitle.Location = new Point(20, 20);
        _lblTitle.Size = new Size(450, 25);
        _lblTitle.ForeColor = Color.DarkBlue;
        
        // Description label  
        _lblDescription.Text = $"Resolución anterior: {_oldResolution.Width}x{_oldResolution.Height}\n" +
                              $"Resolución nueva: {_newResolution.Width}x{_newResolution.Height}";
        _lblDescription.Font = new Font("Segoe UI", 9F);
        _lblDescription.Location = new Point(20, 50);
        _lblDescription.Size = new Size(450, 35);
        _lblDescription.ForeColor = Color.DarkGray;
        
        // Auto-adjust option
        _radioAutoAdjust.Text = "Ajustar automáticamente los cuadrantes";
        _radioAutoAdjust.Font = new Font("Segoe UI", 10F);
        _radioAutoAdjust.Location = new Point(30, 100);
        _radioAutoAdjust.Size = new Size(400, 23);
        _radioAutoAdjust.Checked = false;
        _radioAutoAdjust.CheckedChanged += (s, e) => { if (_radioAutoAdjust.Checked) UserChoice = "auto-adjust"; };
        
        var lblAutoAdjustDesc = new Label
        {
            Text = "Los cuadrantes se reescalarán proporcionalmente a la nueva resolución.",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
            Location = new Point(50, 125),
            Size = new Size(420, 15),
            ForeColor = Color.Gray
        };
        
        // Keep current option
        _radioKeepCurrent.Text = "Mantener posiciones exactas de los cuadrantes";
        _radioKeepCurrent.Font = new Font("Segoe UI", 10F);
        _radioKeepCurrent.Location = new Point(30, 150);
        _radioKeepCurrent.Size = new Size(400, 23);
        _radioKeepCurrent.Checked = true; // Por defecto
        _radioKeepCurrent.CheckedChanged += (s, e) => { if (_radioKeepCurrent.Checked) UserChoice = "keep-current"; };
        
        var lblKeepCurrentDesc = new Label
        {
            Text = "Los cuadrantes mantienen sus coordenadas exactas (recomendado).",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
            Location = new Point(50, 175),
            Size = new Size(420, 15),
            ForeColor = Color.Gray
        };
        
        // Remember choice checkbox
        _checkRememberChoice.Text = "Recordar mi elección y no preguntar más";
        _checkRememberChoice.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _checkRememberChoice.Location = new Point(30, 200);
        _checkRememberChoice.Size = new Size(300, 23);
        _checkRememberChoice.ForeColor = Color.DarkGreen;
        _checkRememberChoice.CheckedChanged += (s, e) => RememberChoice = _checkRememberChoice.Checked;
        
        // OK button
        _btnOK.Text = "Aplicar";
        _btnOK.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnOK.Size = new Size(80, 30);
        _btnOK.Location = new Point(310, 235);
        _btnOK.BackColor = Color.FromArgb(0, 120, 215);
        _btnOK.ForeColor = Color.White;
        _btnOK.FlatStyle = FlatStyle.Flat;
        _btnOK.FlatAppearance.BorderSize = 0;
        _btnOK.DialogResult = DialogResult.OK;
        _btnOK.Click += BtnOK_Click;
        
        // Cancel button
        _btnCancel.Text = "Cancelar";
        _btnCancel.Font = new Font("Segoe UI", 9F);
        _btnCancel.Size = new Size(80, 30);
        _btnCancel.Location = new Point(400, 235);
        _btnCancel.BackColor = Color.Gray;
        _btnCancel.ForeColor = Color.White;
        _btnCancel.FlatStyle = FlatStyle.Flat;
        _btnCancel.FlatAppearance.BorderSize = 0;
        _btnCancel.DialogResult = DialogResult.Cancel;
        
        // Add controls to form
        Controls.AddRange(new Control[] 
        {
            _lblTitle,
            _lblDescription,
            _radioAutoAdjust,
            lblAutoAdjustDesc,
            _radioKeepCurrent,
            lblKeepCurrentDesc,
            _checkRememberChoice,
            _btnOK,
            _btnCancel
        });
        
        AcceptButton = _btnOK;
        CancelButton = _btnCancel;
        
        ResumeLayout(false);
    }
    
    private void BtnOK_Click(object? sender, EventArgs e)
    {
        // Determinar la elección final
        if (_radioAutoAdjust.Checked)
            UserChoice = "auto-adjust";
        else
            UserChoice = "keep-current";
            
        RememberChoice = _checkRememberChoice.Checked;
        
        DialogResult = DialogResult.OK;
        Close();
    }
}