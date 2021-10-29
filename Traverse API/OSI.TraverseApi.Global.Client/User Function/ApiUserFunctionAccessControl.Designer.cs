namespace OSI.TraverseApi.Client
{
    partial class ApiUserFunctionAccessControl
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
                if (bindUserFunction != null)
                {
                    bindUserFunction.AddingNew -= BindingSourceAddingNew;
                    bindUserFunction.CurrentChanged -= BindingSourceCurrentChanged;
                }

                if (btnMultiFunction != null)
                    btnMultiFunction.Click -= btnMultiFunction_Click;

                if (gvUserFunction != null)
                    gvUserFunction.CellValueChanged -= gvUserFunction_CellValueChanged;

                if (navMain != null)
                    navMain.Save -= navMain_Save;

                if (components != null)
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApiUserFunctionAccessControl));
            this.bindUserFunction = new System.Windows.Forms.BindingSource(this.components);
            this.navMain = new TRAVERSE.Controls.TravNavigator(this.components);
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnMultiFunction = new TRAVERSE.Controls.TravNavItem();
            this.dgvUserFunction = new TRAVERSE.Controls.TravDataGridViewControl();
            this.gvUserFunction = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colFunctionId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.lkpFunctionId = new TRAVERSE.Controls.RepositoryItemTravLookupControl();
            this.colAccessExpireDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.dtAccessExpire = new TRAVERSE.Controls.RepositoryItemTravDateControl();
            this.colLastAccess = new DevExpress.XtraGrid.Columns.GridColumn();
            this.layoutMain = new TRAVERSE.Controls.TravLayoutControl();
            this.grpRoot = new DevExpress.XtraLayout.LayoutControlGroup();
            this.itemUserFunction = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).BeginInit();
            this.splitContainerBase.Panel1.SuspendLayout();
            this.splitContainerBase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindUserFunction)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.navMain)).BeginInit();
            this.navMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUserFunction)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvUserFunction)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lkpFunctionId)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtAccessExpire)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtAccessExpire.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).BeginInit();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemUserFunction)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainerBase
            // 
            // 
            // splitContainerBase.Panel1
            // 
            this.splitContainerBase.Panel1.Controls.Add(this.layoutMain);
            this.splitContainerBase.Panel1.Controls.Add(this.navMain);
            // 
            // errorProviderBase
            // 
            this.errorProviderBase.DataSource = this.bindUserFunction;
            // 
            // bindUserFunction
            // 
            this.bindUserFunction.DataSource = typeof(OSI.TraverseApi.Business.ApiUserFunction);
            this.bindUserFunction.AddingNew += new System.ComponentModel.AddingNewEventHandler(this.BindingSourceAddingNew);
            this.bindUserFunction.CurrentChanged += new System.EventHandler(this.BindingSourceCurrentChanged);
            // 
            // navMain
            // 
            this.navMain.BindingSource = this.bindUserFunction;
            this.navMain.CountFormat = "of {0}";
            this.navMain.DisableDataEvents = false;
            this.navMain.DisplayDeleteVerification = true;
            this.navMain.Font = new System.Drawing.Font("Arial Unicode MS", 9F);
            this.navMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.navMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1,
            this.btnMultiFunction});
            this.navMain.Location = new System.Drawing.Point(0, 0);
            this.navMain.Name = "navMain";
            this.navMain.Size = new System.Drawing.Size(800, 25);
            this.navMain.SourceControl = this.dgvUserFunction;
            this.navMain.TabIndex = 0;
            this.navMain.Text = "travNavigator1";
            this.navMain.Save += new System.EventHandler<TRAVERSE.Controls.TravNavigatorEventArgs>(this.navMain_Save);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnMultiFunction
            // 
            this.btnMultiFunction.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnMultiFunction.Image = ((System.Drawing.Image)(resources.GetObject("btnMultiFunction.Image")));
            this.btnMultiFunction.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnMultiFunction.Name = "btnMultiFunction";
            this.btnMultiFunction.Size = new System.Drawing.Size(80, 22);
            this.btnMultiFunction.Text = "&Quick Select";
            this.btnMultiFunction.Click += new System.EventHandler(this.btnMultiFunction_Click);
            // 
            // dgvUserFunction
            // 
            this.dgvUserFunction.DataSource = this.bindUserFunction;
            this.dgvUserFunction.Location = new System.Drawing.Point(4, 4);
            this.dgvUserFunction.MainView = this.gvUserFunction;
            this.dgvUserFunction.MultiSelect = true;
            this.dgvUserFunction.Name = "dgvUserFunction";
            this.dgvUserFunction.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.lkpFunctionId,
            this.dtAccessExpire});
            this.dgvUserFunction.ShowGroupPanel = false;
            this.dgvUserFunction.ShowNewRow = true;
            this.dgvUserFunction.Size = new System.Drawing.Size(792, 567);
            this.dgvUserFunction.TabIndex = 4;
            this.dgvUserFunction.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvUserFunction});
            // 
            // gvUserFunction
            // 
            this.gvUserFunction.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colFunctionId,
            this.colAccessExpireDate,
            this.colLastAccess});
            this.gvUserFunction.GridControl = this.dgvUserFunction;
            this.gvUserFunction.Name = "gvUserFunction";
            this.gvUserFunction.OptionsDetail.AllowZoomDetail = false;
            this.gvUserFunction.OptionsDetail.EnableMasterViewMode = false;
            this.gvUserFunction.OptionsDetail.ShowDetailTabs = false;
            this.gvUserFunction.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom;
            this.gvUserFunction.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowForFocusedRow;
            this.gvUserFunction.OptionsView.ShowDetailButtons = false;
            this.gvUserFunction.OptionsView.ShowGroupPanel = false;
            this.gvUserFunction.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.gvUserFunction_CellValueChanged);
            // 
            // colFunctionId
            // 
            this.colFunctionId.Caption = "Function Name";
            this.colFunctionId.ColumnEdit = this.lkpFunctionId;
            this.colFunctionId.FieldName = "FunctionId";
            this.colFunctionId.Name = "colFunctionId";
            this.colFunctionId.Visible = true;
            this.colFunctionId.VisibleIndex = 0;
            // 
            // lkpFunctionId
            // 
            this.lkpFunctionId.AutoHeight = false;
            this.lkpFunctionId.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.lkpFunctionId.DisablePopup = false;
            this.lkpFunctionId.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Buffered;
            this.lkpFunctionId.LimitToList = false;
            this.lkpFunctionId.LookupDefinitionId = null;
            this.lkpFunctionId.Name = "lkpFunctionId";
            this.lkpFunctionId.NullText = "";
            this.lkpFunctionId.PopupFormMinSize = new System.Drawing.Size(500, 50);
            this.lkpFunctionId.SetupFunctionId = 0;
            this.lkpFunctionId.SimpleMask = null;
            this.lkpFunctionId.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            // 
            // colAccessExpireDate
            // 
            this.colAccessExpireDate.Caption = "Expiration Date";
            this.colAccessExpireDate.ColumnEdit = this.dtAccessExpire;
            this.colAccessExpireDate.FieldName = "AccessExpireDate";
            this.colAccessExpireDate.Name = "colAccessExpireDate";
            this.colAccessExpireDate.Visible = true;
            this.colAccessExpireDate.VisibleIndex = 1;
            // 
            // dtAccessExpire
            // 
            this.dtAccessExpire.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            this.dtAccessExpire.AutoHeight = false;
            this.dtAccessExpire.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dtAccessExpire.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dtAccessExpire.MaxValue = new System.DateTime(9999, 12, 31, 23, 59, 59, 0);
            this.dtAccessExpire.MinValue = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtAccessExpire.Name = "dtAccessExpire";
            this.dtAccessExpire.SetupFunctionId = 0;
            // 
            // colLastAccess
            // 
            this.colLastAccess.Caption = "Last Access";
            this.colLastAccess.DisplayFormat.FormatString = "G";
            this.colLastAccess.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.colLastAccess.FieldName = "LastAccess";
            this.colLastAccess.Name = "colLastAccess";
            this.colLastAccess.OptionsColumn.ReadOnly = true;
            this.colLastAccess.Visible = true;
            this.colLastAccess.VisibleIndex = 2;
            // 
            // layoutMain
            // 
            this.layoutMain.AllowCustomization = false;
            this.layoutMain.Controls.Add(this.dgvUserFunction);
            this.layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutMain.Location = new System.Drawing.Point(0, 25);
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.OptionsFocus.MoveFocusDirection = DevExpress.XtraLayout.MoveFocusDirection.DownThenAcross;
            this.layoutMain.Root = this.grpRoot;
            this.layoutMain.Size = new System.Drawing.Size(800, 575);
            this.layoutMain.TabIndex = 1;
            this.layoutMain.Text = "travLayoutControl1";
            // 
            // grpRoot
            // 
            this.grpRoot.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.grpRoot.GroupBordersVisible = false;
            this.grpRoot.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.itemUserFunction});
            this.grpRoot.Name = "grpRoot";
            this.grpRoot.Padding = new DevExpress.XtraLayout.Utils.Padding(2, 2, 2, 2);
            this.grpRoot.Size = new System.Drawing.Size(800, 575);
            this.grpRoot.TextVisible = false;
            // 
            // itemUserFunction
            // 
            this.itemUserFunction.Control = this.dgvUserFunction;
            this.itemUserFunction.Location = new System.Drawing.Point(0, 0);
            this.itemUserFunction.Name = "itemUserFunction";
            this.itemUserFunction.Size = new System.Drawing.Size(796, 571);
            this.itemUserFunction.TextSize = new System.Drawing.Size(0, 0);
            this.itemUserFunction.TextVisible = false;
            // 
            // ApiUserFunctionAccessControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BindingSource = this.bindUserFunction;
            this.FunctionId = "D146E9FA-2687-452E-84FF-0EBA8454F7FB";
            this.HandleListChangedEvent = true;
            this.Name = "ApiUserFunctionAccessControl";
            this.Navigator = this.navMain;
            this.splitContainerBase.Panel1.ResumeLayout(false);
            this.splitContainerBase.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).EndInit();
            this.splitContainerBase.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindUserFunction)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.navMain)).EndInit();
            this.navMain.ResumeLayout(false);
            this.navMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUserFunction)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvUserFunction)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lkpFunctionId)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtAccessExpire.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtAccessExpire)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutMain)).EndInit();
            this.layoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grpRoot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.itemUserFunction)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource bindUserFunction;
        private TRAVERSE.Controls.TravLayoutControl layoutMain;
        private TRAVERSE.Controls.TravDataGridViewControl dgvUserFunction;
        private DevExpress.XtraGrid.Views.Grid.GridView gvUserFunction;
        private DevExpress.XtraGrid.Columns.GridColumn colFunctionId;
        private DevExpress.XtraGrid.Columns.GridColumn colAccessExpireDate;
        private DevExpress.XtraGrid.Columns.GridColumn colLastAccess;
        private DevExpress.XtraLayout.LayoutControlGroup grpRoot;
        private DevExpress.XtraLayout.LayoutControlItem itemUserFunction;
        private TRAVERSE.Controls.TravNavigator navMain;
        private TRAVERSE.Controls.RepositoryItemTravLookupControl lkpFunctionId;
        private TRAVERSE.Controls.RepositoryItemTravDateControl dtAccessExpire;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private TRAVERSE.Controls.TravNavItem btnMultiFunction;
    }
}
