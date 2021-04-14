using OSI.TraverseApi.Business;
using System;
using System.ComponentModel;
using TRAVERSE.Business;
using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public partial class AddNewUserForm : HostForm
    {
        #region Constructors
        public AddNewUserForm()
            : this(string.Empty)
        { }

        public AddNewUserForm(string compId)
        {
            this.InitializeComponent();
            this.CompId = compId;
        }
        #endregion Constructors

        #region Event Handlers
        private void cboCopyFrom_EditValueChanged(object sender, EventArgs e)
        {
            try
            {
                OncboCopyFrom_EditValueChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void chkCopyFunctions_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                OnchkCopyFunctions_CheckedChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                OnbtnOk_Click(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                OnbtnCancel_Click(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void ProcessWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                OnProcessWorkerCompleted(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void ProcessWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                OnProcessWorkerDoWork(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OncboCopyFrom_EditValueChanged(EventArgs e)
        {
            var user = ((IEntityList)cboCopyFrom.DataSource)[cboCopyFrom.ItemIndex] as ApiUser;
            if (user != null)
            {
                EnableCopyOptions(true);
                SetCopyDefault();
            }
        }

        protected virtual void OnchkCopyFunctions_CheckedChanged(EventArgs e)
        {
            EnableCopyOptions(true);
            SetFunctionCopyDefault(chkCopyFunctions.Checked);
        }

        protected virtual void OnbtnCancel_Click(EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        protected virtual void OnbtnOk_Click(EventArgs e)
        {
            try
            {
                this.ProcessEngine.CopyName = this.chkCopyName.Checked;
                this.ProcessEngine.CopyNotes = this.chkCopyNotes.Checked;
                this.ProcessEngine.CopyFunctionList = this.chkCopyFunctions.Checked;
                this.ProcessEngine.CopyCompanyList = this.chkCopyPermissions.Checked;
                this.ProcessEngine.CopyCompanyFilter = this.chkCopyFilterSettings.Checked;
                this.ProcessEngine.CopyEntitySchema = this.chkCopyFieldNames.Checked;
                this.ProcessEngine.CopyCustomFieldSchema = this.chkCopyCustomFields.Checked;

                this.ProcessEngine.UserTo = this.NewUserInfo;
                if (this.cboCopyFrom.ItemIndex >= 0)
                    this.ProcessEngine.UserFrom = ((IEntityList)this.cboCopyFrom.DataSource)[this.cboCopyFrom.ItemIndex] as ApiUser;

                ProcessWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                if (!ProcessWorker.IsBusy)
                    ClientContext.HandleError(ex, this);
            }
        }

        protected virtual void OnProcessWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                throw e.Error;

            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        protected virtual void OnProcessWorkerDoWork(DoWorkEventArgs e)
        {
            this.ProcessEngine.Execute(UpdateStatusText);
        }
        #endregion Event Handlers

        #region Methods
        protected virtual void EnableCopyOptions(bool enable)
        {
            chkCopyName.Enabled = enable;
            chkCopyNotes.Enabled = enable;
            chkCopyFunctions.Enabled = enable;
            chkCopyPermissions.Enabled = enable && this.chkCopyFunctions.Checked;
            chkCopyFilterSettings.Enabled = enable && this.chkCopyFunctions.Checked;
            chkCopyFieldNames.Enabled = enable && this.chkCopyFunctions.Checked;
            chkCopyCustomFields.Enabled = enable && this.chkCopyFunctions.Checked;
        }

        protected virtual void SetCopyDefault()
        {
            chkCopyName.Checked = false;
            chkCopyNotes.Checked = false;
            chkCopyFunctions.Checked = true;
            chkCopyPermissions.Checked = true;
            chkCopyFilterSettings.Checked = true;
            chkCopyFieldNames.Checked = true;
            chkCopyCustomFields.Checked = true;
        }

        protected virtual void SetFunctionCopyDefault(bool check)
        {
            chkCopyPermissions.Checked = check;
            chkCopyFilterSettings.Checked = check;
            chkCopyFieldNames.Checked = check;
            chkCopyCustomFields.Checked = check;
        }

        protected virtual void LoadUserComboBox()
        {
            cboCopyFrom.DataSource = EntityProvider.GetEntityList<ApiUser, ApiUserProvider>(CompId, null, null);
            cboCopyFrom.ShowColumns(new string[] {
                ApiUserBase.Columns.EmailAddress.ToString() });
        }

        protected virtual void InitStatus()
        {
            this.emptySpaceStatus.Text = "Initializing";
            this.emptySpaceStatus.TextVisible = true;
        }

        protected virtual void EndStatus()
        {
            this.emptySpaceStatus.Text = " ";
            this.emptySpaceStatus.TextVisible = false;
        }

        protected virtual void UpdateStatusText(string message)
        {
            if (this.InvokeRequired)
                this.Invoke(new Status(UpdateStatusText), message);
            else
                this.emptySpaceStatus.Text = message;
        }
        #endregion Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                EnableCopyOptions(false);
                chkCopyName.Checked = false;
                chkCopyNotes.Checked = false;
                chkCopyFunctions.Checked = false;
                chkCopyPermissions.Checked = false;
                chkCopyFilterSettings.Checked = false;
                chkCopyFieldNames.Checked = false;
                chkCopyCustomFields.Checked = false;
                LoadUserComboBox();

                this.bindUser.RaiseListChangedEvents = false;
                this.bindUser.DataSource = new EntityList<ApiUser>(new[] { this.NewUserInfo });
                this.bindUser.RaiseListChangedEvents = true;
                this.bindUser.ResetBindings(true);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }
        #endregion Overrides

        #region Properties
        protected virtual string CompId { get; set; }

        public virtual ApiUser NewUserInfo
        {
            get
            {
                if (_newUserInfo == null)
                    _newUserInfo = new ApiUser(CompId) { ResetPassword = true };

                return _newUserInfo;
            }
        }

        protected virtual ApiUserCopyProcess ProcessEngine
        {
            get
            {
                if (_processEngine == null)
                    _processEngine = ProcessBase.LoadProcessEngine<ApiUserCopyProcess>(CompId);

                return _processEngine;
            }
        }

        private BackgroundWorker ProcessWorker
        {
            get
            {
                if (_worker == null)
                {
                    _worker = new BackgroundWorker();
                    _worker.DoWork += ProcessWorkerDoWork;
                    _worker.RunWorkerCompleted += ProcessWorkerCompleted;
                }
                return _worker;
            }
        }
        #endregion Properties

        #region Fields
        private ApiUser _newUserInfo;
        private ApiUserCopyProcess _processEngine;
        private BackgroundWorker _worker;
        #endregion Fields
    }
}
