namespace OSI.TraverseApi.Client
{
    partial class ApiUserFunctionFilterForm
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
            if (disposing && btnOk != null)
                btnOk.Click -= btnOk_Click;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiUserFunctionFilterForm));
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.dfpFilter = new TRAVERSE.Controls.TravFilterControl();
            this.btnCancel = new TRAVERSE.Controls.TravButton();
            this.btnOk = new TRAVERSE.Controls.TravButton();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.itemCancel = new DevExpress.XtraLayout.LayoutControlItem();
            this.itemFilter = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem2 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.itemApply = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem3 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.emptySpaceItem4 = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemCancel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemApply)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem4)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.dfpFilter);
            this.layoutMain.Controls.Add(this.btnCancel);
            this.layoutMain.Controls.Add(this.btnOk);
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            // 
            // dfpFilter
            // 
            this.dfpFilter.Cursor = System.Windows.Forms.Cursors.Arrow;
            resources.ApplyResources(this.dfpFilter, "dfpFilter");
            this.dfpFilter.Name = "dfpFilter";
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.SecurityId = null;
            this.btnCancel.StyleController = this.layoutMain;
            // 
            // btnOk
            // 
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.Name = "btnOk";
            this.btnOk.SecurityId = null;
            this.btnOk.StyleController = this.layoutMain;
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.itemCancel,
            this.itemFilter,
            this.emptySpaceItem2,
            this.itemApply,
            this.emptySpaceItem3,
            this.emptySpaceItem4});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Size = new System.Drawing.Size(584, 361);
            this.grpRoot.TextVisible = false;
            // 
            // itemCancel
            // 
            this.itemCancel.Control = this.btnCancel;
            this.itemCancel.Location = new System.Drawing.Point(340, 315);
            this.itemCancel.Name = "itemCancel";
            this.itemCancel.Size = new System.Drawing.Size(108, 26);
            this.itemCancel.TextSize = new System.Drawing.Size(0, 0);
            this.itemCancel.TextVisible = false;
            // 
            // itemFilter
            // 
            this.itemFilter.Control = this.dfpFilter;
            this.itemFilter.Location = new System.Drawing.Point(0, 0);
            this.itemFilter.Name = "itemFilter";
            this.itemFilter.Size = new System.Drawing.Size(564, 315);
            this.itemFilter.TextSize = new System.Drawing.Size(0, 0);
            this.itemFilter.TextVisible = false;
            // 
            // emptySpaceItem2
            // 
            this.emptySpaceItem2.AllowHotTrack = false;
            this.emptySpaceItem2.Location = new System.Drawing.Point(0, 315);
            this.emptySpaceItem2.Name = "emptySpaceItem2";
            this.emptySpaceItem2.Size = new System.Drawing.Size(116, 26);
            this.emptySpaceItem2.TextSize = new System.Drawing.Size(0, 0);
            // 
            // itemApply
            // 
            this.itemApply.Control = this.btnOk;
            this.itemApply.Location = new System.Drawing.Point(116, 315);
            this.itemApply.Name = "itemApply";
            this.itemApply.Size = new System.Drawing.Size(108, 26);
            this.itemApply.TextSize = new System.Drawing.Size(0, 0);
            this.itemApply.TextVisible = false;
            // 
            // emptySpaceItem3
            // 
            this.emptySpaceItem3.AllowHotTrack = false;
            this.emptySpaceItem3.Location = new System.Drawing.Point(224, 315);
            this.emptySpaceItem3.Name = "emptySpaceItem3";
            this.emptySpaceItem3.Size = new System.Drawing.Size(116, 26);
            this.emptySpaceItem3.TextSize = new System.Drawing.Size(0, 0);
            // 
            // emptySpaceItem4
            // 
            this.emptySpaceItem4.AllowHotTrack = false;
            this.emptySpaceItem4.Location = new System.Drawing.Point(448, 315);
            this.emptySpaceItem4.Name = "emptySpaceItem4";
            this.emptySpaceItem4.Size = new System.Drawing.Size(116, 26);
            this.emptySpaceItem4.TextSize = new System.Drawing.Size(0, 0);
            // 
            // ApiUserFunctionFilterForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.layoutMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiUserFunctionFilterForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemCancel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemApply)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem4)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private TRAVERSE.Controls.TravFilterControl dfpFilter;
        private TRAVERSE.Controls.TravButton btnCancel;
        private TRAVERSE.Controls.TravButton btnOk;
        private DevExpress.XtraLayout.LayoutControlItem itemCancel;
        private DevExpress.XtraLayout.LayoutControlItem itemFilter;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem2;
        private DevExpress.XtraLayout.LayoutControlItem itemApply;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem3;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem4;
    }
}