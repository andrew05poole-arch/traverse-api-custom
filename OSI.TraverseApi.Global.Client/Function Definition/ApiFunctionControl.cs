#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.ComponentModel;
using TRAVERSE.Business;
using TRAVERSE.Client;
using TRAVERSE.Controls;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Client
{
    public partial class ApiFunctionControl : PluginControlBase
    {
        #region Constructors
        public ApiFunctionControl()
            : this(null)
        { }

        public ApiFunctionControl(IPlugin host)
            : base(host)
        {
            InitializeComponent();
            if (!DevEnvironment.InVSDesigner)
                this.HostPlugin = host;

            SetSchemaControl();
            this.chkAllowNew.Properties.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            this.chkAllowRead.Properties.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            this.chkDelete.Properties.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            this.chkEdit.Properties.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            this.txtID.ReadOnly = true;
        }
        #endregion Constructors

        #region Event Handlers
        private void lkpName_LookupValueSelected(object sender, LookupValueSelectedEventArgs e)
        {
            try
            {
                this.OnlkpName_LookupValueSelected(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void BindingSourceAddingNew(object sender, AddingNewEventArgs e)
        {
            try
            {
                this.OnBindingSourceAddingNew(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void BindingSourceCurrentChanged(object sender, EventArgs e)
        {
            try
            {
                this.OnBindingSourceCurrentChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void CurrentFunctionHeaderChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                OnCurrentFunctionHeaderChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnServerUpdate_TravNavItemClick(object sender, TravNavigatorEventArgs e)
        {
            try
            {
                this.OnButtonServerUpdateClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OnlkpName_LookupValueSelected(LookupValueSelectedEventArgs e)
        {
            if (e.NotInList)
            {
                if (this.CurrentFunction == null || (!this.CurrentFunction.IsNew && string.Compare(this.CurrentFunction.Name.ToString(), e.SelectedValue, true) != 0))
                    this.BindingSource.AddNew();

                this.CurrentFunction.Name = e.SelectedValue;
                this.txtAppId.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(e.SelectedValue))
            {
                this.LoadDataByName(e.SelectedValue);
                return;
            }

            this.LoadDataByName(e.SelectedText);
        }

        protected virtual void OnBindingSourceAddingNew(AddingNewEventArgs e)
        {
            e.NewObject = this.CreateNew();
        }

        protected virtual void OnBindingSourceCurrentChanged(EventArgs e)
        {
            this.CurrentFunction = BindingSource.Current as ApiFunctionHeader;
            SchemaControl.BindingSource.RaiseListChangedEvents = false;
            SchemaControl.BindingSource.DataSource = CurrentFunction?.SchemaList;
            SchemaControl.BindingSource.RaiseListChangedEvents = true;
            SchemaControl.BindingSource.ResetBindings(true);
            EnableControls(true);
        }

        protected virtual void OnCurrentFunctionHeaderChanged(PropertyChangedEventArgs e)
        {
        }

        protected virtual void OnButtonServerUpdateClick(TravNavigatorEventArgs e)
        {
            if (this.CurrentFunction != null)
            {
                if (this.CurrentFunction.IsDirty)
                    this.SaveCurrentItem();

                if (this.CurrentFunction.IsDirty)
                    return;
            }

            this.ShowScriptBuilder();
        }
        #endregion Event Handlers

        #region Methods
        protected virtual void SetSchemaControl()
        {
            SchemaControl.Dock = System.Windows.Forms.DockStyle.Fill;
            if (!DevEnvironment.InVSDesigner)
                SchemaControl.HostPlugin = HostPlugin;
            splitFunction.Panel2.Controls.Add(SchemaControl);
        }

        protected virtual void LoadFunctionLookup()
        {
            var lookup = Lookup.CreateLookupDef("ApiFunction");
            this.lkpName.LoadList(CompId, lookup, null, null);
            this.lkpName.DisplayMember = "Name";
            this.lkpName.ValueMember = "Name";
        }

        protected virtual void LoadOverrideLookup()
        {
            var lookup = Lookup.CreateLookupDef("ApiFunction");
            this.lkpOverride.LoadList(CompId, lookup, null, null);
            this.lkpOverride.DisplayMember = "Name";
            this.lkpOverride.ValueMember = "ID";
        }

        protected virtual void SetButtons()
        {
            this.btnServerUpdate.Visible = this.HasPermission(5240101);
        }

        protected virtual void CreateNewFunction()
        {
            ApiFunctionHeader function = CreateNew();
            if (function == null)
                return;

            ProviderItem.Items.Clear();
            ProviderItem.Items.Add(function);
            BindingSource.DataSource = ProviderItem.Items;
            BindingSource.ResetBindings(false);
            txtAppId.Focus();
            OnUpdateTravNavigator();
            EnableControls(true);
        }

        protected virtual ApiFunctionHeader CreateNew()
        {
            return new ApiFunctionHeader(CompId) { Id = Guid.NewGuid() };
        }

        protected virtual void LoadDataByName(string name)
        {
            this.Provider = this.ProviderItem;

            SqlFilterBuilder<ApiFunctionHeaderBase.Columns> builder = new SqlFilterBuilder<ApiFunctionHeaderBase.Columns>();
            builder.AppendEquals(ApiFunctionHeaderBase.Columns.Name, name);

            this.BindingSource.RaiseListChangedEvents = false;
            this.BindingSource.DataSource = this.ProviderItem.Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
            this.BindingSource.RaiseListChangedEvents = true;
            this.BindingSource.ResetBindings(true);
            this.OnUpdateTravNavigator();
        }

        protected virtual void ShowScriptBuilder()
        {
            LoadFunctionEventArgs args = new LoadFunctionEventArgs(5240101);
            args.Parameters.Add("CompanyId", CompId);
            args.FunctionCaption = "Generate scripts";

            this.LoadFunction(this, args);
        }

        protected virtual void EnableControls(bool enable)
        {
            lkpName.ReadOnly = !enable && string.IsNullOrEmpty(ApiUtility.CurrentApiDb);
            navMain.Enabled = !string.IsNullOrEmpty(ApiUtility.CurrentApiDb);
            txtAppId.ReadOnly = !enable;
            cboType.ReadOnly = !enable;
            chkAllowRead.ReadOnly = !enable;
            chkEdit.ReadOnly = !enable;
            chkAllowNew.ReadOnly = !enable;
            chkDelete.ReadOnly = !enable;
            txtNotes.ReadOnly = !enable;
            txtTableName.ReadOnly = !enable;
            lkpOverride.ReadOnly = !enable;
            SchemaControl.Enabled = enable;
        }
        #endregion Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                if (!DevEnvironment.InVSDesigner)
                {
                    ApiDbSelectForm.SelectCompany();
                    if (string.IsNullOrEmpty(ApiUtility.CurrentApiDb))
                        throw new InvalidValueException("No Api database could be found");

                    this.CompId = ApiUtility.CurrentApiDb;
                    if (HostPlugin != null)
                        HostPlugin.DatabaseName = this.CompId;
                    base.OnLoad(e);
                    this.SetButtons();
                    this.LoadFunctionLookup();
                    this.LoadOverrideLookup();
                    if (SchemaControl != null)
                        SchemaControl.LoadChildFunctionLookup(CompId);
                    this.LoadData();
                }
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
            finally
            {
                EnableControls(BindingSource.Count > 0);
            }
        }

        protected override void LoadData()
        {
            this.Provider = this.ProviderItem;
            this.BindingSource.RaiseListChangedEvents = false;
            this.BindingSource.DataSource = this.ProviderItem.Items;
            this.BindingSource.RaiseListChangedEvents = true;
            this.BindingSource.ResetBindings(true);
        }

        protected override void UpdateData(int index)
        {
            try
            {
                if (index < 0)
                {
                    if (this.ProviderItem.Items.IsDeletedCount > 0)
                    {
                        this.ProviderItem.Update(CompId);
                    }
                }
                else
                {
                    if (this.ProviderItem[index].IsDirty)
                    {
                        this.ProviderItem[index].Validate();
                        if (!this.ProviderItem[index].IsValid)
                            TravMessageBox.Show("", this.ProviderItem[index].Error, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                        else
                            this.ProviderItem.Update(CompId);
                    }
                    this.ProviderItem[index].SetErrorText(null);
                }
            }
            catch (Exception ex)
            {
                if (ex is ProviderException && index > -1)
                {
                    this.ProviderItem[index].SetErrorText(ex.Message);
                }
                ClientContext.HandleError(ex, this);
            }
            finally
            {
                if (this.ProviderItem.Items.IsDeletedCount > 0)
                {
                    this.ProviderItem.Items.AddRange(this.ProviderItem.Items.DeletedItems);
                    this.ProviderItem.Items.DeletedItems.Clear();
                    this.BindingSource.ResetBindings(false);
                }
                EntityProvider.InvalidateLookupObject("ApiFunction");
                this.OnUpdateTravNavigator();
            }
        }

        protected override void RefreshData()
        {
            if (this.CurrentFunction == null)
                return;

            if (this.CurrentFunction.IsDirty)
            {
                System.Windows.Forms.DialogResult dialogResult = TravMessageBox.Show(string.Empty, MessageLocalizer.GetLocalizedString("SaveAndRefresh"),
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Question, System.Windows.Forms.MessageBoxDefaultButton.Button1);

                if (dialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

                if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                {
                    this.SaveCurrentItem();
                    if (this.CurrentFunction.IsDirty)
                        return;
                }
            }

            this.LoadDataByName(this.CurrentFunction.Name);
        }

        protected override void InitExtension()
        {
            Extension.Root = layoutMain;
            Extension.LayoutRoot = layoutMain;
            Extension.Navigator = navMain;
            this.EntityName = string.Empty;
        }

        public override object DesignRoot => layoutMain;
        #endregion Overrides

        #region Properties
        protected virtual ApiFunctionHeader CurrentFunction
        {
            get => _currentFunction;
            set
            {
                if (_currentFunction != null)
                    _currentFunction.PropertyChanged -= CurrentFunctionHeaderChanged;

                _currentFunction = value;

                if (_currentFunction != null)
                    _currentFunction.PropertyChanged += CurrentFunctionHeaderChanged;
            }
        }

        protected virtual ApiFunctionSchemaControl SchemaControl
        {
            get
            {
                if (_schemaControl == null)
                    _schemaControl = new ApiFunctionSchemaControl();
                return _schemaControl;
            }
        }

        protected virtual ApiFunctionHeaderProvider ProviderItem { get; } = new ApiFunctionHeaderProvider();
        #endregion Properties

        #region Fields
        private ApiFunctionSchemaControl _schemaControl;
        private ApiFunctionHeader _currentFunction;
        #endregion Fields
    }
}
