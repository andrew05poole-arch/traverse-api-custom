#region Using Directives
using OSI.TraverseApi.Business;
using System;
using TRAVERSE.Business;
using TRAVERSE.Client;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Client
{
    public partial class ApiCreateDbForm : HostForm
    {
        #region Fields
        private ApiCreateDb _processEngine;
        #endregion Fields

        #region Constructors
        public ApiCreateDbForm()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region Events
        private void chkUseTrusted_CheckStateChanged(object sender, EventArgs e)
        {
            try
            {
                this.OnchkUseTrustedCheckStateChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }
        protected virtual void OnchkUseTrustedCheckStateChanged(EventArgs e)
        {
            bool check = (this.chkUseTrusted.CheckState == System.Windows.Forms.CheckState.Checked);
            this.txtUsername.ReadOnly = check;
            this.txtPassword.ReadOnly = check;
        }

        private void btnProceed_Click(object sender, EventArgs e)
        {
            try
            {
                this.OnbtnProceedClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }
        protected virtual void OnbtnProceedClick(EventArgs e)
        {
            try
            {
                this.Validate();
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
                ProcessEngine.DatabaseName = this.txtDatabaseName.Text;
                ProcessEngine.Username = this.txtUsername.Text;
                ProcessEngine.Password = this.txtPassword.Text;
                ProcessEngine.UseTrusted = this.chkUseTrusted.Checked;

                ProcessEngine.Execute(null);

                TravMessageBox.Show(string.Empty, 
                    "Api database has been installed. Please run the API maintenance update again to complete the installation process",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Asterisk);
                this.Close();
            }
            finally
            {
                this.Cursor = System.Windows.Forms.Cursors.Default;
            }
        }
        #endregion Events

        #region Properties
        protected virtual ApiCreateDb ProcessEngine
        {
            get
            {
                if (_processEngine == null)
                    _processEngine = ProcessBase.LoadProcessEngine<ApiCreateDb>(ApplicationContext.CompId);
                return _processEngine;
            }
        }
        #endregion Properties
    }
}
