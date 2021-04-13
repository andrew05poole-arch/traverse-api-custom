namespace OSI.TraverseApi.Client
{
    partial class ApiUserFunctionCompanyControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiUserFunctionCompanyControl));
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.dgvCompany = new TRAVERSE.Controls.TravDataGridViewControl();
            this.bindCompany = new System.Windows.Forms.BindingSource(this.components);
            this.gvCompany = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colCompanyId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.cboCompanyId = new TRAVERSE.Controls.RepositoryItemTravComboBoxAdv();
            this.colAllowRead = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkAllowRead = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colAllowEdit = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkAllowEdit = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colAllowNew = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkAllowNew = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colAllowDelete = new DevExpress.XtraGrid.Columns.GridColumn();
            this.chkAllowDelete = new TRAVERSE.Controls.RepositoryItemTravCheckBox();
            this.colDisplayFilter = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtFilter = new TRAVERSE.Controls.RepositoryItemTravTextBox();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).BeginInit();
            this.splitContainerBase.Panel1.SuspendLayout();
            this.splitContainerBase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCompany)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindCompany)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvCompany)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboCompanyId)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowRead)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowNew)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowDelete)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFilter)).BeginInit();
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
            this.errorProviderBase.DataSource = this.bindCompany;
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.dgvCompany);
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            // 
            // dgvCompany
            // 
            this.dgvCompany.DataSource = this.bindCompany;
            resources.ApplyResources(this.dgvCompany, "dgvCompany");
            this.dgvCompany.MainView = this.gvCompany;
            this.dgvCompany.MultiSelect = true;
            this.dgvCompany.Name = "dgvCompany";
            this.dgvCompany.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.cboCompanyId,
            this.chkAllowRead,
            this.chkAllowEdit,
            this.chkAllowNew,
            this.chkAllowDelete,
            this.txtFilter});
            this.dgvCompany.ShowGroupPanel = false;
            this.dgvCompany.ShowNewRow = true;
            this.dgvCompany.UseEmbeddedNavigator = true;
            this.dgvCompany.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvCompany});
            // 
            // bindCompany
            // 
            this.bindCompany.DataSource = typeof(OSI.TraverseApi.Business.ApiUserFunctionComp);
            this.bindCompany.AddingNew += new System.ComponentModel.AddingNewEventHandler(this.BindingSourceAddingNew);
            this.bindCompany.CurrentChanged += new System.EventHandler(this.BindingSourceCurrentChanged);
            // 
            // gvCompany
            // 
            this.gvCompany.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colCompanyId,
            this.colAllowRead,
            this.colAllowEdit,
            this.colAllowNew,
            this.colAllowDelete,
            this.colDisplayFilter});
            this.gvCompany.GridControl = this.dgvCompany;
            this.gvCompany.Name = "gvCompany";
            this.gvCompany.OptionsDetail.AllowZoomDetail = false;
            this.gvCompany.OptionsDetail.EnableMasterViewMode = false;
            this.gvCompany.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom;
            this.gvCompany.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            this.gvCompany.OptionsView.ShowGroupPanel = false;
            // 
            // colCompanyId
            // 
            resources.ApplyResources(this.colCompanyId, "colCompanyId");
            this.colCompanyId.ColumnEdit = this.cboCompanyId;
            this.colCompanyId.FieldName = "CompanyId";
            this.colCompanyId.Name = "colCompanyId";
            // 
            // cboCompanyId
            // 
            this.cboCompanyId.AcceptEditorTextAsNewValue = DevExpress.Utils.DefaultBoolean.False;
            resources.ApplyResources(this.cboCompanyId, "cboCompanyId");
            this.cboCompanyId.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(((DevExpress.XtraEditors.Controls.ButtonPredefines)(resources.GetObject("cboCompanyId.Buttons"))))});
            this.cboCompanyId.Name = "cboCompanyId";
            this.cboCompanyId.SetupFunctionId = 0;
            this.cboCompanyId.ShowHeader = false;
            // 
            // colAllowRead
            // 
            this.colAllowRead.ColumnEdit = this.chkAllowRead;
            this.colAllowRead.FieldName = "AllowRead";
            this.colAllowRead.Name = "colAllowRead";
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
            resources.ApplyResources(this.colAllowDelete, "colAllowDelete");
            // 
            // chkAllowDelete
            // 
            resources.ApplyResources(this.chkAllowDelete, "chkAllowDelete");
            this.chkAllowDelete.Name = "chkAllowDelete";
            this.chkAllowDelete.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            // 
            // colDisplayFilter
            // 
            resources.ApplyResources(this.colDisplayFilter, "colDisplayFilter");
            this.colDisplayFilter.ColumnEdit = this.txtFilter;
            this.colDisplayFilter.FieldName = "DisplayFilter";
            this.colDisplayFilter.Name = "colDisplayFilter";
            // 
            // txtFilter
            // 
            this.txtFilter.Appearance.Options.UseTextOptions = true;
            resources.ApplyResources(this.txtFilter, "txtFilter");
            this.txtFilter.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton()});
            this.txtFilter.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Buffered;
            this.txtFilter.FixedPointFormat = false;
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.NumericControl = false;
            this.txtFilter.NumericPrecision = -1;
            this.txtFilter.ReadOnly = true;
            this.txtFilter.SetupFunctionId = 0;
            this.txtFilter.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(this.DisplayFilterClick);
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Padding = new DevExpress.XtraLayout.Utils.Padding(1, 1, 1, 1);
            this.grpRoot.Size = new System.Drawing.Size(800, 600);
            this.grpRoot.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.dgvCompany;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(798, 598);
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextVisible = false;
            // 
            // ApiUserFunctionCompanyControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BindingSource = this.bindCompany;
            this.FunctionId = "33A4AFFF-349A-4979-9653-D3D91FA3F93A";
            this.HandleListChangedEvent = true;
            this.Name = "ApiUserFunctionCompanyControl";
            this.splitContainerBase.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).EndInit();
            this.splitContainerBase.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCompany)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindCompany)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvCompany)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboCompanyId)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowRead)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowNew)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkAllowDelete)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private TRAVERSE.Controls.TravDataGridViewControl dgvCompany;
        private System.Windows.Forms.BindingSource bindCompany;
        private DevExpress.XtraGrid.Views.Grid.GridView gvCompany;
        private DevExpress.XtraGrid.Columns.GridColumn colCompanyId;
        private DevExpress.XtraGrid.Columns.GridColumn colAllowRead;
        private DevExpress.XtraGrid.Columns.GridColumn colAllowEdit;
        private DevExpress.XtraGrid.Columns.GridColumn colAllowNew;
        private DevExpress.XtraGrid.Columns.GridColumn colAllowDelete;
        private DevExpress.XtraGrid.Columns.GridColumn colDisplayFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private TRAVERSE.Controls.RepositoryItemTravComboBoxAdv cboCompanyId;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkAllowRead;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkAllowEdit;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkAllowNew;
        private TRAVERSE.Controls.RepositoryItemTravCheckBox chkAllowDelete;
        private TRAVERSE.Controls.RepositoryItemTravTextBox txtFilter;
    }
}
