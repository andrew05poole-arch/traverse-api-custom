namespace OSI.TraverseApi.Client
{
    partial class ApiUserMultiFunctionControl
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
            if (disposing)
            {
                //if (btnSelectAll != null)
                //    btnSelectAll.TravNavItemClick -= ButtonSelectClick;

                //if (btnSelectNone != null)
                //    btnSelectNone.TravNavItemClick -= ButtonSelectClick;

                if (components != null)
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiUserMultiFunctionControl));
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.chkAllowDelete = new TRAVERSE.Controls.TravCheckBox();
            this.chkAllowEdit = new TRAVERSE.Controls.TravCheckBox();
            this.chkAllowCreate = new TRAVERSE.Controls.TravCheckBox();
            this.chkAllowRead = new TRAVERSE.Controls.TravCheckBox();
            this.dgvFunctionList = new TRAVERSE.Controls.TravDataGridViewControl();
            this.bindMain = new System.Windows.Forms.BindingSource(this.components);
            this.gvFunctionList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colSelect = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkSelect = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colAppId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.cboFuncType = new TRAVERSE.Controls.RepositoryItemTravEnumListControl();
            this.lstCompany = new TRAVERSE.Controls.TravListBox();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.itemCompanyList = new DevExpress.XtraLayout.LayoutControlItem();
            this.itemSelectedList = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.grpPermissions = new DevExpress.XtraLayout.LayoutControlGroup();
            this.itemAllowDelete = new DevExpress.XtraLayout.LayoutControlItem();
            this.itemAllowCreate = new DevExpress.XtraLayout.LayoutControlItem();
            this.itemAllowEdit = new DevExpress.XtraLayout.LayoutControlItem();
            this.itemAllowRead = new DevExpress.XtraLayout.LayoutControlItem();
            this.dtAccessExpire = new TRAVERSE.Controls.TravDateControl();
            this.itemExpireDate = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerProcess)).BeginInit();
            this.splitContainerProcess.Panel2.SuspendLayout();
            this.splitContainerProcess.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).BeginInit();
            this.splitContainerBase.Panel1.SuspendLayout();
            this.splitContainerBase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowDelete.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowEdit.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowCreate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowRead.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFunctionList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvFunctionList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkSelect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboFuncType)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemCompanyList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemSelectedList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpPermissions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemAllowDelete)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemAllowCreate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemAllowEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemAllowRead)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtAccessExpire.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtAccessExpire.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemExpireDate)).BeginInit();
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
            resources.ApplyResources(this.splitContainerBase, "splitContainerBase");
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.dtAccessExpire);
            this.layoutMain.Controls.Add(this.chkAllowDelete);
            this.layoutMain.Controls.Add(this.chkAllowEdit);
            this.layoutMain.Controls.Add(this.chkAllowCreate);
            this.layoutMain.Controls.Add(this.chkAllowRead);
            this.layoutMain.Controls.Add(this.dgvFunctionList);
            this.layoutMain.Controls.Add(this.lstCompany);
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            // 
            // chkAllowDelete
            // 
            resources.ApplyResources(this.chkAllowDelete, "chkAllowDelete");
            this.chkAllowDelete.Name = "chkAllowDelete";
            this.chkAllowDelete.Properties.Caption = resources.GetString("chkAllowDelete.Properties.Caption");
            this.chkAllowDelete.SecurityId = null;
            this.chkAllowDelete.StyleController = this.layoutMain;
            // 
            // chkAllowEdit
            // 
            resources.ApplyResources(this.chkAllowEdit, "chkAllowEdit");
            this.chkAllowEdit.Name = "chkAllowEdit";
            this.chkAllowEdit.Properties.Caption = resources.GetString("chkAllowEdit.Properties.Caption");
            this.chkAllowEdit.SecurityId = null;
            this.chkAllowEdit.StyleController = this.layoutMain;
            // 
            // chkAllowCreate
            // 
            resources.ApplyResources(this.chkAllowCreate, "chkAllowCreate");
            this.chkAllowCreate.Name = "chkAllowCreate";
            this.chkAllowCreate.Properties.Caption = resources.GetString("chkAllowCreate.Properties.Caption");
            this.chkAllowCreate.SecurityId = null;
            this.chkAllowCreate.StyleController = this.layoutMain;
            // 
            // chkAllowRead
            // 
            resources.ApplyResources(this.chkAllowRead, "chkAllowRead");
            this.chkAllowRead.Name = "chkAllowRead";
            this.chkAllowRead.Properties.Caption = resources.GetString("chkAllowRead.Properties.Caption");
            this.chkAllowRead.SecurityId = null;
            this.chkAllowRead.StyleController = this.layoutMain;
            // 
            // dgvFunctionList
            // 
            this.dgvFunctionList.DataSource = this.bindMain;
            resources.ApplyResources(this.dgvFunctionList, "dgvFunctionList");
            this.dgvFunctionList.MainView = this.gvFunctionList;
            this.dgvFunctionList.MultiSelect = true;
            this.dgvFunctionList.Name = "dgvFunctionList";
            this.dgvFunctionList.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.chkSelect,
            this.cboFuncType});
            this.dgvFunctionList.ShowGroupPanel = false;
            this.dgvFunctionList.ShowNewRow = false;
            this.dgvFunctionList.UseEmbeddedNavigator = true;
            this.dgvFunctionList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvFunctionList});
            // 
            // bindMain
            // 
            this.bindMain.AllowNew = false;
            this.bindMain.DataSource = typeof(OSI.TraverseApi.Business.ApiFunctionHeader);
            // 
            // gvFunctionList
            // 
            this.gvFunctionList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colSelect,
            this.colAppId,
            this.colName,
            this.colType});
            this.gvFunctionList.GridControl = this.dgvFunctionList;
            this.gvFunctionList.Name = "gvFunctionList";
            this.gvFunctionList.OptionsBehavior.AllowAddRows = DevExpress.Utils.DefaultBoolean.False;
            this.gvFunctionList.OptionsBehavior.AllowDeleteRows = DevExpress.Utils.DefaultBoolean.False;
            this.gvFunctionList.OptionsDetail.AllowZoomDetail = false;
            this.gvFunctionList.OptionsDetail.EnableMasterViewMode = false;
            this.gvFunctionList.OptionsView.ShowAutoFilterRow = true;
            this.gvFunctionList.OptionsView.ShowGroupPanel = false;
            this.gvFunctionList.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(this.gvFunctionList_CustomUnboundColumnData);
            // 
            // colSelect
            // 
            resources.ApplyResources(this.colSelect, "colSelect");
            this.colSelect.ColumnEdit = this.chkSelect;
            this.colSelect.FieldName = "colSelect";
            this.colSelect.Name = "colSelect";
            this.colSelect.UnboundType = DevExpress.Data.UnboundColumnType.Boolean;
            // 
            // chkSelect
            // 
            resources.ApplyResources(this.chkSelect, "chkSelect");
            this.chkSelect.Name = "chkSelect";
            this.chkSelect.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            // 
            // colAppId
            // 
            resources.ApplyResources(this.colAppId, "colAppId");
            this.colAppId.FieldName = "AppId";
            this.colAppId.Name = "colAppId";
            this.colAppId.OptionsColumn.ReadOnly = true;
            // 
            // colName
            // 
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colName, "colName");
            // 
            // colType
            // 
            this.colType.ColumnEdit = this.cboFuncType;
            this.colType.FieldName = "Type";
            this.colType.Name = "colType";
            this.colType.OptionsColumn.ReadOnly = true;
            resources.ApplyResources(this.colType, "colType");
            // 
            // cboFuncType
            // 
            resources.ApplyResources(this.cboFuncType, "cboFuncType");
            this.cboFuncType.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("cboFuncType.Buttons"))))});
            this.cboFuncType.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] {
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo(resources.GetString("cboFuncType.Columns"), resources.GetString("cboFuncType.Columns1"))});
            this.cboFuncType.DisplayMember = "Description";
            this.cboFuncType.DropDownRows = 3;
            this.cboFuncType.KeyTypeCode = System.TypeCode.Byte;
            this.cboFuncType.KeyValuePair = "1;Setup and Maintenance;2;Transaction;3;Other";
            this.cboFuncType.Name = "cboFuncType";
            this.cboFuncType.SetupFunctionId = 0;
            this.cboFuncType.ShowFooter = false;
            this.cboFuncType.ShowHeader = false;
            this.cboFuncType.ValueMember = "Id";
            // 
            // lstCompany
            // 
            this.lstCompany.ButtonsVisible = true;
            this.lstCompany.CaptionVisible = true;
            this.lstCompany.DataSource = null;
            resources.ApplyResources(this.lstCompany, "lstCompany");
            this.lstCompany.Name = "lstCompany";
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.itemCompanyList,
            this.itemSelectedList,
            this.emptySpaceItem1,
            this.grpPermissions});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Padding = new DevExpress.XtraLayout.Utils.Padding(3, 3, 3, 3);
            this.grpRoot.Size = new System.Drawing.Size(784, 536);
            this.grpRoot.TextVisible = false;
            // 
            // itemCompanyList
            // 
            this.itemCompanyList.Control = this.lstCompany;
            this.itemCompanyList.Location = new System.Drawing.Point(0, 0);
            this.itemCompanyList.Name = "itemCompanyList";
            this.itemCompanyList.Size = new System.Drawing.Size(389, 182);
            this.itemCompanyList.TextSize = new System.Drawing.Size(0, 0);
            this.itemCompanyList.TextVisible = false;
            // 
            // itemSelectedList
            // 
            this.itemSelectedList.Control = this.dgvFunctionList;
            this.itemSelectedList.Location = new System.Drawing.Point(0, 182);
            this.itemSelectedList.Name = "itemSelectedList";
            this.itemSelectedList.Size = new System.Drawing.Size(778, 348);
            this.itemSelectedList.TextSize = new System.Drawing.Size(0, 0);
            this.itemSelectedList.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(389, 158);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(389, 24);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // grpPermissions
            // 
            this.grpPermissions.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.itemAllowDelete,
            this.itemAllowCreate,
            this.itemAllowEdit,
            this.itemAllowRead,
            this.itemExpireDate});
            this.grpPermissions.Location = new System.Drawing.Point(389, 0);
            this.grpPermissions.Name = "grpPermissions";
            this.grpPermissions.Size = new System.Drawing.Size(389, 158);
            resources.ApplyResources(this.grpPermissions, "grpPermissions");
            // 
            // itemAllowDelete
            // 
            this.itemAllowDelete.Control = this.chkAllowDelete;
            this.itemAllowDelete.Location = new System.Drawing.Point(0, 69);
            this.itemAllowDelete.Name = "itemAllowDelete";
            this.itemAllowDelete.Size = new System.Drawing.Size(365, 23);
            this.itemAllowDelete.TextSize = new System.Drawing.Size(0, 0);
            this.itemAllowDelete.TextVisible = false;
            // 
            // itemAllowCreate
            // 
            this.itemAllowCreate.Control = this.chkAllowCreate;
            this.itemAllowCreate.Location = new System.Drawing.Point(0, 46);
            this.itemAllowCreate.Name = "itemAllowCreate";
            this.itemAllowCreate.Size = new System.Drawing.Size(365, 23);
            this.itemAllowCreate.TextSize = new System.Drawing.Size(0, 0);
            this.itemAllowCreate.TextVisible = false;
            // 
            // itemAllowEdit
            // 
            this.itemAllowEdit.Control = this.chkAllowEdit;
            this.itemAllowEdit.Location = new System.Drawing.Point(0, 23);
            this.itemAllowEdit.Name = "itemAllowEdit";
            this.itemAllowEdit.Size = new System.Drawing.Size(365, 23);
            this.itemAllowEdit.TextSize = new System.Drawing.Size(0, 0);
            this.itemAllowEdit.TextVisible = false;
            // 
            // itemAllowRead
            // 
            this.itemAllowRead.Control = this.chkAllowRead;
            this.itemAllowRead.Location = new System.Drawing.Point(0, 0);
            this.itemAllowRead.Name = "itemAllowRead";
            this.itemAllowRead.Size = new System.Drawing.Size(365, 23);
            this.itemAllowRead.TextSize = new System.Drawing.Size(0, 0);
            this.itemAllowRead.TextVisible = false;
            // 
            // dtAccessExpire
            // 
            resources.ApplyResources(this.dtAccessExpire, "dtAccessExpire");
            this.dtAccessExpire.Name = "dtAccessExpire";
            this.dtAccessExpire.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            this.dtAccessExpire.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("travDateControl1.Properties.Buttons"))))});
            this.dtAccessExpire.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("travDateControl1.Properties.CalendarTimeProperties.Buttons"))))});
            this.dtAccessExpire.Properties.MaxValue = new System.DateTime(9999, 12, 31, 23, 59, 59, 0);
            this.dtAccessExpire.Properties.MinValue = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtAccessExpire.Properties.SetupFunctionId = 0;
            this.dtAccessExpire.SecurityId = null;
            this.dtAccessExpire.StyleController = this.layoutMain;
            // 
            // itemExpireDate
            // 
            this.itemExpireDate.Control = this.dtAccessExpire;
            this.itemExpireDate.Location = new System.Drawing.Point(0, 92);
            this.itemExpireDate.Name = "itemExpireDate";
            this.itemExpireDate.Size = new System.Drawing.Size(365, 24);
            resources.ApplyResources(this.itemExpireDate, "itemExpireDate");
            this.itemExpireDate.TextSize = new System.Drawing.Size(71, 13);
            // 
            // ApiUserMultiFunctionControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "ApiUserMultiFunctionControl";
            this.splitContainerProcess.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerProcess)).EndInit();
            this.splitContainerProcess.ResumeLayout(false);
            this.splitContainerBase.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).EndInit();
            this.splitContainerBase.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowDelete.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowEdit.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowCreate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowRead.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFunctionList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvFunctionList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkSelect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboFuncType)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemCompanyList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemSelectedList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpPermissions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemAllowDelete)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemAllowCreate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemAllowEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemAllowRead)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtAccessExpire.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtAccessExpire.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemExpireDate)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private TRAVERSE.Controls.TravCheckBox chkAllowDelete;
        private TRAVERSE.Controls.TravCheckBox chkAllowEdit;
        private TRAVERSE.Controls.TravCheckBox chkAllowCreate;
        private TRAVERSE.Controls.TravCheckBox chkAllowRead;
        private TRAVERSE.Controls.TravDataGridViewControl dgvFunctionList;
        private System.Windows.Forms.BindingSource bindMain;
        private DevExpress.XtraGrid.Views.Grid.GridView gvFunctionList;
        private DevExpress.XtraGrid.Columns.GridColumn colAppId;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colType;
        private TRAVERSE.Controls.RepositoryItemTravEnumListControl cboFuncType;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkSelect;
        private TRAVERSE.Controls.TravListBox lstCompany;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private DevExpress.XtraLayout.LayoutControlItem itemCompanyList;
        private DevExpress.XtraLayout.LayoutControlItem itemSelectedList;
        private DevExpress.XtraLayout.LayoutControlItem itemAllowRead;
        private DevExpress.XtraLayout.LayoutControlItem itemAllowCreate;
        private DevExpress.XtraLayout.LayoutControlItem itemAllowEdit;
        private DevExpress.XtraLayout.LayoutControlItem itemAllowDelete;
        private DevExpress.XtraGrid.Columns.GridColumn colSelect;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraLayout.LayoutControlGroup grpPermissions;
        private TRAVERSE.Controls.TravDateControl dtAccessExpire;
        private DevExpress.XtraLayout.LayoutControlItem itemExpireDate;
    }
}