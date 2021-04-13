namespace OSI.TraverseApi.Client
{
    partial class ApiFunctionSchemaTranslateForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiFunctionSchemaTranslateForm));
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.dgvTranslate = new TRAVERSE.Controls.TravDataGridViewControl();
            this.bindTranslate = new System.Windows.Forms.BindingSource(this.components);
            this.gvTranslate = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colKey = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colValue = new DevExpress.XtraGrid.Columns.GridColumn();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTranslate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindTranslate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvTranslate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.dgvTranslate);
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            // 
            // dgvTranslate
            // 
            this.dgvTranslate.DataSource = this.bindTranslate;
            resources.ApplyResources(this.dgvTranslate, "dgvTranslate");
            this.dgvTranslate.MainView = this.gvTranslate;
            this.dgvTranslate.MultiSelect = true;
            this.dgvTranslate.Name = "dgvTranslate";
            this.dgvTranslate.ShowGroupPanel = false;
            this.dgvTranslate.ShowNewRow = true;
            this.dgvTranslate.UseEmbeddedNavigator = true;
            this.dgvTranslate.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvTranslate});
            // 
            // bindTranslate
            // 
            this.bindTranslate.DataSource = typeof(OSI.TraverseApi.Business.ApiValueTranslate);
            // 
            // gvTranslate
            // 
            this.gvTranslate.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colKey,
            this.colValue});
            this.gvTranslate.GridControl = this.dgvTranslate;
            this.gvTranslate.Name = "gvTranslate";
            this.gvTranslate.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom;
            this.gvTranslate.OptionsView.ShowGroupPanel = false;
            // 
            // colKey
            // 
            resources.ApplyResources(this.colKey, "colKey");
            this.colKey.FieldName = "Key";
            this.colKey.Name = "colKey";
            // 
            // colValue
            // 
            resources.ApplyResources(this.colValue, "colValue");
            this.colValue.FieldName = "Value";
            this.colValue.Name = "colValue";
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Size = new System.Drawing.Size(451, 301);
            this.grpRoot.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.dgvTranslate;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(431, 281);
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextVisible = false;
            // 
            // ApiFunctionSchemaTranslateForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.layoutMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiFunctionSchemaTranslateForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTranslate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindTranslate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvTranslate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private TRAVERSE.Controls.TravDataGridViewControl dgvTranslate;
        private System.Windows.Forms.BindingSource bindTranslate;
        private DevExpress.XtraGrid.Views.Grid.GridView gvTranslate;
        private DevExpress.XtraGrid.Columns.GridColumn colKey;
        private DevExpress.XtraGrid.Columns.GridColumn colValue;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
    }
}