#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using TRAVERSE.Business;
using TRAVERSE.Business.Sys;
using TRAVERSE.Client;
using TRAVERSE.Controls;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Client
{
    public partial class ApiUserMultiFunctionControl : ProcessControlBase
    {
        #region Fields
        private TravNavItem _btnSelectAll;
        private TravNavItem _btnSelectNone;
        private ApiCreateMultiFunction _processEngine;
        #endregion Fields

        #region Constructors
        public ApiUserMultiFunctionControl()
            : base()
        {
            InitializeComponent();
            this.AddItem<TravNavItem>(ButtonSelectAll);
            this.AddItem<TravNavItem>(ButtonSelectNone);
            this.ButtonActivity.Enabled = false;
            this.ButtonReset.Enabled = false;
        }
        #endregion Constructors

        #region Event Handlers
        private void ButtonSelectClick(object sender, TravNavigatorEventArgs e)
        {
            try
            {
                bool selectAll = (bool)((TravNavItem)sender).Tag;
                ManageSelection(selectAll);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void gvFunctionList_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            try
            {
                this.OngvFunctionList_CustomUnboundColumnData(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OngvFunctionList_CustomUnboundColumnData(DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            var function = e.Row as ApiFunctionHeader;
            if (e.Column == this.colSelect)
            {
                bool exists = this.FunctionList.Exists(f => f.Id == function.Id);

                if (e.IsGetData)
                    e.Value = exists;

                if (e.IsSetData)
                {
                    if ((bool)e.Value && !exists)
                        this.FunctionList.Add(function);
                    else if (!(bool)e.Value && exists)
                        this.FunctionList.Remove(function);
                }
            }
        }
        #endregion Event Handlers

        #region Protected Methods
        protected virtual void LoadCompanyList()
        {
            this.lstCompany.DataSource = (new CompanyProvider()).Load(ApplicationContext.CompId);
            this.lstCompany.DisplayColumns.Clear();
            this.lstCompany.DisplayColumns.AddRange(new string[] { CompanyBase.Columns.CompanyId.ToString(), CompanyBase.Columns.Name.ToString() });
            this.lstCompany.ShowColumns();
            this.lstCompany.CaptionControl.Text = "&Company List";
            this.lstCompany.SelectAll();
        }

        protected virtual void ResetControl()
        {
            this.chkAllowCreate.Checked = true;
            this.chkAllowDelete.Checked = true;
            this.chkAllowEdit.Checked = true;
            this.chkAllowRead.Checked = true;

            this.FunctionList.Clear();
        }

        protected virtual void LoadFunctionList()
        {
            SQLSortBuilder builder = new SQLSortBuilder();
            ApiFunctionHeaderProvider provider = new ApiFunctionHeaderProvider();
            builder.Append(ApiFunctionHeaderBase.Columns.AppId);
            builder.Append(ApiFunctionHeaderBase.Columns.Name);

            this.bindMain.RaiseListChangedEvents = false;
            this.bindMain.DataSource = provider.Load(this.CompId, new FilterCriteria("", builder.ToString()));
            this.bindMain.RaiseListChangedEvents = true;
            this.bindMain.ResetBindings(true);
        }

        protected TravNavItem CreateSelectButton(bool selectAll)
        {
            TravNavItem item = new TravNavItem();
            item.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            item.Text = selectAll ? Localizer.GetLocalizedString("btnSelectAll") : Localizer.GetLocalizedString("btnUnselectAll");
            item.Tag = selectAll;
            item.TravNavItemClick += ButtonSelectClick;

            return item;
        }

        protected virtual void ManageSelection(bool selectAll)
        {
            //Use this method so that we only process the records that are currently available in the view and not all the items in the binding list
            for (int i = 0; i < this.gvFunctionList.RowCount; i++)
            {
                var function = this.gvFunctionList.GetRow(i) as ApiFunctionHeader;
                if (selectAll && !this.FunctionList.Exists(f => f.Id == function.Id))
                {
                    this.FunctionList.Add(function);
                }
                else if (!selectAll && this.FunctionList.Exists(f => f.Id == function.Id))
                {
                    this.FunctionList.Remove(function);
                }
            }
            this.dgvFunctionList.RefreshDataSource();
        }
        #endregion Protected Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                this.LoadCompanyList();
                this.ResetControl();
                this.LoadFunctionList();
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        public override void Execute()
        {
            try
            {
                if (this.CurrentUser == null)
                    return;

                this.Validate();
                this.InitStatus();
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;

                Process = ProcessEngine;
                ProcessEngine.UserInfo = this.CurrentUser;
                ProcessEngine.AllowCreate = this.chkAllowCreate.Checked;
                ProcessEngine.AllowDelete = this.chkAllowDelete.Checked;
                ProcessEngine.AllowEdit = this.chkAllowEdit.Checked;
                ProcessEngine.AllowRead = this.chkAllowRead.Checked;

                ProcessEngine.ExpirationDate = this.dtAccessExpire.EditValue == null ? null : new DateTime?((DateTime)this.dtAccessExpire.EditValue);
                ProcessEngine.FunctionList.AddRange(this.FunctionList);

                foreach (Company company in this.lstCompany.SelectedList)
                    ProcessEngine.SelectedCompanyList.Add(company.CompanyId);

                base.ExecuteAsync(UpdateStatusText);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
            finally
            {
                if (!IsProcessBusy)
                {
                    this.EndStatus();
                    this.Cursor = System.Windows.Forms.Cursors.Default;
                }
            }
        }

        protected override void OnProcessCompleted()
        {
            try
            {
                if (ProcessError != null)
                    throw ProcessError;

                this.CurrentUser.FunctionList.ResetBindings();

                this.ParentForm.Close();
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
            finally
            {
                this.EndStatus();
                this.Cursor = System.Windows.Forms.Cursors.Default;
            }
        }

        protected override string CompId { get => this.CurrentUser?.CompId ?? base.CompId; set => base.CompId = value; }
        #endregion Overrides

        #region Properties
        public List<ApiFunctionHeader> FunctionList { get; } = new List<ApiFunctionHeader>();

        public ApiUser CurrentUser { get; set; }

        protected virtual TravNavItem ButtonSelectAll { get => _btnSelectAll ?? (_btnSelectAll = CreateSelectButton(true)); }

        protected virtual TravNavItem ButtonSelectNone { get => _btnSelectNone ?? (_btnSelectNone = CreateSelectButton(false)); }

        protected virtual ApiCreateMultiFunction ProcessEngine
        {
            get
            {
                if (_processEngine == null)
                    _processEngine = ProcessBase.LoadProcessEngine<ApiCreateMultiFunction>(CompId);

                return _processEngine;
            }
        }
        #endregion Properties
    }
}
