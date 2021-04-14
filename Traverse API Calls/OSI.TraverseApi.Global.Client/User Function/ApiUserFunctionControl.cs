using OSI.TraverseApi.Business;
using System;
using TRAVERSE.Business;
using TRAVERSE.Client;
using TRAVERSE.Core;

namespace OSI.TraverseApi.Client
{
    public partial class ApiUserFunctionControl : PluginControlBase
    {
        #region Constructors
        public ApiUserFunctionControl()
            : this(null)
        { }

        public ApiUserFunctionControl(IPlugin host)
            : base(host)
        {
            InitializeComponent();
            if (!DevEnvironment.InVSDesigner)
                base.HostPlugin = host;

            SetAccessControl();
            SetCompanyControl();
            SetSchemaControl();
        }
        #endregion Constructors

        #region Event Handlers
        private void AccessBindingSourceChanged(object sender, EventArgs e)
        {
            try
            {
                this.OnAccessBindingSourceChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OnAccessBindingSourceChanged(EventArgs e)
        {
            var function = AccessControl.BindingSource.Current as ApiUserFunction;
            CompanyControl.BindingSource.RaiseListChangedEvents = false;
            SchemaControl.BindingSource.RaiseListChangedEvents = false;

            CompanyControl.ParentFunction = function;
            SchemaControl.ParentFunction = function;
            CompanyControl.BindingSource.DataSource = function?.CompanyList;
            SchemaControl.BindingSource.DataSource = function?.SchemaList;

            CompanyControl.BindingSource.RaiseListChangedEvents = true;
            SchemaControl.BindingSource.RaiseListChangedEvents = true;

            CompanyControl.BindingSource.ResetBindings(true);
            SchemaControl.BindingSource.ResetBindings(true);
        }
        #endregion Event Handlers

        #region Methods
        protected virtual int FunctionSort(ApiUserFunction function1, ApiUserFunction function2)
        {
            if (function1 == null && function2 == null)
                return 0;
            if (function1 == null)
                return 1;
            if (function2 == null)
                return -1;

            if (function1.FunctionName == function2.FunctionName)
                return 0;
            return string.Compare(function1.FunctionName, function2.FunctionName);
        }

        protected virtual void SetAccessControl()
        {
            AccessControl.Dock = System.Windows.Forms.DockStyle.Fill;
            if (!DevEnvironment.InVSDesigner)
                AccessControl.HostPlugin = HostPlugin;
            splitMain.Panel1.Controls.Add(AccessControl);
            AccessControl.BindingSource.CurrentChanged -= AccessBindingSourceChanged;
            AccessControl.BindingSource.CurrentChanged += AccessBindingSourceChanged;
        }

        protected virtual void SetCompanyControl()
        {
            CompanyControl.Dock = System.Windows.Forms.DockStyle.Fill;
            if (!DevEnvironment.InVSDesigner)
                CompanyControl.HostPlugin = HostPlugin;
            tabCompany.Controls.Add(CompanyControl);
        }

        protected virtual void SetSchemaControl()
        {
            SchemaControl.Dock = System.Windows.Forms.DockStyle.Fill;
            if (!DevEnvironment.InVSDesigner)
                SchemaControl.HostPlugin = HostPlugin;
            tabSchema.Controls.Add(SchemaControl);
        }
        #endregion Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                if (!DevEnvironment.InVSDesigner)
                {
                    if (this.HostPlugin.Parameters == null || this.HostPlugin.Parameters.Count == 0)
                        return;

                    CompId = Convert.ToString(this.HostPlugin.Parameters["CompanyId"]);
                    CurrentUser = this.HostPlugin.Parameters["User"] as ApiUser;

                    if (CurrentUser != null)
                        ProviderItem.Items.Add(CurrentUser);

                    base.OnLoad(e);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected override void LoadData()
        {
            this.Provider = this.ProviderItem;
            CurrentUser.FunctionList.Sort(FunctionSort);
            BindingSource.RaiseListChangedEvents = false;
            AccessControl.BindingSource.RaiseListChangedEvents = false;

            BindingSource.DataSource = this.ProviderItem.Items;
            AccessControl.BindingSource.DataSource = CurrentUser.FunctionList;

            BindingSource.RaiseListChangedEvents = true;
            AccessControl.BindingSource.RaiseListChangedEvents = true;

            BindingSource.ResetBindings(true);
            AccessControl.BindingSource.ResetBindings(true);
        }

        protected override void UpdateData(int index)
        {
            try
            {
                if (this.CurrentUser.IsDirty)
                {
                    this.CurrentUser.ValidateAll(true);
                    if (!this.CurrentUser.IsValid)
                        TravMessageBox.Show("", this.CurrentUser.Error, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                    else
                    {
                        this.bindUser.RaiseListChangedEvents = false;
                        this.CurrentUser.SuppressEntityEvents = true;
                        this.ProviderItem.Update(CompId);
                    }
                }
                this.CurrentUser.SetErrorText(null);
            }
            catch (Exception ex)
            {
                if (ex is ProviderException && index > -1)
                {
                    this.CurrentUser.FunctionList[index].SetErrorText(ex.Message);
                }
                ClientContext.HandleError(ex, this);
            }
            finally
            {
                this.bindUser.RaiseListChangedEvents = true;
                this.CurrentUser.SuppressEntityEvents = false;
                AccessControl.ResetNavigator();
            }
        }

        public override bool CanClose()
        {
            this.Validate();
            return base.CanClose() && (this.CurrentUser == null || !this.CurrentUser.IsDirty) && this.AccessControl.CanClose() && this.CompanyControl.CanClose() && this.SchemaControl.CanClose();
        }
        #endregion Overrides

        #region Properties
        protected virtual ApiUser CurrentUser { get; set; }

        protected virtual ApiUserProvider ProviderItem { get; } = new ApiUserProvider();

        public virtual ApiUserFunctionAccessControl AccessControl
        {
            get
            {
                if (_accessControl == null)
                    _accessControl = new ApiUserFunctionAccessControl(this);
                return _accessControl;
            }
        }

        public virtual ApiUserFunctionCompanyControl CompanyControl
        {
            get
            {
                if (_companyControl == null)
                    _companyControl = new ApiUserFunctionCompanyControl();
                return _companyControl;
            }
        }

        public virtual ApiUserFunctionSchemaControl SchemaControl
        {
            get
            {
                if (_schemaControl == null)
                    _schemaControl = new ApiUserFunctionSchemaControl();
                return _schemaControl;
            }
        }
        #endregion Properties

        #region Fields
        private ApiUserFunctionAccessControl _accessControl;
        private ApiUserFunctionCompanyControl _companyControl;
        private ApiUserFunctionSchemaControl _schemaControl;
        #endregion Fields
    }
}
