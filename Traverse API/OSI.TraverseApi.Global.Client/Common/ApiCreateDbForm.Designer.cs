namespace OSI.TraverseApi.Client
{
    partial class ApiCreateDbForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiCreateDbForm));
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.btnProceed = new TRAVERSE.Controls.TravButton();
            this.chkUseTrusted = new TRAVERSE.Controls.TravCheckBox();
            this.txtPassword = new TRAVERSE.Controls.TravTextBox();
            this.txtUsername = new TRAVERSE.Controls.TravTextBox();
            this.txtDatabaseName = new TRAVERSE.Controls.TravTextBox();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.itemDatabaseName = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.itemUsername = new DevExpress.XtraLayout.LayoutControlItem();
            this.itemPassword = new DevExpress.XtraLayout.LayoutControlItem();
            this.itemTrustedConnection = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem3 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.emptySpaceItem2 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.itemProceed = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chkUseTrusted.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPassword.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtUsername.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDatabaseName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemDatabaseName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemUsername)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemPassword)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemTrustedConnection)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemProceed)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.btnProceed);
            this.layoutMain.Controls.Add(this.chkUseTrusted);
            this.layoutMain.Controls.Add(this.txtPassword);
            this.layoutMain.Controls.Add(this.txtUsername);
            this.layoutMain.Controls.Add(this.txtDatabaseName);
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            // 
            // btnProceed
            // 
            resources.ApplyResources(this.btnProceed, "btnProceed");
            this.btnProceed.Name = "btnProceed";
            this.btnProceed.SecurityId = null;
            this.btnProceed.StyleController = this.layoutMain;
            this.btnProceed.Click += new System.EventHandler(this.btnProceed_Click);
            // 
            // chkUseTrusted
            // 
            resources.ApplyResources(this.chkUseTrusted, "chkUseTrusted");
            this.chkUseTrusted.Name = "chkUseTrusted";
            this.chkUseTrusted.Properties.Caption = resources.GetString("chkUseTrusted.Properties.Caption");
            this.chkUseTrusted.SecurityId = null;
            this.chkUseTrusted.StyleController = this.layoutMain;
            this.chkUseTrusted.CheckStateChanged += new System.EventHandler(this.chkUseTrusted_CheckStateChanged);
            // 
            // txtPassword
            // 
            resources.ApplyResources(this.txtPassword, "txtPassword");
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Properties.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Buffered;
            this.txtPassword.Properties.FixedPointFormat = false;
            this.txtPassword.Properties.NumericControl = false;
            this.txtPassword.Properties.NumericPrecision = -1;
            this.txtPassword.Properties.PasswordChar = '*';
            this.txtPassword.Properties.SetupFunctionId = 0;
            this.txtPassword.SecurityId = null;
            this.txtPassword.StyleController = this.layoutMain;
            // 
            // txtUsername
            // 
            resources.ApplyResources(this.txtUsername, "txtUsername");
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Properties.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Buffered;
            this.txtUsername.Properties.FixedPointFormat = false;
            this.txtUsername.Properties.NumericControl = false;
            this.txtUsername.Properties.NumericPrecision = -1;
            this.txtUsername.Properties.SetupFunctionId = 0;
            this.txtUsername.SecurityId = null;
            this.txtUsername.StyleController = this.layoutMain;
            // 
            // txtDatabaseName
            // 
            resources.ApplyResources(this.txtDatabaseName, "txtDatabaseName");
            this.txtDatabaseName.Name = "txtDatabaseName";
            this.txtDatabaseName.Properties.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Buffered;
            this.txtDatabaseName.Properties.FixedPointFormat = false;
            this.txtDatabaseName.Properties.NumericControl = false;
            this.txtDatabaseName.Properties.NumericPrecision = -1;
            this.txtDatabaseName.Properties.SetupFunctionId = 0;
            this.txtDatabaseName.SecurityId = null;
            this.txtDatabaseName.StyleController = this.layoutMain;
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.itemDatabaseName,
            this.emptySpaceItem1,
            this.itemUsername,
            this.itemPassword,
            this.itemTrustedConnection,
            this.emptySpaceItem3,
            this.emptySpaceItem2,
            this.itemProceed});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Size = new System.Drawing.Size(381, 188);
            this.grpRoot.TextVisible = false;
            // 
            // itemDatabaseName
            // 
            this.itemDatabaseName.Control = this.txtDatabaseName;
            this.itemDatabaseName.Location = new System.Drawing.Point(0, 0);
            this.itemDatabaseName.Name = "itemDatabaseName";
            this.itemDatabaseName.Size = new System.Drawing.Size(361, 24);
            resources.ApplyResources(this.itemDatabaseName, "itemDatabaseName");
            this.itemDatabaseName.TextSize = new System.Drawing.Size(70, 13);
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(0, 95);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(361, 47);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // itemUsername
            // 
            this.itemUsername.Control = this.txtUsername;
            this.itemUsername.Location = new System.Drawing.Point(0, 24);
            this.itemUsername.Name = "itemUsername";
            this.itemUsername.Size = new System.Drawing.Size(361, 24);
            resources.ApplyResources(this.itemUsername, "itemUsername");
            this.itemUsername.TextSize = new System.Drawing.Size(70, 13);
            // 
            // itemPassword
            // 
            this.itemPassword.Control = this.txtPassword;
            this.itemPassword.Location = new System.Drawing.Point(0, 48);
            this.itemPassword.Name = "itemPassword";
            this.itemPassword.Size = new System.Drawing.Size(361, 24);
            resources.ApplyResources(this.itemPassword, "itemPassword");
            this.itemPassword.TextSize = new System.Drawing.Size(70, 13);
            // 
            // itemTrustedConnection
            // 
            this.itemTrustedConnection.Control = this.chkUseTrusted;
            this.itemTrustedConnection.Location = new System.Drawing.Point(0, 72);
            this.itemTrustedConnection.Name = "itemTrustedConnection";
            this.itemTrustedConnection.Size = new System.Drawing.Size(361, 23);
            this.itemTrustedConnection.TextSize = new System.Drawing.Size(0, 0);
            this.itemTrustedConnection.TextVisible = false;
            // 
            // emptySpaceItem3
            // 
            this.emptySpaceItem3.AllowHotTrack = false;
            this.emptySpaceItem3.Location = new System.Drawing.Point(261, 142);
            this.emptySpaceItem3.Name = "emptySpaceItem3";
            this.emptySpaceItem3.Size = new System.Drawing.Size(100, 26);
            this.emptySpaceItem3.TextSize = new System.Drawing.Size(0, 0);
            // 
            // emptySpaceItem2
            // 
            this.emptySpaceItem2.AllowHotTrack = false;
            this.emptySpaceItem2.Location = new System.Drawing.Point(0, 142);
            this.emptySpaceItem2.Name = "emptySpaceItem2";
            this.emptySpaceItem2.Size = new System.Drawing.Size(100, 26);
            this.emptySpaceItem2.TextSize = new System.Drawing.Size(0, 0);
            // 
            // itemProceed
            // 
            this.itemProceed.Control = this.btnProceed;
            this.itemProceed.Location = new System.Drawing.Point(100, 142);
            this.itemProceed.Name = "itemProceed";
            this.itemProceed.Size = new System.Drawing.Size(161, 26);
            this.itemProceed.TextSize = new System.Drawing.Size(0, 0);
            this.itemProceed.TextVisible = false;
            // 
            // ApiCreateDbForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.layoutMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiCreateDbForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chkUseTrusted.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPassword.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtUsername.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDatabaseName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemDatabaseName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemUsername)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemPassword)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemTrustedConnection)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemProceed)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private TRAVERSE.Controls.TravButton btnProceed;
        private TRAVERSE.Controls.TravCheckBox chkUseTrusted;
        private TRAVERSE.Controls.TravTextBox txtPassword;
        private TRAVERSE.Controls.TravTextBox txtUsername;
        private TRAVERSE.Controls.TravTextBox txtDatabaseName;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private DevExpress.XtraLayout.LayoutControlItem itemDatabaseName;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraLayout.LayoutControlItem itemUsername;
        private DevExpress.XtraLayout.LayoutControlItem itemPassword;
        private DevExpress.XtraLayout.LayoutControlItem itemTrustedConnection;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem3;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem2;
        private DevExpress.XtraLayout.LayoutControlItem itemProceed;
    }
}