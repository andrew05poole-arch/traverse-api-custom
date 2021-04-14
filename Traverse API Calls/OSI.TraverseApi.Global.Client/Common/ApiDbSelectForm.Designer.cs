namespace OSI.TraverseApi.Client
{
    partial class ApiDbSelectForm
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lstDatabase = new TRAVERSE.Controls.TravSimpleListBox();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.btnOk = new TRAVERSE.Controls.TravButton();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.btnCancel = new TRAVERSE.Controls.TravButton();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.emptySpaceItem2 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.emptySpaceItem3 = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lstDatabase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem3)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.btnCancel);
            this.layoutMain.Controls.Add(this.btnOk);
            this.layoutMain.Controls.Add(this.lstDatabase);
            this.layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutMain.Location = new System.Drawing.Point(0, 0);
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            this.layoutMain.Size = new System.Drawing.Size(384, 561);
            this.layoutMain.TabIndex = 0;
            this.layoutMain.Text = "travLayoutControl1";
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1,
            this.layoutControlItem2,
            this.layoutControlItem3,
            this.emptySpaceItem1,
            this.emptySpaceItem2,
            this.emptySpaceItem3});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Size = new System.Drawing.Size(384, 561);
            this.grpRoot.TextVisible = false;
            // 
            // lstDatabase
            // 
            this.lstDatabase.Location = new System.Drawing.Point(12, 12);
            this.lstDatabase.Name = "lstDatabase";
            this.lstDatabase.Size = new System.Drawing.Size(360, 511);
            this.lstDatabase.StyleController = this.layoutMain;
            this.lstDatabase.TabIndex = 4;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.lstDatabase;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(364, 515);
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextVisible = false;
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(203, 527);
            this.btnOk.Name = "btnOk";
            this.btnOk.SecurityId = null;
            this.btnOk.Size = new System.Drawing.Size(82, 22);
            this.btnOk.StyleController = this.layoutMain;
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "OK";
            this.btnOk.Click += new System.EventHandler(this.OKButtonClicked);
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.btnOk;
            this.layoutControlItem2.Location = new System.Drawing.Point(191, 515);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Size = new System.Drawing.Size(86, 26);
            this.layoutControlItem2.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem2.TextVisible = false;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(102, 527);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.SecurityId = null;
            this.btnCancel.Size = new System.Drawing.Size(87, 22);
            this.btnCancel.StyleController = this.layoutMain;
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.btnCancel;
            this.layoutControlItem3.Location = new System.Drawing.Point(90, 515);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Size = new System.Drawing.Size(91, 26);
            this.layoutControlItem3.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem3.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(181, 515);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(10, 26);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // emptySpaceItem2
            // 
            this.emptySpaceItem2.AllowHotTrack = false;
            this.emptySpaceItem2.Location = new System.Drawing.Point(0, 515);
            this.emptySpaceItem2.Name = "emptySpaceItem2";
            this.emptySpaceItem2.Size = new System.Drawing.Size(90, 26);
            this.emptySpaceItem2.TextSize = new System.Drawing.Size(0, 0);
            // 
            // emptySpaceItem3
            // 
            this.emptySpaceItem3.AllowHotTrack = false;
            this.emptySpaceItem3.Location = new System.Drawing.Point(277, 515);
            this.emptySpaceItem3.Name = "emptySpaceItem3";
            this.emptySpaceItem3.Size = new System.Drawing.Size(87, 26);
            this.emptySpaceItem3.TextSize = new System.Drawing.Size(0, 0);
            // 
            // ApiDbSelectForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 561);
            this.Controls.Add(this.layoutMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiDbSelectForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select API Database";
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lstDatabase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem3)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private TRAVERSE.Controls.TravButton btnCancel;
        private TRAVERSE.Controls.TravButton btnOk;
        private TRAVERSE.Controls.TravSimpleListBox lstDatabase;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem2;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem3;
    }
}
