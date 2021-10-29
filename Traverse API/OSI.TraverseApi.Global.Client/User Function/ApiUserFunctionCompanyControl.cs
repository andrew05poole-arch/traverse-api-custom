using DevExpress.XtraGrid.Columns;
using OSI.TraverseApi.Business;
using System;
using System.ComponentModel;
using TRAVERSE.Client;
using TRAVERSE.Controls;

namespace OSI.TraverseApi.Client
{
    public partial class ApiUserFunctionCompanyControl : PluginControlBase
    {
        #region Constructors
        public ApiUserFunctionCompanyControl()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region Event Handlers
        private void BindingSourceAddingNew(object sender, AddingNewEventArgs e)
        {
            try
            {
                OnBindingSourceAddingNew(e);
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
                OnBindingSourceCurrentChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void CurrentCompanyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                OnCurrentCompanyPropertyChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void DisplayFilterClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            try
            {
                OnDisplayFilterClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OnBindingSourceAddingNew(AddingNewEventArgs e)
        {
            e.NewObject = CreateNew();
        }

        protected virtual void OnBindingSourceCurrentChanged(EventArgs e)
        {
            CurrentCompany = BindingSource.Current as ApiUserFunctionComp;
            EnableColumns();
        }

        protected virtual void OnCurrentCompanyPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ApiUserFunctionCompBase.Columns.CompanyId.ToString())
            {
                if (this.CurrentCompany.IsNew)
                    this.CurrentCompany.Scope = this.ParentFunction.FunctionInfo.Scope; //for new entries, replace scope (access) using the function definition
                else
                    this.CurrentCompany.Scope &= this.ParentFunction.FunctionInfo.Scope; //for existing entries, reset scope for what is supported by the function

                EnableColumns();
            }
        }

        protected virtual void OnDisplayFilterClick(DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (CurrentCompany == null)
                return;

            using (var form = this.BuildAccessFilterForm())
            {
                form.FilterControl.FilterString = this.CurrentCompany.DisplayFilter;
                if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    this.CurrentCompany.DisplayFilter = form.FilterControl.FilterString;

                    if (string.IsNullOrEmpty(form.FilterControl.SqlFilterString))
                        this.CurrentCompany.Filter = null;
                    else
                        this.CurrentCompany.Filter = form.FilterControl.SqlFilterString.Replace("N'", "'");
                }
            }
        }
        #endregion Event Handlers

        #region Protected Methods
        protected virtual void LoadCombobox()
        {
            SetupMaintControl("TRAVERSE.Business.Sys.dll", "TRAVERSE.Business.Sys.CompanyProvider", cboCompanyId, new string[] { "CompanyId", "Name" }, 0);
        }

        protected virtual ApiUserFunctionComp CreateNew()
        {
            return this.ParentFunction.CompanyList.AddNew();
        }

        protected virtual ApiUserFunctionFilterForm BuildAccessFilterForm()
        {
            ApiUserFunctionFilterForm form = new ApiUserFunctionFilterForm();
            form.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            form.Size = new System.Drawing.Size(600, 400);
            form.FilterControl.FilterColumns.Clear();
            form.FilterControl.FilterString = string.Empty;
            form.Text = string.Format("{0} {1}", this.ParentFunction.FunctionName, form.Text);

            if (this.CurrentCompany != null)
            {
                form.FilterControl.SetFilterColumnsCollection(this.GetFilterColumns());
                form.FilterControl.FilterString = this.CurrentCompany.DisplayFilter;
            }

            return form;
        }

        protected virtual DevExpress.XtraEditors.Filtering.FilterColumnCollection GetFilterColumns()
        {
            var collection = new DevExpress.XtraEditors.Filtering.FilterColumnCollection();
            foreach (var field in ParentFunction.FunctionInfo.SchemaList)
                collection.Add(
                    new DevExpress.XtraEditors.Filtering.UnboundFilterColumn(
                        field.ApiFieldName, field.TravFieldName, typeof(string), new RepositoryItemTravTextBox(), DevExpress.Data.Filtering.Helpers.FilterColumnClauseClass.String));

            collection.Sort();
            return collection;
        }

        protected virtual void EnableColumns()
        {
            bool enable = this.CurrentCompany != null && !string.IsNullOrEmpty(this.CurrentCompany.CompanyId);
            foreach (var column in this.gvCompany.Columns)
            {
                if (((GridColumn)column) == this.colCompanyId)
                    continue;

                ((GridColumn)column).OptionsColumn.AllowEdit = enable;
            }
        }
        #endregion Protected Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                LoadCombobox();
                EnableColumns();
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected override void UpdateData(int index)
        { }
        #endregion Overrides

        #region Properties
        public virtual ApiUserFunction ParentFunction { get; set; }

        protected virtual ApiUserFunctionComp CurrentCompany
        {
            get => _currentCompany;
            set
            {
                if (_currentCompany != null)
                    _currentCompany.PropertyChanged -= CurrentCompanyPropertyChanged;

                _currentCompany = value;

                if (_currentCompany != null)
                    _currentCompany.PropertyChanged += CurrentCompanyPropertyChanged;
            }
        }
        #endregion Properties

        #region Fields
        private ApiUserFunctionComp _currentCompany;
        #endregion Fields
    }
}
