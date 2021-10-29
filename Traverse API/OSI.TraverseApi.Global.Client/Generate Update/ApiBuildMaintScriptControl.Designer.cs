namespace OSI.TraverseApi.Client
{
    partial class ApiBuildMaintScriptControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiBuildMaintScriptControl));
            this.bindMain = new System.Windows.Forms.BindingSource();
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.dgvMain = new TRAVERSE.Controls.TravDataGridViewControl();
            this.gvMain = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colSelect = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkSelect = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colAppId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.cboType = new TRAVERSE.Controls.RepositoryItemTravEnumListControl();
            this.colAllowRead = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkAllowRead = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colAllowEdit = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkAllowEdit = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colAllowNew = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkAllowNew = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colAllowDelete = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkAllowDelete = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colOverrideId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.lkpOverride = new TRAVERSE.Controls.RepositoryItemTravLookupControl();
            this.repositoryItemTravLookupControl1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.itemFunction = new DevExpress.XtraLayout.LayoutControlItem();
            this.fbdScript = new System.Windows.Forms.FolderBrowserDialog();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerProcess)).BeginInit();
            this.splitContainerProcess.Panel2.SuspendLayout();
            this.splitContainerProcess.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).BeginInit();
            this.splitContainerBase.Panel1.SuspendLayout();
            this.splitContainerBase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkSelect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboType)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowRead)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowNew)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowDelete)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lkpOverride)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTravLookupControl1View)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemFunction)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainerProcess
            // 
            this.splitContainerProcess.Panel1Collapsed = true;
            // 
            // splitContainerProcess.Panel2
            // 
            this.splitContainerProcess.Panel2.Controls.Add(this.layoutMain);
            resources.ApplyResources(this.splitContainerProcess, "splitContainerProcess");
            // 
            // splitContainerBase
            // 
            // 
            // errorProviderBase
            // 
            this.errorProviderBase.DataSource = this.bindMain;
            // 
            // bindMain
            // 
            this.bindMain.AllowNew = false;
            this.bindMain.DataSource = typeof(OSI.TraverseApi.Business.ApiFunctionHeader);
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.dgvMain);
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            // 
            // dgvMain
            // 
            this.dgvMain.DataSource = this.bindMain;
            this.dgvMain.EmbeddedNavigator.Buttons.Append.Visible = false;
            this.dgvMain.EmbeddedNavigator.Buttons.CancelEdit.Visible = false;
            this.dgvMain.EmbeddedNavigator.Buttons.Edit.Visible = false;
            this.dgvMain.EmbeddedNavigator.Buttons.EndEdit.Visible = false;
            this.dgvMain.EmbeddedNavigator.Buttons.Remove.Visible = false;
            resources.ApplyResources(this.dgvMain, "dgvMain");
            this.dgvMain.MainView = this.gvMain;
            this.dgvMain.MultiSelect = true;
            this.dgvMain.Name = "dgvMain";
            this.dgvMain.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.chkAllowRead,
            this.chkAllowEdit,
            this.chkAllowNew,
            this.chkAllowDelete,
            this.cboType,
            this.lkpOverride,
            this.chkSelect});
            this.dgvMain.ShowGroupPanel = false;
            this.dgvMain.ShowNewRow = false;
            this.dgvMain.UseEmbeddedNavigator = true;
            this.dgvMain.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvMain});
            // 
            // gvMain
            // 
            this.gvMain.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colSelect,
            this.colName,
            this.colAppId,
            this.colType,
            this.colAllowRead,
            this.colAllowEdit,
            this.colAllowNew,
            this.colAllowDelete,
            this.colOverrideId});
            this.gvMain.GridControl = this.dgvMain;
            this.gvMain.Name = "gvMain";
            this.gvMain.OptionsDetail.AllowZoomDetail = false;
            this.gvMain.OptionsDetail.EnableMasterViewMode = false;
            this.gvMain.OptionsView.ShowGroupPanel = false;
            this.gvMain.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(this.gvMain_CustomUnboundColumnData);
            // 
            // colSelect
            // 
            resources.ApplyResources(this.colSelect, "colSelect");
            this.colSelect.ColumnEdit = this.chkSelect;
            this.colSelect.FieldName = "colSelect";
            this.colSelect.Name = "colSelect";
            this.colSelect.OptionsColumn.FixedWidth = true;
            this.colSelect.UnboundType = DevExpress.Data.UnboundColumnType.Boolean;
            // 
            // chkSelect
            // 
            resources.ApplyResources(this.chkSelect, "chkSelect");
            this.chkSelect.Name = "chkSelect";
            this.chkSelect.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            // 
            // colName
            // 
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colName, "colName");
            // 
            // colAppId
            // 
            this.colAppId.FieldName = "AppId";
            this.colAppId.Name = "colAppId";
            this.colAppId.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colAppId, "colAppId");
            // 
            // colType
            // 
            this.colType.ColumnEdit = this.cboType;
            this.colType.FieldName = "Type";
            this.colType.Name = "colType";
            this.colType.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colType, "colType");
            // 
            // cboType
            // 
            resources.ApplyResources(this.cboType, "cboType");
            this.cboType.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("cboType.Buttons"))))});
            this.cboType.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] {
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo(resources.GetString("cboType.Columns"), resources.GetString("cboType.Columns1"))});
            this.cboType.DisplayMember = "Description";
            this.cboType.DropDownRows = 3;
            this.cboType.KeyTypeCode = System.TypeCode.Byte;
            this.cboType.KeyValuePair = "1;Setup and Maintenance;2;Transaction;3;Other";
            this.cboType.Name = "cboType";
            this.cboType.SetupFunctionId = 0;
            this.cboType.ShowFooter = false;
            this.cboType.ShowHeader = false;
            this.cboType.ValueMember = "Id";
            // 
            // colAllowRead
            // 
            this.colAllowRead.ColumnEdit = this.chkAllowRead;
            this.colAllowRead.FieldName = "AllowRead";
            this.colAllowRead.Name = "colAllowRead";
            this.colAllowRead.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colAllowRead, "colAllowRead");
            // 
            // chkAllowRead
            // 
            resources.ApplyResources(this.chkAllowRead, "chkAllowRead");
            this.chkAllowRead.Name = "chkAllowRead";
            this.chkAllowRead.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            // 
            // colAllowEdit
            // 
            this.colAllowEdit.ColumnEdit = this.chkAllowEdit;
            this.colAllowEdit.FieldName = "AllowEdit";
            this.colAllowEdit.Name = "colAllowEdit";
            this.colAllowEdit.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colAllowEdit, "colAllowEdit");
            // 
            // chkAllowEdit
            // 
            resources.ApplyResources(this.chkAllowEdit, "chkAllowEdit");
            this.chkAllowEdit.Name = "chkAllowEdit";
            this.chkAllowEdit.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            // 
            // colAllowNew
            // 
            this.colAllowNew.ColumnEdit = this.chkAllowNew;
            this.colAllowNew.FieldName = "AllowNew";
            this.colAllowNew.Name = "colAllowNew";
            this.colAllowNew.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colAllowNew, "colAllowNew");
            // 
            // chkAllowNew
            // 
            resources.ApplyResources(this.chkAllowNew, "chkAllowNew");
            this.chkAllowNew.Name = "chkAllowNew";
            this.chkAllowNew.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            // 
            // colAllowDelete
            // 
            this.colAllowDelete.ColumnEdit = this.chkAllowDelete;
            this.colAllowDelete.FieldName = "AllowDelete";
            this.colAllowDelete.Name = "colAllowDelete";
            this.colAllowDelete.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colAllowDelete, "colAllowDelete");
            // 
            // chkAllowDelete
            // 
            resources.ApplyResources(this.chkAllowDelete, "chkAllowDelete");
            this.chkAllowDelete.Name = "chkAllowDelete";
            this.chkAllowDelete.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            // 
            // colOverrideId
            // 
            resources.ApplyResources(this.colOverrideId, "colOverrideId");
            this.colOverrideId.ColumnEdit = this.lkpOverride;
            this.colOverrideId.FieldName = "OverrideId";
            this.colOverrideId.Name = "colOverrideId";
            this.colOverrideId.OptionsColumn.ReadOnly = true;
            // 
            // lkpOverride
            // 
            resources.ApplyResources(this.lkpOverride, "lkpOverride");
            this.lkpOverride.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton()});
            this.lkpOverride.DisablePopup = false;
            this.lkpOverride.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Buffered;
            this.lkpOverride.LimitToList = false;
            this.lkpOverride.LookupDefinitionId = null;
            this.lkpOverride.Name = "lkpOverride";
            this.lkpOverride.PopupFormMinSize = new System.Drawing.Size(500, 50);
            this.lkpOverride.PopupView = this.repositoryItemTravLookupControl1View;
            this.lkpOverride.SetupFunctionId = 0;
            this.lkpOverride.SimpleMask = null;
            this.lkpOverride.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
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
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.itemFunction});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Size = new System.Drawing.Size(739, 448);
            this.grpRoot.TextVisible = false;
            // 
            // itemFunction
            // 
            this.itemFunction.Control = this.dgvMain;
            this.itemFunction.Location = new System.Drawing.Point(0, 0);
            this.itemFunction.Name = "itemFunction";
            this.itemFunction.Size = new System.Drawing.Size(719, 428);
            this.itemFunction.TextSize = new System.Drawing.Size(0, 0);
            this.itemFunction.TextVisible = false;
            // 
            // fbdScript
            // 
            this.fbdScript.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // ApiBuildMaintScriptControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BindingSource = this.bindMain;
            this.FunctionId = "A70061A4-33B0-47A8-A6DE-61B9309DB4BC";
            this.HandleListChangedEvent = true;
            this.Name = "ApiBuildMaintScriptControl";
            this.splitContainerProcess.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerProcess)).EndInit();
            this.splitContainerProcess.ResumeLayout(false);
            this.splitContainerBase.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).EndInit();
            this.splitContainerBase.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkSelect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboType)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowRead)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowNew)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowDelete)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lkpOverride)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTravLookupControl1View)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemFunction)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private TRAVERSE.Controls.TravDataGridViewControl dgvMain;
        private System.Windows.Forms.BindingSource bindMain;
        private DevExpress.XtraGrid.Views.Grid.GridView gvMain;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colAppId;
        private DevExpress.XtraGrid.Columns.GridColumn colType;
        private DevExpress.XtraGrid.Columns.GridColumn colAllowRead;
        private DevExpress.XtraGrid.Columns.GridColumn colAllowEdit;
        private DevExpress.XtraGrid.Columns.GridColumn colAllowNew;
        private DevExpress.XtraGrid.Columns.GridColumn colAllowDelete;
        private DevExpress.XtraGrid.Columns.GridColumn colOverrideId;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private DevExpress.XtraLayout.LayoutControlItem itemFunction;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkAllowRead;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkAllowEdit;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkAllowNew;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkAllowDelete;
        private TRAVERSE.Controls.RepositoryItemTravEnumListControl cboType;
        private DevExpress.XtraGrid.Columns.GridColumn colSelect;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkSelect;
        private TRAVERSE.Controls.RepositoryItemTravLookupControl lkpOverride;
        private DevExpress.XtraGrid.Views.Grid.GridView repositoryItemTravLookupControl1View;
        private System.Windows.Forms.FolderBrowserDialog fbdScript;
    }
}
