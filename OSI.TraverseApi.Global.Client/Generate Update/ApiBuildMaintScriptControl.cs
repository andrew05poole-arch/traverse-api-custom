using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using TRAVERSE.Business;
using TRAVERSE.Client;
using TRAVERSE.Controls;
using TRAVERSE.Core;

namespace OSI.TraverseApi.Client
{
    public partial class ApiBuildMaintScriptControl : ProcessControlBase
    {
        #region Constructors
        public ApiBuildMaintScriptControl()
            : this(null)
        { }

        public ApiBuildMaintScriptControl(IPlugin host)
            : base()
        {
            InitializeComponent();
            if (!DevEnvironment.InVSDesigner)
                this.HostPlugin = host;
        }
        #endregion Constructors

        #region Event Handlers
        private void gvMain_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            try
            {
                this.OngvMain_CustomUnboundColumnData(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void ButtonSelectAllOrNoneClick(object sender, TravNavigatorEventArgs e)
        {
            try
            {
                this.OnButtonSelectAllOrNoneClick((bool)((TravNavItem)sender).Tag, e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OngvMain_CustomUnboundColumnData(DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column == colSelect)
            {
                ApiFunctionHeader function = e.Row as ApiFunctionHeader;
                if (function == null)
                    return;

                if (e.IsGetData)
                {
                    e.Value = SelectFunctionList.Contains(function);
                }
                if (e.IsSetData)
                {
                    if ((bool)e.Value && !SelectFunctionList.Contains(function))
                        SelectFunctionList.Add(function);

                    if (!(bool)e.Value && SelectFunctionList.Contains(function))
                        SelectFunctionList.Remove(function);
                }
            }
        }

        protected virtual void OnButtonSelectAllOrNoneClick(bool selectAll, TravNavigatorEventArgs e)
        {
            this.gvMain.PostEditor();
            this.Validate();

            this.SelectFunctionList.Clear();
            if (selectAll)
            {
                for (int i = 0; i < gvMain.RowCount; i++)
                {
                    SelectFunctionList.Add(gvMain.GetRow(i) as ApiFunctionHeader);
                }
            }
            dgvMain.RefreshDataSource();
        }
        #endregion Event Handlers

        #region Methods
        protected virtual void LoadOverrideLookup()
        {
            var lookup = Lookup.CreateLookupDef("ApiFunction");
            this.lkpOverride.LoadList(CompId, lookup, null, null);
            this.lkpOverride.DisplayMember = "Name";
            this.lkpOverride.ValueMember = "ID";
        }

        protected virtual void SetButtons()
        {
            var strip = this.ButtonReset.GetCurrentParent();
            if (strip == null)
                return;

            strip.Items.Add(ButtonSelectAll);
            strip.Items.Add(ButtonSelectNone);
            this.ButtonActivity.Enabled = false;
        }

        protected virtual TravNavItem LoadSelectAllButton()
        {
            if (_btnSelectAll == null)
            {
                _btnSelectAll = new TravNavItem();
                _btnSelectAll.Text = Localizer.GetLocalizedString("btnSelectAll");
                _btnSelectAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
                _btnSelectAll.Tag = true;
                _btnSelectAll.TravNavItemClick += ButtonSelectAllOrNoneClick;
            }
            return _btnSelectAll;
        }

        protected virtual TravNavItem LoadSelectNoneButton()
        {
            if (_btnSelectNone == null)
            {
                _btnSelectNone = new TravNavItem();
                _btnSelectNone.Text = Localizer.GetLocalizedString("btnUnselectAll");
                _btnSelectNone.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
                _btnSelectNone.Tag = false;
                _btnSelectNone.TravNavItemClick += ButtonSelectAllOrNoneClick;
            }
            return _btnSelectNone;
        }

        protected virtual void EnableButtons(bool enable)
        {
            this.ButtonExecute.Enabled = enable;
            this.ButtonReset.Enabled = enable;
            this.ButtonSelectAll.Enabled = enable;
            this.ButtonSelectNone.Enabled = enable;
        }
        #endregion Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                if (Parameters != null && Parameters.ContainsKey("CompanyId"))
                    CompId = Parameters["CompanyId"] as string;
                else
                {
                    ApiDbSelectForm.SelectCompany();
                    this.CompId = ApiUtility.CurrentApiDb;
                }
                this.SetButtons();

                base.OnLoad(e);
                this.LoadOverrideLookup();
                this.LoadData();
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected override void LoadData()
        {
            ApiFunctionHeaderProvider provider = new ApiFunctionHeaderProvider();
            this.BindingSource.RaiseListChangedEvents = false;
            this.BindingSource.DataSource = provider.Load(CompId, new FilterCriteria("", ApiFunctionHeaderBase.Columns.Name.ToString()));
            this.BindingSource.RaiseListChangedEvents = true;
            this.BindingSource.ResetBindings(true);
        }

        protected override void UpdateData(int index)
        { }

        public override void Execute()
        {
            try
            {
                this.gvMain.PostEditor();
                this.Validate();

                if (this.SelectFunctionList.Count == 0)
                {
                    TravMessageBox.Show("", MessageLocalizer.GetLocalizedString("SelectFunction"));
                    return;
                }

                this.Process = this.ProcessEngine;
                if (string.IsNullOrEmpty(this.fbdScript.SelectedPath))
                    this.fbdScript.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (this.fbdScript.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                this.ProcessEngine.FilePath = this.fbdScript.SelectedPath;
                this.ProcessEngine.FunctionList.AddRange(this.SelectFunctionList);

                this.InitStatus();
                this.EnableButtons(false);
                this.ExecuteAsync(this.UpdateStatusText);
            }
            catch (Exception ex)
            {

                ClientContext.HandleError(ex, this);
            }
            finally
            {
                if (!this.IsProcessBusy)
                {
                    this.EndStatus();
                    this.EnableButtons(true);
                }
            }
        }

        public override void ResetParameters()
        {
            this.SelectFunctionList.Clear();
            dgvMain.RefreshDataSource();
        }

        protected override void OnProcessCompleted()
        {
            this.EndStatus();
            this.EnableButtons(true);
            this.ResetParameters();
            TravMessageBox.Show(string.Empty, Localizer.GetLocalizedString("ProcessComplete"),
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Asterisk);
            this.ParentForm.Close();
        }
        #endregion Overrides

        #region Properties
        protected virtual List<ApiFunctionHeader> SelectFunctionList { get; } = new List<ApiFunctionHeader>();

        protected virtual TravNavItem ButtonSelectAll
        {
            get
            {
                return this.LoadSelectAllButton();
            }
        }

        protected virtual TravNavItem ButtonSelectNone
        {
            get
            {
                return this.LoadSelectNoneButton();
            }
        }

        protected virtual ApiGenerateScriptProcess ProcessEngine
        {
            get
            {
                if (_processEngine == null)
                    _processEngine = ProcessBase.LoadProcessEngine<ApiGenerateScriptProcess>(CompId);

                return _processEngine;
            }
        }
        #endregion Properties

        #region Fields
        private TravNavItem _btnSelectAll;
        private TravNavItem _btnSelectNone;
        private ApiGenerateScriptProcess _processEngine;
        #endregion Fields
    }
}
