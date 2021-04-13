namespace TraverseApiInstaller
{
    partial class InstallerForm
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallerForm));
            this.grpConnection = new System.Windows.Forms.GroupBox();
            this.lblCompId = new System.Windows.Forms.Label();
            this.lblSysDb = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblPort = new System.Windows.Forms.Label();
            this.lblServerName = new System.Windows.Forms.Label();
            this.txtCompId = new System.Windows.Forms.TextBox();
            this.bindMain = new System.Windows.Forms.BindingSource(this.components);
            this.txtSysDb = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.txtServerPort = new System.Windows.Forms.TextBox();
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.grpApiInfo = new System.Windows.Forms.GroupBox();
            this.chkCss = new System.Windows.Forms.CheckBox();
            this.lblApiDb = new System.Windows.Forms.Label();
            this.lblAppName = new System.Windows.Forms.Label();
            this.lblWebsite = new System.Windows.Forms.Label();
            this.txtApiDb = new System.Windows.Forms.TextBox();
            this.cboWebApp = new System.Windows.Forms.ComboBox();
            this.cboWebsite = new System.Windows.Forms.ComboBox();
            this.btnProceed = new System.Windows.Forms.Button();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.txtPctComplete = new System.Windows.Forms.TextBox();
            this.grpConnection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindMain)).BeginInit();
            this.grpApiInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpConnection
            // 
            this.grpConnection.Controls.Add(this.lblCompId);
            this.grpConnection.Controls.Add(this.lblSysDb);
            this.grpConnection.Controls.Add(this.lblPassword);
            this.grpConnection.Controls.Add(this.lblUsername);
            this.grpConnection.Controls.Add(this.lblPort);
            this.grpConnection.Controls.Add(this.lblServerName);
            this.grpConnection.Controls.Add(this.txtCompId);
            this.grpConnection.Controls.Add(this.txtSysDb);
            this.grpConnection.Controls.Add(this.txtPassword);
            this.grpConnection.Controls.Add(this.txtUsername);
            this.grpConnection.Controls.Add(this.txtServerPort);
            this.grpConnection.Controls.Add(this.txtServerName);
            resources.ApplyResources(this.grpConnection, "grpConnection");
            this.grpConnection.Name = "grpConnection";
            this.grpConnection.TabStop = false;
            // 
            // lblCompId
            // 
            resources.ApplyResources(this.lblCompId, "lblCompId");
            this.lblCompId.Name = "lblCompId";
            // 
            // lblSysDb
            // 
            resources.ApplyResources(this.lblSysDb, "lblSysDb");
            this.lblSysDb.Name = "lblSysDb";
            // 
            // lblPassword
            // 
            resources.ApplyResources(this.lblPassword, "lblPassword");
            this.lblPassword.Name = "lblPassword";
            // 
            // lblUsername
            // 
            resources.ApplyResources(this.lblUsername, "lblUsername");
            this.lblUsername.Name = "lblUsername";
            // 
            // lblPort
            // 
            resources.ApplyResources(this.lblPort, "lblPort");
            this.lblPort.Name = "lblPort";
            // 
            // lblServerName
            // 
            resources.ApplyResources(this.lblServerName, "lblServerName");
            this.lblServerName.Name = "lblServerName";
            // 
            // txtCompId
            // 
            this.txtCompId.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "DefaultCompany", true));
            resources.ApplyResources(this.txtCompId, "txtCompId");
            this.txtCompId.Name = "txtCompId";
            // 
            // bindMain
            // 
            this.bindMain.AllowNew = false;
            this.bindMain.DataSource = typeof(TraverseApiInstaller.ApiConfigInfo);
            // 
            // txtSysDb
            // 
            this.txtSysDb.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "SystemDatabase", true));
            resources.ApplyResources(this.txtSysDb, "txtSysDb");
            this.txtSysDb.Name = "txtSysDb";
            // 
            // txtPassword
            // 
            this.txtPassword.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "UnencryptedPassword", true));
            resources.ApplyResources(this.txtPassword, "txtPassword");
            this.txtPassword.Name = "txtPassword";
            // 
            // txtUsername
            // 
            this.txtUsername.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "UserName", true));
            resources.ApplyResources(this.txtUsername, "txtUsername");
            this.txtUsername.Name = "txtUsername";
            // 
            // txtServerPort
            // 
            this.txtServerPort.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "ServerPort", true));
            resources.ApplyResources(this.txtServerPort, "txtServerPort");
            this.txtServerPort.Name = "txtServerPort";
            // 
            // txtServerName
            // 
            this.txtServerName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "ServerName", true));
            resources.ApplyResources(this.txtServerName, "txtServerName");
            this.txtServerName.Name = "txtServerName";
            // 
            // grpApiInfo
            // 
            this.grpApiInfo.Controls.Add(this.chkCss);
            this.grpApiInfo.Controls.Add(this.lblApiDb);
            this.grpApiInfo.Controls.Add(this.lblAppName);
            this.grpApiInfo.Controls.Add(this.lblWebsite);
            this.grpApiInfo.Controls.Add(this.txtApiDb);
            this.grpApiInfo.Controls.Add(this.cboWebApp);
            this.grpApiInfo.Controls.Add(this.cboWebsite);
            resources.ApplyResources(this.grpApiInfo, "grpApiInfo");
            this.grpApiInfo.Name = "grpApiInfo";
            this.grpApiInfo.TabStop = false;
            // 
            // chkCss
            // 
            resources.ApplyResources(this.chkCss, "chkCss");
            this.chkCss.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.bindMain, "OverwriteContent", true));
            this.chkCss.Name = "chkCss";
            this.chkCss.UseVisualStyleBackColor = true;
            // 
            // lblApiDb
            // 
            resources.ApplyResources(this.lblApiDb, "lblApiDb");
            this.lblApiDb.Name = "lblApiDb";
            // 
            // lblAppName
            // 
            resources.ApplyResources(this.lblAppName, "lblAppName");
            this.lblAppName.Name = "lblAppName";
            // 
            // lblWebsite
            // 
            resources.ApplyResources(this.lblWebsite, "lblWebsite");
            this.lblWebsite.Name = "lblWebsite";
            // 
            // txtApiDb
            // 
            this.txtApiDb.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "ApiDatabase", true));
            resources.ApplyResources(this.txtApiDb, "txtApiDb");
            this.txtApiDb.Name = "txtApiDb";
            // 
            // cboWebApp
            // 
            this.cboWebApp.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cboWebApp.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboWebApp.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "ApplicationName", true));
            resources.ApplyResources(this.cboWebApp, "cboWebApp");
            this.cboWebApp.FormattingEnabled = true;
            this.cboWebApp.Name = "cboWebApp";
            // 
            // cboWebsite
            // 
            this.cboWebsite.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cboWebsite.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboWebsite.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindMain, "WebsiteName", true));
            resources.ApplyResources(this.cboWebsite, "cboWebsite");
            this.cboWebsite.FormattingEnabled = true;
            this.cboWebsite.Name = "cboWebsite";
            this.cboWebsite.SelectedValueChanged += new System.EventHandler(this.cboWebsite_SelectedValueChanged);
            // 
            // btnProceed
            // 
            resources.ApplyResources(this.btnProceed, "btnProceed");
            this.btnProceed.Name = "btnProceed";
            this.btnProceed.UseVisualStyleBackColor = true;
            this.btnProceed.Click += new System.EventHandler(this.btnProceed_Click);
            // 
            // txtStatus
            // 
            this.txtStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.txtStatus, "txtStatus");
            this.txtStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ReadOnly = true;
            this.txtStatus.TabStop = false;
            // 
            // txtPctComplete
            // 
            this.txtPctComplete.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.txtPctComplete, "txtPctComplete");
            this.txtPctComplete.Name = "txtPctComplete";
            this.txtPctComplete.ReadOnly = true;
            this.txtPctComplete.TabStop = false;
            // 
            // InstallerForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtPctComplete);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.btnProceed);
            this.Controls.Add(this.grpApiInfo);
            this.Controls.Add(this.grpConnection);
            this.Name = "InstallerForm";
            this.ShowIcon = false;
            this.grpConnection.ResumeLayout(false);
            this.grpConnection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindMain)).EndInit();
            this.grpApiInfo.ResumeLayout(false);
            this.grpApiInfo.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox grpConnection;
        private System.Windows.Forms.TextBox txtCompId;
        private System.Windows.Forms.TextBox txtSysDb;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtServerPort;
        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.GroupBox grpApiInfo;
        private System.Windows.Forms.ComboBox cboWebApp;
        private System.Windows.Forms.ComboBox cboWebsite;
        private System.Windows.Forms.TextBox txtApiDb;
        private System.Windows.Forms.Button btnProceed;
        private System.Windows.Forms.Label lblCompId;
        private System.Windows.Forms.Label lblSysDb;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.Label lblServerName;
        private System.Windows.Forms.Label lblApiDb;
        private System.Windows.Forms.Label lblAppName;
        private System.Windows.Forms.Label lblWebsite;
        private System.Windows.Forms.CheckBox chkCss;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.BindingSource bindMain;
        private System.Windows.Forms.TextBox txtPctComplete;
    }
}