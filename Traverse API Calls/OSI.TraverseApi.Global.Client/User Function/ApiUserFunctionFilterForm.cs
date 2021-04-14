using System;
using TRAVERSE.Client;
using TRAVERSE.Controls;

namespace OSI.TraverseApi.Client
{
    public partial class ApiUserFunctionFilterForm : HostForm
    {
        #region Constructors
        public ApiUserFunctionFilterForm()
        {
            InitializeComponent();
            btnOk.Click += btnOk_Click;
        }
        #endregion Constructors

        #region Event Handlers
        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                this.OnbtnOk_Click(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }
        #endregion Event Handlers

        #region Protected Methods
        protected virtual void OnbtnOk_Click(EventArgs e)
        {
            this.Validate();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }
        #endregion Protected Methods

        #region Properties
        public TravFilterControl FilterControl
        {
            get
            {
                return this.dfpFilter;
            }
        }
        #endregion Properties
    }
}
