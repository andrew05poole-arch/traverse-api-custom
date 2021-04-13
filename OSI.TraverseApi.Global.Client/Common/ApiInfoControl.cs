#region Using Directives
using OSI.TraverseApi.Business;
using System;
using TRAVERSE.Business;
using TRAVERSE.Client;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Client
{
    public partial class ApiInfoControl : ProcessControlBase
    {
        #region Constructors
        public ApiInfoControl()
            : this(null)
        { }

        public ApiInfoControl(IPlugin host)
            : base()
        {
            InitializeComponent();
            if (!DevEnvironment.InVSDesigner)
                this.HostPlugin = host;

            txtApiDb.Properties.ReadOnly = true;
            txtVersion.Properties.ReadOnly = true;
            ButtonActivity.Visible = false;
            ButtonReset.Visible = false;
            ButtonExecute.Text = Localizer.GetLocalizedString("btnOK");
            foreach (var item in ButtonExecute.GetCurrentParent().Items)
            {
                if (item is System.Windows.Forms.ToolStripSeparator)
                    ((System.Windows.Forms.ToolStripSeparator)item).Visible = false;
            }
        }
        #endregion Constructors

        #region Event Handlers
        private void chkLocalEnv_CheckStateChanged(object sender, EventArgs e)
        {
            try
            {
                this.OnchkLocalEnv_CheckStateChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void chkUseMsgSecurity_CheckStateChanged(object sender, EventArgs e)
        {
            try
            {
                this.SetMessageSecurityFields();
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OnchkLocalEnv_CheckStateChanged(EventArgs e)
        {
            if (this.chkLocalEnv.Checked && ApiUtility.ApiConfig.IsDebugLocalEnv.GetValueOrDefault() != this.chkLocalEnv.Checked)
            {
                var response = TravMessageBox.Show("",
                    "This option should only be used on development environments that are not connected to the internet. Would you like to continue?",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning,
                    System.Windows.Forms.MessageBoxDefaultButton.Button2);

                ApiUtility.ApiConfig.IsDebugLocalEnv = (System.Windows.Forms.DialogResult.Yes == response);
            }
        }
        #endregion Event Handlers

        #region Methods
        protected void ShowDatabaseCreateForm()
        {
            using (ApiCreateDbForm form = new ApiCreateDbForm())
            {
                form.ShowDialog(this);
            }
        }

        protected virtual void SetMessageSecurityFields()
        {
            this.cboMsgEncryptionType.ReadOnly = !this.chkUseMsgSecurity.Checked;
            this.txtSharedKey.ReadOnly = !this.chkUseMsgSecurity.Checked;
        }

        protected virtual void EnableFields(bool enable)
        {
            this.txtAccessExpire.ReadOnly = !enable;
            this.txtAuthTimeout.ReadOnly = !enable;
            this.txtRefreshExpire.ReadOnly = !enable;
            this.chkLocalEnv.ReadOnly = !enable;
            this.chkUseMsgSecurity.ReadOnly = !enable;
            this.ButtonExecute.Enabled = enable;
            this.SetMessageSecurityFields();
        }
        #endregion Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                if (!DevEnvironment.InVSDesigner)
                {
                    ApiUtility.ResetApiInfo();
                    ApiDbSelectForm.SelectCompany();

                    //No database found; We need to create one
                    if (string.IsNullOrEmpty(ApiUtility.CurrentApiDb))
                        ShowDatabaseCreateForm();

                    if (string.IsNullOrEmpty(ApiUtility.CurrentApiDb))
                        throw new InvalidValueException("No Api database could be found");

                    this.CompId = ApiUtility.CurrentApiDb;
                    base.OnLoad(e);
                    this.LoadData();
                }
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
            finally
            {
                this.EnableFields(BindingSource.Count > 0);
            }
        }

        protected override void LoadData()
        {
            this.BindingSource.RaiseListChangedEvents = false;
            this.BindingSource.DataSource = new EntityList<ApiInfo>(new[] { ApiUtility.ApiConfig });
            this.BindingSource.RaiseListChangedEvents = true;
            this.BindingSource.ResetBindings(true);

            ApiUtility.ApiConfig.MarkAsDirty();
        }

        public override void Execute()
        {
            this.layoutMain.Validate();
            ApiUtility.SaveApiInfo();
        }

        protected override void UpdateData(int index)
        { }

        protected override void InitExtension()
        {
            this.EntityName = "";
        }
        #endregion Overrides

    }
}
