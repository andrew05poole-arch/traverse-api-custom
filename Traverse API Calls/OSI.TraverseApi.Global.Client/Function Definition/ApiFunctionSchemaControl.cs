#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.ComponentModel;
using System.Windows.Forms;
using TRAVERSE.Business;
using TRAVERSE.Client;
#endregion Using Directives

namespace OSI.TraverseApi.Client
{
    public partial class ApiFunctionSchemaControl : PluginControlBase
    {
        #region Constructors
        public ApiFunctionSchemaControl()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region Event Handlers
        private void gvSchema_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            try
            {
                OngvSchemaCustomUnboundColumnData(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void gvSchema_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            try
            {
                OngvSchema_CustomColumnDisplayText(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void txtValueTranslation_CustomDisplayText(object sender, DevExpress.XtraEditors.Controls.CustomDisplayTextEventArgs e)
        {
            try
            {
                OntxtValueTranslation_CustomDisplayText(e);
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

        private void CurrentSchemaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                OnCurrentSchemaPropertyChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void TranslationButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            try
            {
                OnTranslationButtonClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OngvSchemaCustomUnboundColumnData(DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column == colValueTranslation)
            {
                if (e.IsGetData)
                {
                    var schema = this.gvSchema.GetRow(e.ListSourceRowIndex) as ApiFunctionSchema;
                    if (schema != null && schema.ValueList.Count > 0)
                        e.Value = "<<Assigned>>";
                    else
                        e.Value = string.Empty;
                }
            }
        }

        protected virtual void OngvSchema_CustomColumnDisplayText(DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            var schema = this.gvSchema.GetRow(e.ListSourceRowIndex) as ApiFunctionSchema;

            if (e.Column == colValueTranslation)
            {
                if (schema != null && schema.ValueList.Count > 0)
                    e.DisplayText = "<<Assigned>>";
                else
                    e.DisplayText = string.Empty;
            }
        }

        protected virtual void OnBindingSourceAddingNew(AddingNewEventArgs e)
        {
            e.NewObject = CreateNew();
        }

        protected virtual void OnBindingSourceCurrentChanged(EventArgs e)
        {
            CurrentSchema = BindingSource.Current as ApiFunctionSchema;
        }

        protected void OnCurrentSchemaPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ApiFunctionSchemaBase.Columns.TravFieldName.ToString())
            {
                this.CurrentSchema.QueryColumnName = this.CurrentSchema.TravFieldName;
            }
        }

        protected virtual void OnTranslationButtonClick(DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            using (ApiFunctionSchemaTranslateForm form = new ApiFunctionSchemaTranslateForm())
            {
                form.BindingSource.RaiseListChangedEvents = false;
                form.BindingSource.DataSource = this.CurrentSchema.ValueList;
                form.BindingSource.RaiseListChangedEvents = true;
                form.BindingSource.ResetBindings(true);

                form.ShowDialog(this);
            }
        }

        protected virtual void OntxtValueTranslation_CustomDisplayText(DevExpress.XtraEditors.Controls.CustomDisplayTextEventArgs e)
        {
            if (this.CurrentSchema != null && this.CurrentSchema.ValueList.Count > 0)
                e.DisplayText = "<<Assigned>>";
            else
                e.DisplayText = string.Empty;
        }
        #endregion Event Handlers

        #region Methods
        protected virtual ApiFunctionSchema CreateNew()
        {
            return new ApiFunctionSchema(CompId);
        }

        public virtual void LoadChildFunctionLookup(string compId)
        {
            var lookup = Lookup.CreateLookupDef("ApiFunction");
            this.lkpFunction.LoadList(CompId, lookup, null, null);
            this.lkpFunction.DisplayMember = "Name";
            this.lkpFunction.ValueMember = "ID";
        }
        #endregion Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.cboCheckOptions.SetFlags(typeof(ApiFieldSetting));
        }

        protected override void UpdateData(int index)
        { }
        #endregion Overrides

        #region Properties
        protected virtual ApiFunctionSchema CurrentSchema
        {
            get => _currentSchema;
            set
            {
                if (_currentSchema != null)
                    _currentSchema.PropertyChanged -= CurrentSchemaPropertyChanged;

                _currentSchema = value;

                if (_currentSchema != null)
                    _currentSchema.PropertyChanged += CurrentSchemaPropertyChanged;
            }
        }
        #endregion Properties

        #region Fields
        private ApiFunctionSchema _currentSchema;
        #endregion Fields
    }
}
