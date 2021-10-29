namespace OSI.TraverseApi.Client
{
    partial class ApiFunctionSchemaControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiFunctionSchemaControl));
            this.bindMain = new System.Windows.Forms.BindingSource(this.components);
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.dgvSchema = new TRAVERSE.Controls.TravDataGridViewControl();
            this.gvSchema = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colSeqNum = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colApiFieldName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colFieldOption = new DevExpress.XtraGrid.Columns.GridColumn();
            this.cboCheckOptions = new DevExpress.XtraEditors.Repository.RepositoryItemCheckedComboBoxEdit();
            this.colTravFieldName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colChildFunctionId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.lkpFunction = new TRAVERSE.Controls.RepositoryItemTravLookupControl();
            this.repositoryItemTravLookupControl1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colValueTranslation = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtValueTranslation = new TRAVERSE.Controls.RepositoryItemTravTextBox();
            this.colNotes = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtNotes = new TRAVERSE.Controls.RepositoryItemTravMemoEditControl();
            this.colQueryColumnName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).BeginInit();
            this.splitContainerBase.Panel1.SuspendLayout();
            this.splitContainerBase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSchema)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvSchema)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboCheckOptions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lkpFunction)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTravLookupControl1View)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtValueTranslation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNotes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
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
            this.errorProviderBase.DataSource = this.bindMain;
            // 
            // bindMain
            // 
            this.bindMain.DataSource = typeof(OSI.TraverseApi.Business.ApiFunctionSchema);
            this.bindMain.AddingNew += new System.ComponentModel.AddingNewEventHandler(this.BindingSourceAddingNew);
            this.bindMain.CurrentChanged += new System.EventHandler(this.BindingSourceCurrentChanged);
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.dgvSchema);
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            // 
            // dgvSchema
            // 
            this.dgvSchema.DataSource = this.bindMain;
            resources.ApplyResources(this.dgvSchema, "dgvSchema");
            this.dgvSchema.MainView = this.gvSchema;
            this.dgvSchema.MultiSelect = true;
            this.dgvSchema.Name = "dgvSchema";
            this.dgvSchema.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.txtNotes,
            this.lkpFunction,
            this.txtValueTranslation,
            this.cboCheckOptions});
            this.dgvSchema.ShowGroupPanel = false;
            this.dgvSchema.ShowNewRow = true;
            this.dgvSchema.UseEmbeddedNavigator = true;
            this.dgvSchema.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvSchema});
            // 
            // gvSchema
            // 
            this.gvSchema.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colSeqNum,
            this.colApiFieldName,
            this.colFieldOption,
            this.colTravFieldName,
            this.colChildFunctionId,
            this.colValueTranslation,
            this.colNotes,
            this.colQueryColumnName});
            this.gvSchema.GridControl = this.dgvSchema;
            this.gvSchema.Name = "gvSchema";
            this.gvSchema.OptionsDetail.AllowZoomDetail = false;
            this.gvSchema.OptionsDetail.EnableMasterViewMode = false;
            this.gvSchema.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom;
            this.gvSchema.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowForFocusedRow;
            this.gvSchema.OptionsView.ShowGroupPanel = false;
            this.gvSchema.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(this.gvSchema_CustomUnboundColumnData);
            this.gvSchema.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvSchema_CustomColumnDisplayText);
            // 
            // colSeqNum
            // 
            this.colSeqNum.FieldName = "SeqNum";
            this.colSeqNum.Name = "colSeqNum";
            resources.ApplyResources(this.colSeqNum, "colSeqNum");
            // 
            // colApiFieldName
            // 
            resources.ApplyResources(this.colApiFieldName, "colApiFieldName");
            this.colApiFieldName.FieldName = "ApiFieldName";
            this.colApiFieldName.Name = "colApiFieldName";
            // 
            // colFieldOption
            // 
            resources.ApplyResources(this.colFieldOption, "colFieldOption");
            this.colFieldOption.ColumnEdit = this.cboCheckOptions;
            this.colFieldOption.FieldName = "FieldScope";
            this.colFieldOption.Name = "colFieldOption";
            // 
            // cboCheckOptions
            // 
            this.cboCheckOptions.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            resources.ApplyResources(this.cboCheckOptions, "cboCheckOptions");
            this.cboCheckOptions.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("cboCheckOptions.Buttons"))))});
            this.cboCheckOptions.DropDownRows = 10;
            this.cboCheckOptions.Name = "cboCheckOptions";
            // 
            // colTravFieldName
            // 
            resources.ApplyResources(this.colTravFieldName, "colTravFieldName");
            this.colTravFieldName.FieldName = "TravFieldName";
            this.colTravFieldName.Name = "colTravFieldName";
            // 
            // colChildFunctionId
            // 
            resources.ApplyResources(this.colChildFunctionId, "colChildFunctionId");
            this.colChildFunctionId.ColumnEdit = this.lkpFunction;
            this.colChildFunctionId.FieldName = "ChildFunctionId";
            this.colChildFunctionId.Name = "colChildFunctionId";
            // 
            // lkpFunction
            // 
            resources.ApplyResources(this.lkpFunction, "lkpFunction");
            this.lkpFunction.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton()});
            this.lkpFunction.DisablePopup = false;
            this.lkpFunction.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Buffered;
            this.lkpFunction.LimitToList = false;
            this.lkpFunction.LookupDefinitionId = null;
            this.lkpFunction.Name = "lkpFunction";
            this.lkpFunction.PopupFormMinSize = new System.Drawing.Size(500, 50);
            this.lkpFunction.PopupView = this.repositoryItemTravLookupControl1View;
            this.lkpFunction.SetupFunctionId = 0;
            this.lkpFunction.SimpleMask = null;
            this.lkpFunction.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            // 
            // repositoryItemTravLookupControl1View
            // 
            this.repositoryItemTravLookupControl1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.repositoryItemTravLookupControl1View.Name = "repositoryItemTravLookupControl1View";
            this.repositoryItemTravLookupControl1View.OptionsBehavior.AutoPopulateColumns = false;
            this.repositoryItemTravLookupControl1View.OptionsMenu.EnableGroupPanelMenu = false;
            this.repositoryItemTravLookupControl1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.repositoryItemTravLookupControl1View.OptionsView.ShowAutoFilterRow = true;
            this.repositoryItemTravLookupControl1View.OptionsView.ShowGroupPanel = false;
            this.repositoryItemTravLookupControl1View.OptionsView.ShowIndicator = false;
            this.repositoryItemTravLookupControl1View.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.True;
            // 
            // colValueTranslation
            // 
            this.colValueTranslation.ColumnEdit = this.txtValueTranslation;
            this.colValueTranslation.FieldName = "ValueTranslation";
            this.colValueTranslation.Name = "colValueTranslation";
            this.colValueTranslation.OptionsColumn.ReadOnly = true;
            this.colValueTranslation.UnboundType = DevExpress.Data.UnboundColumnType.String;
            resources.ApplyResources(this.colValueTranslation, "colValueTranslation");
            // 
            // txtValueTranslation
            // 
            this.txtValueTranslation.Appearance.Options.UseTextOptions = true;
            resources.ApplyResources(this.txtValueTranslation, "txtValueTranslation");
            this.txtValueTranslation.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton()});
            this.txtValueTranslation.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Buffered;
            this.txtValueTranslation.FixedPointFormat = false;
            this.txtValueTranslation.Name = "txtValueTranslation";
            this.txtValueTranslation.NumericControl = false;
            this.txtValueTranslation.NumericPrecision = -1;
            this.txtValueTranslation.SetupFunctionId = 0;
            this.txtValueTranslation.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.txtValueTranslation.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(this.TranslationButtonClick);
            this.txtValueTranslation.CustomDisplayText += new DevExpress.XtraEditors.Controls.CustomDisplayTextEventHandler(this.txtValueTranslation_CustomDisplayText);
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
            this.txtNotes.ShowIcon = false;
            // 
            // colQueryColumnName
            // 
            resources.ApplyResources(this.colQueryColumnName, "colQueryColumnName");
            this.colQueryColumnName.FieldName = "QueryColumnName";
            this.colQueryColumnName.Name = "colQueryColumnName";
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Padding = new DevExpress.XtraLayout.Utils.Padding(2, 2, 2, 2);
            this.grpRoot.Size = new System.Drawing.Size(800, 600);
            this.grpRoot.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.dgvSchema;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(796, 596);
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextVisible = false;
            // 
            // ApiFunctionSchemaControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BindingSource = this.bindMain;
            this.FunctionId = "65912DAA-2272-41E6-A1DE-8981C4143CBA";
            this.HandleListChangedEvent = true;
            this.Name = "ApiFunctionSchemaControl";
            this.splitContainerBase.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).EndInit();
            this.splitContainerBase.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSchema)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvSchema)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboCheckOptions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lkpFunction)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTravLookupControl1View)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtValueTranslation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNotes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource bindMain;
        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private TRAVERSE.Controls.TravDataGridViewControl dgvSchema;
        private DevExpress.XtraGrid.Views.Grid.GridView gvSchema;
        private DevExpress.XtraGrid.Columns.GridColumn colSeqNum;
        private DevExpress.XtraGrid.Columns.GridColumn colTravFieldName;
        private DevExpress.XtraGrid.Columns.GridColumn colApiFieldName;
        private DevExpress.XtraGrid.Columns.GridColumn colNotes;
        private DevExpress.XtraGrid.Columns.GridColumn colChildFunctionId;
        private DevExpress.XtraGrid.Columns.GridColumn colValueTranslation;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private TRAVERSE.Controls.RepositoryItemTravLookupControl lkpFunction;
        private DevExpress.XtraGrid.Views.Grid.GridView repositoryItemTravLookupControl1View;
        private TRAVERSE.Controls.RepositoryItemTravTextBox txtValueTranslation;
        private TRAVERSE.Controls.RepositoryItemTravMemoEditControl txtNotes;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckedComboBoxEdit cboCheckOptions;
        private DevExpress.XtraGrid.Columns.GridColumn colFieldOption;
        private DevExpress.XtraGrid.Columns.GridColumn colQueryColumnName;
    }
}
