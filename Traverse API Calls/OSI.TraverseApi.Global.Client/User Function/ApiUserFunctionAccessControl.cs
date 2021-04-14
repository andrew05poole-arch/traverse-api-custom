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
    public partial class ApiUserFunctionAccessControl : PluginControlBase
    {
        #region Constructors
        public ApiUserFunctionAccessControl()
            : base()
        {
            InitializeComponent();
        }

        public ApiUserFunctionAccessControl(ApiUserFunctionControl parent)
            : this()
        {
            ParentControl = parent;
        }
        #endregion Constructors

        #region Event Handlers
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
                OnBindingSourceCurrentChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void gvUserFunction_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            try
            {
                OngvUserFunctionCellValueChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void CurrentFunctionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                OnCurrentFunctionPropertyChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void navMain_Save(object sender, TravNavigatorEventArgs e)
        {
            try
            {
                OnNavigatorSave(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnMultiFunction_Click(object sender, EventArgs e)
        {
            try
            {
                OnbtnMultiFunction_Click(e);
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
            CurrentFunction = BindingSource.Current as ApiUserFunction;
        }

        protected virtual void OngvUserFunctionCellValueChanged(DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            this.dgvUserFunction.FindRowByKey(colFunctionId, e);
        }

        protected virtual void OnCurrentFunctionPropertyChanged(PropertyChangedEventArgs e)
        {
        }

        protected virtual void OnNavigatorSave(TravNavigatorEventArgs e)
        {
            if (ParentControl != null)
            {
                ParentControl.SaveCurrentItem();
                e.Handled = true;
            }
        }

        protected virtual void OnbtnMultiFunction_Click(EventArgs e)
        {
            this.ShowMultiSelectForm();
        }
        #endregion Event Handlers

        #region Protected Methods
        protected virtual ApiUserFunction CreateNew()
        {
            return new ApiUserFunction() { Parent = this.CurrentUser };
        }

        protected virtual void LoadFunctionIdList()
        {
            var lookup = Lookup.CreateLookupDef("ApiFunction");
            this.lkpFunctionId.LoadList(CompId, lookup, null, null);
            this.lkpFunctionId.DisplayMember = "Name";
            this.lkpFunctionId.ValueMember = "ID";
            this.lkpFunctionId.LimitToList = true;
        }

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

        protected virtual void ShowMultiSelectForm()
        {
            using (ApiUserMultiFunctionForm form = new ApiUserMultiFunctionForm(this.CurrentUser))
            {
                this.ParentControl.CompanyControl.BindingSource.RaiseListChangedEvents = false;
                form.ShowDialog(this);
                this.ParentControl.CompanyControl.BindingSource.RaiseListChangedEvents = true;
            }
        }
        #endregion Protected Methods

        #region Public Methods
        public void ResetNavigator()
        {
            OnUpdateTravNavigator();
        }
        #endregion Public Methods

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

                    base.OnLoad(e);
                    LoadFunctionIdList();
                }
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
        protected virtual ApiUserFunction CurrentFunction
        {
            get => _currentFunction;
            set
            {
                if (_currentFunction != null)
                    _currentFunction.PropertyChanged -= CurrentFunctionPropertyChanged;
                _currentFunction = value;
                if (_currentFunction != null)
                    _currentFunction.PropertyChanged += CurrentFunctionPropertyChanged;
            }
        }

        protected virtual ApiUser CurrentUser { get; set; }

        protected ApiUserFunctionControl ParentControl { get; set; }
        #endregion Properties

        #region Fields
        private ApiUserFunction _currentFunction;
        #endregion Fields
    }
}
