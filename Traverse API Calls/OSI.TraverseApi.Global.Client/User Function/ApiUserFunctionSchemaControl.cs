using OSI.TraverseApi.Business;
using System;
using System.ComponentModel;
using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public partial class ApiUserFunctionSchemaControl : PluginControlBase
    {
        #region Constructors
        public ApiUserFunctionSchemaControl()
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

        protected virtual void OnBindingSourceAddingNew(AddingNewEventArgs e)
        {
            e.NewObject = CreateNew();
        }
        #endregion Event Handlers

        #region Protected Methods
        protected virtual ApiUserFunctionSchema CreateNew()
        {
            return new ApiUserFunctionSchema() { Parent = ParentFunction };
        }

        protected virtual void LoadComboBox()
        {
            cboFunctionSchemaId.DataSource = ParentFunction.FunctionInfo.SchemaList;
            cboFunctionSchemaId.ShowColumns(new string[] { ApiFunctionSchemaBase.Columns.ApiFieldName.ToString() });
            cboFunctionSchemaId.ValueMember = ApiFunctionSchemaBase.Columns.Id.ToString();
            cboFunctionSchemaId.DisplayMember = ApiFunctionSchemaBase.Columns.ApiFieldName.ToString();
        }
        #endregion Protected Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                this.LoadComboBox();
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
        #endregion Properties
    }
}
