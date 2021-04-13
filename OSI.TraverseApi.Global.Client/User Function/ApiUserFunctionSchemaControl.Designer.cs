namespace OSI.TraverseApi.Client
{
    partial class ApiUserFunctionSchemaControl
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiUserFunctionSchemaControl));
            this.bindUserSchema = new System.Windows.Forms.BindingSource(this.components);
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.dgvUserSchema = new TRAVERSE.Controls.TravDataGridViewControl();
            this.gvUserSchema = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colApiFieldName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colFieldType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.cboFieldType = new TRAVERSE.Controls.RepositoryItemTravEnumListControl();
            this.colFunctionSchemaId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.cboFunctionSchemaId = new TRAVERSE.Controls.RepositoryItemTravComboBoxAdv();
            this.colCustomFieldName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colHidden = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkHidden = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colDefaultValue = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colNotes = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtNotes = new TRAVERSE.Controls.RepositoryItemTravMemoEditControl();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.itemUserSchema = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).BeginInit();
            this.splitContainerBase.Panel1.SuspendLayout();
            this.splitContainerBase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindUserSchema)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUserSchema)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvUserSchema)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboFieldType)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboFunctionSchemaId)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkHidden)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNotes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemUserSchema)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainerBase
            // 
            // 
            // splitContainerBase.Panel1
            // 
            this.splitContainerBase.Panel1.Controls.Add(this.layoutMain);
            // 
            // errorProviderBase
            // 
            this.errorProviderBase.DataSource = this.bindUserSchema;
            // 
            // bindUserSchema
            // 
            this.bindUserSchema.DataSource = typeof(OSI.TraverseApi.Business.ApiUserFunctionSchema);
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.dgvUserSchema);
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            // 
            // dgvUserSchema
            // 
            this.dgvUserSchema.DataSource = this.bindUserSchema;
            resources.ApplyResources(this.dgvUserSchema, "dgvUserSchema");
            this.dgvUserSchema.MainView = this.gvUserSchema;
            this.dgvUserSchema.MultiSelect = true;
            this.dgvUserSchema.Name = "dgvUserSchema";
            this.dgvUserSchema.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.chkHidden,
            this.txtNotes,
            this.cboFunctionSchemaId,
            this.cboFieldType});
            this.dgvUserSchema.ShowGroupPanel = false;
            this.dgvUserSchema.ShowNewRow = true;
            this.dgvUserSchema.UseEmbeddedNavigator = true;
            this.dgvUserSchema.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvUserSchema});
            // 
            // gvUserSchema
            // 
            this.gvUserSchema.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colApiFieldName,
            this.colFieldType,
            this.colFunctionSchemaId,
            this.colCustomFieldName,
            this.colHidden,
            this.colDefaultValue,
            this.colNotes});
            this.gvUserSchema.GridControl = this.dgvUserSchema;
            this.gvUserSchema.Name = "gvUserSchema";
            this.gvUserSchema.OptionsDetail.AllowZoomDetail = false;
            this.gvUserSchema.OptionsDetail.EnableMasterViewMode = false;
            this.gvUserSchema.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom;
            this.gvUserSchema.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            this.gvUserSchema.OptionsView.ShowGroupPanel = false;
            // 
            // colApiFieldName
            // 
            resources.ApplyResources(this.colApiFieldName, "colApiFieldName");
            this.colApiFieldName.FieldName = "ApiFieldName";
            this.colApiFieldName.Name = "colApiFieldName";
            // 
            // colFieldType
            // 
            this.colFieldType.ColumnEdit = this.cboFieldType;
            this.colFieldType.FieldName = "FieldType";
            this.colFieldType.Name = "colFieldType";
            resources.ApplyResources(this.colFieldType, "colFieldType");
            // 
            // cboFieldType
            // 
            resources.ApplyResources(this.cboFieldType, "cboFieldType");
            this.cboFieldType.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("cboFieldType.Buttons"))))});
            this.cboFieldType.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] {
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo(resources.GetString("cboFieldType.Columns"), resources.GetString("cboFieldType.Columns1"))});
            this.cboFieldType.DisplayMember = "Description";
            this.cboFieldType.DropDownRows = 2;
            this.cboFieldType.KeyTypeCode = System.TypeCode.Byte;
            this.cboFieldType.KeyValuePair = "0;Entity Field;1;Custom Field";
            this.cboFieldType.Name = "cboFieldType";
            this.cboFieldType.SetupFunctionId = 0;
            this.cboFieldType.ShowFooter = false;
            this.cboFieldType.ShowHeader = false;
            this.cboFieldType.ValueMember = "Id";
            // 
            // colFunctionSchemaId
            // 
            resources.ApplyResources(this.colFunctionSchemaId, "colFunctionSchemaId");
            this.colFunctionSchemaId.ColumnEdit = this.cboFunctionSchemaId;
            this.colFunctionSchemaId.FieldName = "FunctionSchemaId";
            this.colFunctionSchemaId.Name = "colFunctionSchemaId";
            // 
            // cboFunctionSchemaId
            // 
            this.cboFunctionSchemaId.AcceptEditorTextAsNewValue = DevExpress.Utils.DefaultBoolean.False;
            resources.ApplyResources(this.cboFunctionSchemaId, "cboFunctionSchemaId");
            this.cboFunctionSchemaId.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("cboFunctionSchemaId.Buttons"))))});
            this.cboFunctionSchemaId.Name = "cboFunctionSchemaId";
            this.cboFunctionSchemaId.SetupFunctionId = 0;
            this.cboFunctionSchemaId.ShowHeader = false;
            // 
            // colCustomFieldName
            // 
            this.colCustomFieldName.FieldName = "CustomFieldName";
            this.colCustomFieldName.Name = "colCustomFieldName";
            resources.ApplyResources(this.colCustomFieldName, "colCustomFieldName");
            // 
            // colHidden
            // 
            this.colHidden.ColumnEdit = this.chkHidden;
            this.colHidden.FieldName = "Hidden";
            this.colHidden.Name = "colHidden";
            resources.ApplyResources(this.colHidden, "colHidden");
            // 
            // chkHidden
            // 
            resources.ApplyResources(this.chkHidden, "chkHidden");
            this.chkHidden.Name = "chkHidden";
            this.chkHidden.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            // 
            // colDefaultValue
            // 
            this.colDefaultValue.FieldName = "DefaultValue";
            this.colDefaultValue.Name = "colDefaultValue";
            resources.ApplyResources(this.colDefaultValue, "colDefaultValue");
            // 
            // colNotes
            // 
            this.colNotes.ColumnEdit = this.txtNotes;
            this.colNotes.FieldName = "Notes";
            this.colNotes.Name = "colNotes";
            resources.ApplyResources(this.colNotes, "colNotes");
            // 
            // txtNotes
            // 
            resources.ApplyResources(this.txtNotes, "txtNotes");
            this.txtNotes.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("txtNotes.Buttons"))))});
            this.txtNotes.Name = "txtNotes";
            this.txtNotes.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtNotes.ShowIcon = false;
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.itemUserSchema});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Padding = new DevExpress.XtraLayout.Utils.Padding(1, 1, 1, 1);
            this.grpRoot.Size = new System.Drawing.Size(800, 600);
            this.grpRoot.TextVisible = false;
            // 
            // itemUserSchema
            // 
            this.itemUserSchema.Control = this.dgvUserSchema;
            this.itemUserSchema.Location = new System.Drawing.Point(0, 0);
            this.itemUserSchema.Name = "itemUserSchema";
            this.itemUserSchema.Size = new System.Drawing.Size(798, 598);
            this.itemUserSchema.TextSize = new System.Drawing.Size(0, 0);
            this.itemUserSchema.TextVisible = false;
            // 
            // ApiUserFunctionSchemaControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BindingSource = this.bindUserSchema;
            this.HandleListChangedEvent = true;
            this.Name = "ApiUserFunctionSchemaControl";
            this.splitContainerBase.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).EndInit();
            this.splitContainerBase.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindUserSchema)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvUserSchema)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvUserSchema)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboFieldType)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboFunctionSchemaId)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkHidden)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNotes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemUserSchema)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource bindUserSchema;
        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private TRAVERSE.Controls.TravDataGridViewControl dgvUserSchema;
        private DevExpress.XtraGrid.Views.Grid.GridView gvUserSchema;
        private DevExpress.XtraGrid.Columns.GridColumn colApiFieldName;
        private DevExpress.XtraGrid.Columns.GridColumn colFieldType;
        private DevExpress.XtraGrid.Columns.GridColumn colFunctionSchemaId;
        private DevExpress.XtraGrid.Columns.GridColumn colCustomFieldName;
        private DevExpress.XtraGrid.Columns.GridColumn colHidden;
        private DevExpress.XtraGrid.Columns.GridColumn colDefaultValue;
        private DevExpress.XtraGrid.Columns.GridColumn colNotes;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private DevExpress.XtraLayout.LayoutControlItem itemUserSchema;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkHidden;
        private TRAVERSE.Controls.RepositoryItemTravMemoEditControl txtNotes;
        private TRAVERSE.Controls.RepositoryItemTravComboBoxAdv cboFunctionSchemaId;
        private TRAVERSE.Controls.RepositoryItemTravEnumListControl cboFieldType;
    }
}
