namespace Capturer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblNextCapture = new System.Windows.Forms.Label();
            this.lblTotalScreenshots = new System.Windows.Forms.Label();
            this.lblStorageUsed = new System.Windows.Forms.Label();
            this.lblLastEmail = new System.Windows.Forms.Label();
            this.btnStartCapture = new System.Windows.Forms.Button();
            this.btnStopCapture = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnSendEmail = new System.Windows.Forms.Button();
            this.btnRoutineEmail = new System.Windows.Forms.Button();
            this.btnCaptureNow = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnQuadrants = new System.Windows.Forms.Button();
            this.listViewScreenshots = new System.Windows.Forms.ListView();
            this.btnMinimizeToTray = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.showToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.captureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.groupBoxStatus = new System.Windows.Forms.GroupBox();
            this.groupBoxControls = new System.Windows.Forms.GroupBox();
            this.groupBoxRecent = new System.Windows.Forms.GroupBox();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.contextMenuStrip.SuspendLayout();
            this.groupBoxStatus.SuspendLayout();
            this.groupBoxControls.SuspendLayout();
            this.groupBoxRecent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblStatus.Location = new System.Drawing.Point(15, 25);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(142, 15);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Estado: Detenido";
            // 
            // lblNextCapture
            // 
            this.lblNextCapture.AutoSize = true;
            this.lblNextCapture.Location = new System.Drawing.Point(300, 25);
            this.lblNextCapture.Name = "lblNextCapture";
            this.lblNextCapture.Size = new System.Drawing.Size(125, 15);
            this.lblNextCapture.TabIndex = 1;
            this.lblNextCapture.Text = "Próxima: --:--:--";
            // 
            // lblTotalScreenshots
            // 
            this.lblTotalScreenshots.AutoSize = true;
            this.lblTotalScreenshots.Location = new System.Drawing.Point(15, 50);
            this.lblTotalScreenshots.Name = "lblTotalScreenshots";
            this.lblTotalScreenshots.Size = new System.Drawing.Size(125, 15);
            this.lblTotalScreenshots.TabIndex = 2;
            this.lblTotalScreenshots.Text = "Total Capturas: 0";
            // 
            // lblStorageUsed
            // 
            this.lblStorageUsed.AutoSize = true;
            this.lblStorageUsed.Location = new System.Drawing.Point(300, 50);
            this.lblStorageUsed.Name = "lblStorageUsed";
            this.lblStorageUsed.Size = new System.Drawing.Size(140, 15);
            this.lblStorageUsed.TabIndex = 3;
            this.lblStorageUsed.Text = "Almacenamiento: 0 MB";
            // 
            // lblLastEmail
            // 
            this.lblLastEmail.AutoSize = true;
            this.lblLastEmail.Location = new System.Drawing.Point(15, 75);
            this.lblLastEmail.Name = "lblLastEmail";
            this.lblLastEmail.Size = new System.Drawing.Size(130, 15);
            this.lblLastEmail.TabIndex = 4;
            this.lblLastEmail.Text = "Último Email: Nunca";
            // 
            // btnStartCapture
            // 
            this.btnStartCapture.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(167)))), ((int)(((byte)(69)))));
            this.btnStartCapture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartCapture.ForeColor = System.Drawing.Color.White;
            this.btnStartCapture.Location = new System.Drawing.Point(15, 25);
            this.btnStartCapture.Name = "btnStartCapture";
            this.btnStartCapture.Size = new System.Drawing.Size(100, 35);
            this.btnStartCapture.TabIndex = 6;
            this.btnStartCapture.Text = "Iniciar";
            this.btnStartCapture.UseVisualStyleBackColor = false;
            // 
            // btnStopCapture
            // 
            this.btnStopCapture.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.btnStopCapture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopCapture.ForeColor = System.Drawing.Color.White;
            this.btnStopCapture.Location = new System.Drawing.Point(125, 25);
            this.btnStopCapture.Name = "btnStopCapture";
            this.btnStopCapture.Size = new System.Drawing.Size(100, 35);
            this.btnStopCapture.TabIndex = 7;
            this.btnStopCapture.Text = "Detener";
            this.btnStopCapture.UseVisualStyleBackColor = false;
            // 
            // btnSettings
            // 
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.Location = new System.Drawing.Point(235, 25);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(100, 35);
            this.btnSettings.TabIndex = 8;
            this.btnSettings.Text = "Configuración";
            this.btnSettings.UseVisualStyleBackColor = false;
            // 
            // btnSendEmail
            // 
            this.btnSendEmail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(123)))), ((int)(((byte)(255)))));
            this.btnSendEmail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSendEmail.ForeColor = System.Drawing.Color.White;
            this.btnSendEmail.Location = new System.Drawing.Point(345, 25);
            this.btnSendEmail.Name = "btnSendEmail";
            this.btnSendEmail.Size = new System.Drawing.Size(100, 35);
            this.btnSendEmail.TabIndex = 9;
            this.btnSendEmail.Text = "Email Manual";
            this.btnSendEmail.UseVisualStyleBackColor = false;
            // 
            // btnRoutineEmail
            // 
            this.btnRoutineEmail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(43)))), ((int)(((byte)(226)))));
            this.btnRoutineEmail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRoutineEmail.ForeColor = System.Drawing.Color.White;
            this.btnRoutineEmail.Location = new System.Drawing.Point(345, 65);
            this.btnRoutineEmail.Name = "btnRoutineEmail";
            this.btnRoutineEmail.Size = new System.Drawing.Size(100, 35);
            this.btnRoutineEmail.TabIndex = 20;
            this.btnRoutineEmail.Text = "Reportes Automáticos";
            this.btnRoutineEmail.UseVisualStyleBackColor = false;
            // 
            // btnCaptureNow
            // 
            this.btnCaptureNow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(193)))), ((int)(((byte)(7)))));
            this.btnCaptureNow.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCaptureNow.ForeColor = System.Drawing.Color.Black;
            this.btnCaptureNow.Location = new System.Drawing.Point(455, 25);
            this.btnCaptureNow.Name = "btnCaptureNow";
            this.btnCaptureNow.Size = new System.Drawing.Size(120, 35);
            this.btnCaptureNow.TabIndex = 10;
            this.btnCaptureNow.Text = "Capturar Ahora";
            this.btnCaptureNow.UseVisualStyleBackColor = false;
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(162)))), ((int)(((byte)(184)))));
            this.btnOpenFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFolder.ForeColor = System.Drawing.Color.White;
            this.btnOpenFolder.Location = new System.Drawing.Point(585, 25);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(120, 35);
            this.btnOpenFolder.TabIndex = 11;
            this.btnOpenFolder.Text = "Abrir Carpeta";
            this.btnOpenFolder.UseVisualStyleBackColor = false;
            // 
            // btnQuadrants
            // 
            this.btnQuadrants.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(152)))), ((int)(((byte)(0)))));
            this.btnQuadrants.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnQuadrants.ForeColor = System.Drawing.Color.White;
            this.btnQuadrants.Location = new System.Drawing.Point(15, 70);
            this.btnQuadrants.Name = "btnQuadrants";
            this.btnQuadrants.Size = new System.Drawing.Size(120, 35);
            this.btnQuadrants.TabIndex = 12;
            this.btnQuadrants.Text = "Cuadrantes (Beta)";
            this.btnQuadrants.UseVisualStyleBackColor = false;
            // 
            // listViewScreenshots
            // 
            this.listViewScreenshots.Location = new System.Drawing.Point(15, 25);
            this.listViewScreenshots.Name = "listViewScreenshots";
            this.listViewScreenshots.Size = new System.Drawing.Size(690, 240);
            this.listViewScreenshots.TabIndex = 10;
            this.listViewScreenshots.UseCompatibleStateImageBehavior = false;
            this.listViewScreenshots.View = System.Windows.Forms.View.Details;
            // 
            // btnMinimizeToTray
            // 
            this.btnMinimizeToTray.Location = new System.Drawing.Point(480, 600);
            this.btnMinimizeToTray.Name = "btnMinimizeToTray";
            this.btnMinimizeToTray.Size = new System.Drawing.Size(120, 30);
            this.btnMinimizeToTray.TabIndex = 12;
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(610, 600);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(120, 30);
            this.btnExit.TabIndex = 13;
            this.btnExit.Text = "Salir";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnMinimizeToTray.Text = "Minimizar";
            this.btnMinimizeToTray.UseVisualStyleBackColor = true;
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.notifyIcon.Text = "Capturer";
            this.notifyIcon.Visible = true;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showToolStripMenuItem,
            this.captureToolStripMenuItem,
            new System.Windows.Forms.ToolStripSeparator(),
            this.exitToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(180, 76);
            // 
            // showToolStripMenuItem
            // 
            this.showToolStripMenuItem.Name = "showToolStripMenuItem";
            this.showToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.showToolStripMenuItem.Text = "Mostrar";
            // 
            // captureToolStripMenuItem
            // 
            this.captureToolStripMenuItem.Name = "captureToolStripMenuItem";
            this.captureToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.captureToolStripMenuItem.Text = "📸 Captura de Pantalla";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.exitToolStripMenuItem.Text = "Salir";
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 1000;
            // 
            // groupBoxStatus
            // 
            this.groupBoxStatus.Controls.Add(this.lblStatus);
            this.groupBoxStatus.Controls.Add(this.lblNextCapture);
            this.groupBoxStatus.Controls.Add(this.lblTotalScreenshots);
            this.groupBoxStatus.Controls.Add(this.lblStorageUsed);
            this.groupBoxStatus.Controls.Add(this.lblLastEmail);
            this.groupBoxStatus.Location = new System.Drawing.Point(15, 15);
            this.groupBoxStatus.Name = "groupBoxStatus";
            this.groupBoxStatus.Size = new System.Drawing.Size(720, 110);
            this.groupBoxStatus.TabIndex = 14;
            this.groupBoxStatus.TabStop = false;
            this.groupBoxStatus.Text = "Estado del Sistema";
            // 
            // groupBoxControls
            // 
            this.groupBoxControls.Controls.Add(this.btnStartCapture);
            this.groupBoxControls.Controls.Add(this.btnStopCapture);
            this.groupBoxControls.Controls.Add(this.btnSettings);
            this.groupBoxControls.Controls.Add(this.btnSendEmail);
            this.groupBoxControls.Controls.Add(this.btnRoutineEmail);
            this.groupBoxControls.Controls.Add(this.btnCaptureNow);
            this.groupBoxControls.Controls.Add(this.btnOpenFolder);
            this.groupBoxControls.Controls.Add(this.btnQuadrants);
            this.groupBoxControls.Location = new System.Drawing.Point(15, 140);
            this.groupBoxControls.Name = "groupBoxControls";
            this.groupBoxControls.Size = new System.Drawing.Size(720, 115);
            this.groupBoxControls.TabIndex = 15;
            this.groupBoxControls.TabStop = false;
            this.groupBoxControls.Text = "Controles";
            // 
            // groupBoxRecent
            // 
            this.groupBoxRecent.Controls.Add(this.listViewScreenshots);
            this.groupBoxRecent.Location = new System.Drawing.Point(15, 235);
            this.groupBoxRecent.Name = "groupBoxRecent";
            this.groupBoxRecent.Size = new System.Drawing.Size(720, 280);
            this.groupBoxRecent.TabIndex = 16;
            this.groupBoxRecent.TabStop = false;
            this.groupBoxRecent.Text = "Capturas Recientes";
            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.Location = new System.Drawing.Point(15, 525);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(64, 64);
            this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxLogo.TabIndex = 17;
            this.pictureBoxLogo.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 650);
            this.Controls.Add(this.groupBoxRecent);
            this.Controls.Add(this.groupBoxControls);
            this.Controls.Add(this.groupBoxStatus);
            this.Controls.Add(this.pictureBoxLogo);
            this.Controls.Add(this.btnMinimizeToTray);
            this.Controls.Add(this.btnExit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Capturer - Gestor de Screenshots";
            this.contextMenuStrip.ResumeLayout(false);
            this.groupBoxStatus.ResumeLayout(false);
            this.groupBoxStatus.PerformLayout();
            this.groupBoxControls.ResumeLayout(false);
            this.groupBoxRecent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblNextCapture;
        private System.Windows.Forms.Label lblTotalScreenshots;
        private System.Windows.Forms.Label lblStorageUsed;
        private System.Windows.Forms.Label lblLastEmail;
        private System.Windows.Forms.Button btnStartCapture;
        private System.Windows.Forms.Button btnStopCapture;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnSendEmail;
        private System.Windows.Forms.Button btnRoutineEmail;
        private System.Windows.Forms.Button btnCaptureNow;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnQuadrants;
        private System.Windows.Forms.ListView listViewScreenshots;
        private System.Windows.Forms.Button btnMinimizeToTray;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem showToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem captureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.GroupBox groupBoxStatus;
        private System.Windows.Forms.GroupBox groupBoxControls;
        private System.Windows.Forms.GroupBox groupBoxRecent;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
    }
}
