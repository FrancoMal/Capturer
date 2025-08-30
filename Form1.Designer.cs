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
            this.btnActivityDashboard = new System.Windows.Forms.Button();
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
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(58)))), ((int)(((byte)(64)))));
            this.lblStatus.Location = new System.Drawing.Point(20, 35);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(134, 19);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Estado: Detenido";
            // 
            // lblNextCapture
            // 
            this.lblNextCapture.AutoSize = true;
            this.lblNextCapture.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblNextCapture.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblNextCapture.Location = new System.Drawing.Point(400, 37);
            this.lblNextCapture.Name = "lblNextCapture";
            this.lblNextCapture.Size = new System.Drawing.Size(109, 15);
            this.lblNextCapture.TabIndex = 1;
            this.lblNextCapture.Text = "Próxima: --:--:--";
            // 
            // lblTotalScreenshots
            // 
            this.lblTotalScreenshots.AutoSize = true;
            this.lblTotalScreenshots.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblTotalScreenshots.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblTotalScreenshots.Location = new System.Drawing.Point(20, 65);
            this.lblTotalScreenshots.Name = "lblTotalScreenshots";
            this.lblTotalScreenshots.Size = new System.Drawing.Size(106, 15);
            this.lblTotalScreenshots.TabIndex = 2;
            this.lblTotalScreenshots.Text = "Total Capturas: 0";
            // 
            // lblStorageUsed
            // 
            this.lblStorageUsed.AutoSize = true;
            this.lblStorageUsed.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblStorageUsed.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblStorageUsed.Location = new System.Drawing.Point(400, 65);
            this.lblStorageUsed.Name = "lblStorageUsed";
            this.lblStorageUsed.Size = new System.Drawing.Size(124, 15);
            this.lblStorageUsed.TabIndex = 3;
            this.lblStorageUsed.Text = "Almacenamiento: 0 MB";
            // 
            // lblLastEmail
            // 
            this.lblLastEmail.AutoSize = true;
            this.lblLastEmail.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblLastEmail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblLastEmail.Location = new System.Drawing.Point(20, 90);
            this.lblLastEmail.Name = "lblLastEmail";
            this.lblLastEmail.Size = new System.Drawing.Size(109, 15);
            this.lblLastEmail.TabIndex = 4;
            this.lblLastEmail.Text = "Último Email: Nunca";
            // 
            // btnStartCapture
            // 
            this.btnStartCapture.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnStartCapture.FlatAppearance.BorderSize = 0;
            this.btnStartCapture.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(114)))), ((int)(((byte)(71)))));
            this.btnStartCapture.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(125)))), ((int)(((byte)(78)))));
            this.btnStartCapture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartCapture.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnStartCapture.ForeColor = System.Drawing.Color.White;
            this.btnStartCapture.Location = new System.Drawing.Point(20, 30);
            this.btnStartCapture.Name = "btnStartCapture";
            this.btnStartCapture.Size = new System.Drawing.Size(110, 40);
            this.btnStartCapture.TabIndex = 6;
            this.btnStartCapture.Text = "▶ Iniciar";
            this.btnStartCapture.UseVisualStyleBackColor = false;
            // 
            // btnStopCapture
            // 
            this.btnStopCapture.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.btnStopCapture.FlatAppearance.BorderSize = 0;
            this.btnStopCapture.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(27)))), ((int)(((byte)(27)))));
            this.btnStopCapture.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(164)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.btnStopCapture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopCapture.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnStopCapture.ForeColor = System.Drawing.Color.White;
            this.btnStopCapture.Location = new System.Drawing.Point(140, 30);
            this.btnStopCapture.Name = "btnStopCapture";
            this.btnStopCapture.Size = new System.Drawing.Size(110, 40);
            this.btnStopCapture.TabIndex = 7;
            this.btnStopCapture.Text = "⏹ Detener";
            this.btnStopCapture.UseVisualStyleBackColor = false;
            // 
            // btnSettings
            // 
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(85)))), ((int)(((byte)(99)))));
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.btnSettings.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(76)))), ((int)(((byte)(91)))));
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.Location = new System.Drawing.Point(260, 30);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(120, 40);
            this.btnSettings.TabIndex = 8;
            this.btnSettings.Text = "⚙ Configuración";
            this.btnSettings.UseVisualStyleBackColor = false;
            // 
            // btnSendEmail
            // 
            this.btnSendEmail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnSendEmail.FlatAppearance.BorderSize = 0;
            this.btnSendEmail.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(88)))), ((int)(((byte)(202)))));
            this.btnSendEmail.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(99)))), ((int)(((byte)(228)))));
            this.btnSendEmail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSendEmail.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnSendEmail.ForeColor = System.Drawing.Color.White;
            this.btnSendEmail.Location = new System.Drawing.Point(390, 30);
            this.btnSendEmail.Name = "btnSendEmail";
            this.btnSendEmail.Size = new System.Drawing.Size(120, 40);
            this.btnSendEmail.TabIndex = 9;
            this.btnSendEmail.Text = "✉ Email Manual";
            this.btnSendEmail.UseVisualStyleBackColor = false;
            // 
            // btnRoutineEmail
            // 
            this.btnRoutineEmail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(111)))), ((int)(((byte)(66)))), ((int)(((byte)(193)))));
            this.btnRoutineEmail.FlatAppearance.BorderSize = 0;
            this.btnRoutineEmail.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(44)))), ((int)(((byte)(156)))));
            this.btnRoutineEmail.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(55)))), ((int)(((byte)(175)))));
            this.btnRoutineEmail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRoutineEmail.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnRoutineEmail.ForeColor = System.Drawing.Color.White;
            this.btnRoutineEmail.Location = new System.Drawing.Point(520, 30);
            this.btnRoutineEmail.Name = "btnRoutineEmail";
            this.btnRoutineEmail.Size = new System.Drawing.Size(130, 40);
            this.btnRoutineEmail.TabIndex = 20;
            this.btnRoutineEmail.Text = "📊 Reportes Auto";
            this.btnRoutineEmail.UseVisualStyleBackColor = false;
            // 
            // btnCaptureNow
            // 
            this.btnCaptureNow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(193)))), ((int)(((byte)(7)))));
            this.btnCaptureNow.FlatAppearance.BorderSize = 0;
            this.btnCaptureNow.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(164)))), ((int)(((byte)(6)))));
            this.btnCaptureNow.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(179)))), ((int)(((byte)(6)))));
            this.btnCaptureNow.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCaptureNow.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnCaptureNow.ForeColor = System.Drawing.Color.Black;
            this.btnCaptureNow.Location = new System.Drawing.Point(20, 80);
            this.btnCaptureNow.Name = "btnCaptureNow";
            this.btnCaptureNow.Size = new System.Drawing.Size(130, 40);
            this.btnCaptureNow.TabIndex = 10;
            this.btnCaptureNow.Text = "📸 Capturar Ahora";
            this.btnCaptureNow.UseVisualStyleBackColor = false;
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(162)))), ((int)(((byte)(184)))));
            this.btnOpenFolder.FlatAppearance.BorderSize = 0;
            this.btnOpenFolder.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(134)))), ((int)(((byte)(152)))));
            this.btnOpenFolder.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(148)))), ((int)(((byte)(168)))));
            this.btnOpenFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnOpenFolder.ForeColor = System.Drawing.Color.White;
            this.btnOpenFolder.Location = new System.Drawing.Point(160, 80);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(130, 40);
            this.btnOpenFolder.TabIndex = 11;
            this.btnOpenFolder.Text = "📁 Abrir Carpeta";
            this.btnOpenFolder.UseVisualStyleBackColor = false;
            // 
            // btnQuadrants
            // 
            this.btnQuadrants.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(124)))), ((int)(((byte)(95)))));
            this.btnQuadrants.FlatAppearance.BorderSize = 0;
            this.btnQuadrants.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(188)))), ((int)(((byte)(106)))), ((int)(((byte)(81)))));
            this.btnQuadrants.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(115)))), ((int)(((byte)(88)))));
            this.btnQuadrants.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnQuadrants.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnQuadrants.ForeColor = System.Drawing.Color.White;
            this.btnQuadrants.Location = new System.Drawing.Point(300, 80);
            this.btnQuadrants.Name = "btnQuadrants";
            this.btnQuadrants.Size = new System.Drawing.Size(130, 40);
            this.btnQuadrants.TabIndex = 12;
            this.btnQuadrants.Text = "🔲 Cuadrantes";
            this.btnQuadrants.UseVisualStyleBackColor = false;
            // 
            // btnActivityDashboard
            // 
            this.btnActivityDashboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(111)))), ((int)(((byte)(66)))), ((int)(((byte)(193)))));
            this.btnActivityDashboard.FlatAppearance.BorderSize = 0;
            this.btnActivityDashboard.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(56)))), ((int)(((byte)(164)))));
            this.btnActivityDashboard.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(103)))), ((int)(((byte)(61)))), ((int)(((byte)(179)))));
            this.btnActivityDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActivityDashboard.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnActivityDashboard.ForeColor = System.Drawing.Color.White;
            this.btnActivityDashboard.Location = new System.Drawing.Point(440, 80);
            this.btnActivityDashboard.Name = "btnActivityDashboard";
            this.btnActivityDashboard.Size = new System.Drawing.Size(130, 40);
            this.btnActivityDashboard.TabIndex = 13;
            this.btnActivityDashboard.Text = "📊 Dashboard";
            this.btnActivityDashboard.UseVisualStyleBackColor = false;
            // 
            // listViewScreenshots
            // 
            this.listViewScreenshots.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listViewScreenshots.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.listViewScreenshots.Location = new System.Drawing.Point(20, 30);
            this.listViewScreenshots.Name = "listViewScreenshots";
            this.listViewScreenshots.Size = new System.Drawing.Size(700, 245);
            this.listViewScreenshots.TabIndex = 10;
            this.listViewScreenshots.UseCompatibleStateImageBehavior = false;
            this.listViewScreenshots.View = System.Windows.Forms.View.Details;
            // 
            // btnMinimizeToTray
            // 
            this.btnMinimizeToTray.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnMinimizeToTray.FlatAppearance.BorderSize = 0;
            this.btnMinimizeToTray.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMinimizeToTray.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnMinimizeToTray.ForeColor = System.Drawing.Color.White;
            this.btnMinimizeToTray.Location = new System.Drawing.Point(520, 620);
            this.btnMinimizeToTray.Name = "btnMinimizeToTray";
            this.btnMinimizeToTray.Size = new System.Drawing.Size(120, 35);
            this.btnMinimizeToTray.TabIndex = 12;
            this.btnMinimizeToTray.Text = "⬇ Minimizar";
            this.btnMinimizeToTray.UseVisualStyleBackColor = false;
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.Location = new System.Drawing.Point(650, 620);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(110, 35);
            this.btnExit.TabIndex = 13;
            this.btnExit.Text = "✕ Salir";
            this.btnExit.UseVisualStyleBackColor = false;
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
            this.groupBoxStatus.BackColor = System.Drawing.Color.White;
            this.groupBoxStatus.Controls.Add(this.lblStatus);
            this.groupBoxStatus.Controls.Add(this.lblNextCapture);
            this.groupBoxStatus.Controls.Add(this.lblTotalScreenshots);
            this.groupBoxStatus.Controls.Add(this.lblStorageUsed);
            this.groupBoxStatus.Controls.Add(this.lblLastEmail);
            this.groupBoxStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBoxStatus.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.groupBoxStatus.Location = new System.Drawing.Point(20, 20);
            this.groupBoxStatus.Name = "groupBoxStatus";
            this.groupBoxStatus.Padding = new System.Windows.Forms.Padding(10);
            this.groupBoxStatus.Size = new System.Drawing.Size(740, 120);
            this.groupBoxStatus.TabIndex = 14;
            this.groupBoxStatus.TabStop = false;
            this.groupBoxStatus.Text = "📊 Estado del Sistema";
            // 
            // groupBoxControls
            // 
            this.groupBoxControls.BackColor = System.Drawing.Color.White;
            this.groupBoxControls.Controls.Add(this.btnStartCapture);
            this.groupBoxControls.Controls.Add(this.btnStopCapture);
            this.groupBoxControls.Controls.Add(this.btnSettings);
            this.groupBoxControls.Controls.Add(this.btnSendEmail);
            this.groupBoxControls.Controls.Add(this.btnRoutineEmail);
            this.groupBoxControls.Controls.Add(this.btnCaptureNow);
            this.groupBoxControls.Controls.Add(this.btnOpenFolder);
            this.groupBoxControls.Controls.Add(this.btnQuadrants);
            this.groupBoxControls.Controls.Add(this.btnActivityDashboard);
            this.groupBoxControls.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBoxControls.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.groupBoxControls.Location = new System.Drawing.Point(20, 155);
            this.groupBoxControls.Name = "groupBoxControls";
            this.groupBoxControls.Padding = new System.Windows.Forms.Padding(10);
            this.groupBoxControls.Size = new System.Drawing.Size(740, 140);
            this.groupBoxControls.TabIndex = 15;
            this.groupBoxControls.TabStop = false;
            this.groupBoxControls.Text = "🎛 Panel de Control";
            // 
            // groupBoxRecent
            // 
            this.groupBoxRecent.BackColor = System.Drawing.Color.White;
            this.groupBoxRecent.Controls.Add(this.listViewScreenshots);
            this.groupBoxRecent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBoxRecent.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.groupBoxRecent.Location = new System.Drawing.Point(20, 310);
            this.groupBoxRecent.Name = "groupBoxRecent";
            this.groupBoxRecent.Padding = new System.Windows.Forms.Padding(10);
            this.groupBoxRecent.Size = new System.Drawing.Size(740, 290);
            this.groupBoxRecent.TabIndex = 16;
            this.groupBoxRecent.TabStop = false;
            this.groupBoxRecent.Text = "📷 Capturas Recientes";
            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxLogo.Location = new System.Drawing.Point(30, 620);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(48, 48);
            this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxLogo.TabIndex = 17;
            this.pictureBoxLogo.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true; // Enable automatic scrolling
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(780, 680);
            this.Controls.Add(this.groupBoxRecent);
            this.Controls.Add(this.groupBoxControls);
            this.Controls.Add(this.groupBoxStatus);
            this.Controls.Add(this.pictureBoxLogo);
            this.Controls.Add(this.btnMinimizeToTray);
            this.Controls.Add(this.btnExit);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Capturer v2.4 - Sistema de Monitoreo";
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
        private System.Windows.Forms.Button btnActivityDashboard;
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
