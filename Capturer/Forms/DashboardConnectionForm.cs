using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Capturer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Capturer.Forms;

/// <summary>
/// Form for connecting Capturer to Dashboard Web using invitation links
/// </summary>
public partial class DashboardConnectionForm : Form
{
    private readonly IDashboardInvitationService _invitationService;
    private readonly ILogger<DashboardConnectionForm> _logger;
    private readonly IServiceProvider _serviceProvider;

    // UI Controls
    private TextBox txtInvitationUrl;
    private TextBox txtComputerName;
    private Button btnConnect;
    private Button btnTestConnection;
    private Button btnCancel;
    private Label lblStatus;
    private ProgressBar progressBar;
    private RichTextBox txtInstructions;
    private Panel pnlApprovalWaiting;
    private Label lblApprovalMessage;
    private Button btnCheckApproval;
    private System.Windows.Forms.Timer approvalCheckTimer;

    public DashboardConnectionForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _invitationService = serviceProvider.GetRequiredService<IDashboardInvitationService>();
        _logger = serviceProvider.GetRequiredService<ILogger<DashboardConnectionForm>>();
        
        InitializeComponent();
        SetupForm();
    }

    private void SetupForm()
    {
        this.Text = "Connect to Dashboard Web";
        this.Size = new Size(600, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // Create controls
        CreateControls();
        LayoutControls();
        SetupEventHandlers();
        
        // Set default computer name
        txtComputerName.Text = Environment.MachineName;
    }

    private void CreateControls()
    {
        // Instructions
        txtInstructions = new RichTextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Control,
            Font = new Font("Segoe UI", 9F),
            Text = "📋 Instructions:\n\n" +
                   "1. Ask your administrator for an invitation link\n" +
                   "2. Paste the invitation URL below\n" +
                   "3. Choose a descriptive name for this computer\n" +
                   "4. Click 'Connect to Dashboard'\n\n" +
                   "⚠️ Note: Some organizations require manual approval. " +
                   "You may need to wait for your administrator to approve this computer."
        };

        // Invitation URL
        var lblInvitationUrl = new Label
        {
            Text = "🔗 Invitation URL:",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        txtInvitationUrl = new TextBox
        {
            PlaceholderText = "http://dashboard.company.com/invite/abc123...",
            Font = new Font("Segoe UI", 9F),
            Width = 450
        };

        // Computer Name
        var lblComputerName = new Label
        {
            Text = "💻 Computer Name:",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        txtComputerName = new TextBox
        {
            PlaceholderText = "e.g., Juan-Office-PC",
            Font = new Font("Segoe UI", 9F),
            Width = 300
        };

        // Buttons
        btnTestConnection = new Button
        {
            Text = "🔍 Test Connection",
            Size = new Size(130, 30),
            UseVisualStyleBackColor = true
        };

        btnConnect = new Button
        {
            Text = "🔗 Connect to Dashboard",
            Size = new Size(150, 35),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 123, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };

        // Status and Progress
        lblStatus = new Label
        {
            Text = "Ready to connect",
            AutoSize = true,
            ForeColor = Color.Blue
        };

        progressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            Visible = false,
            Width = 400
        };

        // Approval waiting panel
        pnlApprovalWaiting = new Panel
        {
            Visible = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.LightYellow,
            Height = 100
        };

        lblApprovalMessage = new Label
        {
            Text = "⏳ Waiting for administrator approval...",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.Orange,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        };

        btnCheckApproval = new Button
        {
            Text = "🔄 Check Approval Status",
            Size = new Size(150, 25),
            UseVisualStyleBackColor = true
        };

        // Timer for approval checking
        approvalCheckTimer = new System.Windows.Forms.Timer
        {
            Interval = 10000 // Check every 10 seconds
        };
    }

    private void LayoutControls()
    {
        var padding = 20;
        var yPos = padding;

        // Instructions
        txtInstructions.Location = new Point(padding, yPos);
        txtInstructions.Size = new Size(this.Width - padding * 2 - 20, 120);
        this.Controls.Add(txtInstructions);
        yPos += txtInstructions.Height + 15;

        // Invitation URL
        var lblInvitationUrl = new Label { Text = "🔗 Invitation URL:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        lblInvitationUrl.Location = new Point(padding, yPos);
        this.Controls.Add(lblInvitationUrl);
        yPos += lblInvitationUrl.Height + 5;

        txtInvitationUrl.Location = new Point(padding, yPos);
        txtInvitationUrl.Width = this.Width - padding * 2 - 20;
        this.Controls.Add(txtInvitationUrl);
        yPos += txtInvitationUrl.Height + 15;

        // Computer Name
        var lblComputerName = new Label { Text = "💻 Computer Name:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        lblComputerName.Location = new Point(padding, yPos);
        this.Controls.Add(lblComputerName);
        yPos += lblComputerName.Height + 5;

        txtComputerName.Location = new Point(padding, yPos);
        this.Controls.Add(txtComputerName);

        btnTestConnection.Location = new Point(txtComputerName.Right + 10, yPos - 2);
        this.Controls.Add(btnTestConnection);
        yPos += txtComputerName.Height + 20;

        // Status
        lblStatus.Location = new Point(padding, yPos);
        this.Controls.Add(lblStatus);
        yPos += lblStatus.Height + 5;

        // Progress bar
        progressBar.Location = new Point(padding, yPos);
        progressBar.Width = this.Width - padding * 2 - 20;
        this.Controls.Add(progressBar);
        yPos += progressBar.Height + 15;

        // Approval waiting panel
        pnlApprovalWaiting.Location = new Point(padding, yPos);
        pnlApprovalWaiting.Width = this.Width - padding * 2 - 20;
        
        lblApprovalMessage.Location = new Point(10, 10);
        lblApprovalMessage.Width = pnlApprovalWaiting.Width - 20;
        lblApprovalMessage.Height = 30;
        pnlApprovalWaiting.Controls.Add(lblApprovalMessage);

        btnCheckApproval.Location = new Point((pnlApprovalWaiting.Width - btnCheckApproval.Width) / 2, 45);
        pnlApprovalWaiting.Controls.Add(btnCheckApproval);
        
        this.Controls.Add(pnlApprovalWaiting);
        yPos += pnlApprovalWaiting.Height + 20;

        // Buttons
        btnConnect.Location = new Point(this.Width - btnConnect.Width - padding - 15, this.Height - 70);
        btnCancel.Location = new Point(btnConnect.Left - btnCancel.Width - 10, btnConnect.Top + 2);
        
        this.Controls.Add(btnConnect);
        this.Controls.Add(btnCancel);
    }

    private void SetupEventHandlers()
    {
        btnConnect.Click += async (s, e) => await ConnectToDashboard();
        btnTestConnection.Click += async (s, e) => await TestConnection();
        btnCheckApproval.Click += async (s, e) => await CheckApprovalStatus();
        
        txtInvitationUrl.TextChanged += (s, e) => ValidateInput();
        txtComputerName.TextChanged += (s, e) => ValidateInput();

        approvalCheckTimer.Tick += async (s, e) => await CheckApprovalStatus();
    }

    private async Task TestConnection()
    {
        if (string.IsNullOrWhiteSpace(txtInvitationUrl.Text))
        {
            MessageBox.Show("Please enter an invitation URL first.", "Missing URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusyState(true, "Testing connection...");

        try
        {
            var result = await _invitationService.ValidateInvitationAsync(txtInvitationUrl.Text.Trim());

            if (result.IsValid)
            {
                SetStatusMessage($"✅ Connection successful! Organization: {result.OrganizationName}", Color.Green);
                
                if (result.RequireApproval)
                {
                    SetStatusMessage($"✅ Connection OK. Note: This organization requires manual approval.", Color.Orange);
                }
            }
            else
            {
                SetStatusMessage($"❌ Connection failed: {result.ErrorMessage}", Color.Red);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection");
            SetStatusMessage($"❌ Connection test failed: {ex.Message}", Color.Red);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async Task ConnectToDashboard()
    {
        if (!ValidateInput())
        {
            return;
        }

        SetBusyState(true, "Connecting to dashboard...");
        pnlApprovalWaiting.Visible = false;

        try
        {
            var invitationUrl = txtInvitationUrl.Text.Trim();
            var computerName = txtComputerName.Text.Trim();

            var result = await _invitationService.ProcessInvitationAsync(invitationUrl, computerName);

            if (!result.Success)
            {
                SetStatusMessage($"❌ Connection failed: {result.ErrorMessage}", Color.Red);
                return;
            }

            if (result.Status == "Approved")
            {
                SetStatusMessage("✅ Successfully connected to dashboard!", Color.Green);
                
                var successMessage = $"Computer '{computerName}' has been successfully connected to the dashboard!\n\n" +
                                   $"Dashboard URL: {result.DashboardUrl}\n" +
                                   $"Your computer will now sync data automatically.\n\n" +
                                   "You can close this window and continue using Capturer normally.";

                MessageBox.Show(successMessage, "Connection Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else if (result.Status == "PendingApproval")
            {
                SetStatusMessage("⏳ Registration submitted, waiting for approval...", Color.Orange);
                ShowApprovalWaitingPanel(result);
                StartApprovalChecking();
            }
            else
            {
                SetStatusMessage($"❓ Unexpected status: {result.Status}", Color.Orange);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to dashboard");
            SetStatusMessage($"❌ Connection error: {ex.Message}", Color.Red);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async Task CheckApprovalStatus()
    {
        // This would need to be implemented to poll the dashboard for approval status
        // For now, we'll just show a message
        SetStatusMessage("🔄 Checking approval status...", Color.Blue);
        
        // TODO: Implement actual approval status checking
        await Task.Delay(1000);
        
        SetStatusMessage("⏳ Still waiting for approval. Please contact your administrator.", Color.Orange);
    }

    private bool ValidateInput()
    {
        var isValid = true;
        var errorMessage = "";

        if (string.IsNullOrWhiteSpace(txtInvitationUrl.Text))
        {
            errorMessage = "Please enter an invitation URL";
            isValid = false;
        }
        else if (!Uri.TryCreate(txtInvitationUrl.Text.Trim(), UriKind.Absolute, out _))
        {
            errorMessage = "Please enter a valid URL";
            isValid = false;
        }
        else if (string.IsNullOrWhiteSpace(txtComputerName.Text))
        {
            errorMessage = "Please enter a computer name";
            isValid = false;
        }
        else if (txtComputerName.Text.Trim().Length < 3)
        {
            errorMessage = "Computer name must be at least 3 characters";
            isValid = false;
        }

        btnConnect.Enabled = isValid;
        btnTestConnection.Enabled = !string.IsNullOrWhiteSpace(txtInvitationUrl.Text);

        if (!isValid && !string.IsNullOrEmpty(errorMessage))
        {
            SetStatusMessage($"⚠️ {errorMessage}", Color.Red);
        }
        else if (isValid)
        {
            SetStatusMessage("Ready to connect", Color.Blue);
        }

        return isValid;
    }

    private void SetBusyState(bool isBusy, string? statusMessage = null)
    {
        btnConnect.Enabled = !isBusy;
        btnTestConnection.Enabled = !isBusy;
        txtInvitationUrl.Enabled = !isBusy;
        txtComputerName.Enabled = !isBusy;
        
        progressBar.Visible = isBusy;
        
        if (!string.IsNullOrEmpty(statusMessage))
        {
            SetStatusMessage(statusMessage, isBusy ? Color.Blue : Color.Black);
        }

        Application.DoEvents(); // Allow UI to update
    }

    private void SetStatusMessage(string message, Color color)
    {
        lblStatus.Text = message;
        lblStatus.ForeColor = color;
        Application.DoEvents();
    }

    private void ShowApprovalWaitingPanel(RegistrationResult result)
    {
        lblApprovalMessage.Text = $"⏳ Registration submitted for computer '{result.ComputerName}'.\n" +
                                 "Please wait for your administrator to approve this connection.";
        
        pnlApprovalWaiting.Visible = true;
    }

    private void StartApprovalChecking()
    {
        approvalCheckTimer.Start();
    }

    private void StopApprovalChecking()
    {
        approvalCheckTimer.Stop();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        StopApprovalChecking();
        base.OnFormClosed(e);
    }

    // Show connection dialog from main form
    public static DialogResult ShowConnectionDialog(IServiceProvider serviceProvider)
    {
        using var form = new DashboardConnectionForm(serviceProvider);
        return form.ShowDialog();
    }
}

// Partial class for designer generated code
public partial class DashboardConnectionForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            approvalCheckTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(584, 461);
    }

    #endregion
}