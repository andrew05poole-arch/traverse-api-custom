#region Using Directives
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
#endregion Using Directives

namespace TraverseApiInstaller
{
    public partial class InstallerForm : Form
    {
        #region Fields
        private BackgroundWorker _processWorker;
        private ProcessWebInstall _processEngine;
        #endregion Fields

        #region Constructors
        public InstallerForm()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region Event Handlers
        private void cboWebsite_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                this.OncboWebsite_SelectedValueChanged(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnProceed_Click(object sender, EventArgs e)
        {
            try
            {
                this.OnbtnProceed_Click(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ProcessWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            ProcessEngine.Execute(new InstallUtility.Status(UpdateStatus));
        }

        private void ProcessWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.EndStatus();
            this.EnableFields(true);
            this.Cursor = Cursors.Default;
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
                return;
            }
            ApiConfigInfo.SaveSettings();
            if (DialogResult.Yes == MessageBox.Show("Process has completed successfully!\r\nWould you like to restart the website now?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2))
            {
                InstallUtility.Helper.StartApplicationPool();
                InstallUtility.Helper.StartSite();
            }
            this.Close();
        }

        protected virtual void OncboWebsite_SelectedValueChanged(EventArgs e)
        {
            this.LoadWebApp();
        }

        protected virtual void OnbtnProceed_Click(EventArgs e)
        {
            try
            {
                this.Validate();
                string validMsg = ProcessEngine.Validate();
                this.InitStatus();
                this.EnableFields(false);
                if (DialogResult.No == MessageBox.Show("This process will stop the current IIS service, if running. You will have to manually restart it once the process is complete. Would you like to continue?", "Open Systems Api", MessageBoxButtons.YesNo))
                    return;

                this.Cursor = Cursors.WaitCursor;
                ProcessWorker.RunWorkerAsync();
            }
            finally
            {
                if (!ProcessWorker.IsBusy)
                {
                    this.EnableFields(true);
                    this.EndStatus();
                    this.Cursor = Cursors.Default;
                }
            }
        }

        protected virtual void UpdateStatus(string message, double percentageComplete)
        {
            if (this.InvokeRequired)
                this.Invoke(new InstallUtility.Status(UpdateStatus), message, percentageComplete);
            else
            {
                this.txtStatus.Text = message;
                if (percentageComplete != InstallUtility.NoPctComplete)
                    this.txtPctComplete.Text = string.Format("{0:0.00%}", percentageComplete);
                else
                    this.txtPctComplete.Text = string.Empty;
            }
        }
        #endregion Event Handlers

        #region Protected Methods
        protected void LoadWebSiteCombo()
        {
            this.cboWebsite.Items.Clear();
            this.cboWebsite.Items.AddRange(InstallUtility.Helper.WebsiteInfoList.Keys.ToArray());
            if (InstallUtility.Helper.WebsiteInfoList.Keys.Count > 0)
            {
                if (InstallUtility.Helper.WebsiteInfoList.ContainsKey("TraverseApi"))
                {
                    ApiConfigInfo.Configuration.WebsiteName = "TraverseApi";
                    return;
                }

                ApiConfigInfo.Configuration.WebsiteName = InstallUtility.Helper.WebsiteInfoList.Keys.ToArray()[0];
            }
        }

        protected void LoadWebApp()
        {
            this.cboWebApp.Items.Clear();

            if (!InstallUtility.Helper.WebsiteInfoList.ContainsKey(this.cboWebsite.Text))
                this.cboWebApp.Enabled = false;
            else
            {
                this.cboWebApp.Items.AddRange(InstallUtility.Helper.WebsiteInfoList[this.cboWebsite.Text].ToArray());
                this.cboWebApp.Enabled = (this.cboWebApp.Items.Count > 1);
                if (this.cboWebApp.Items.Count > 0)
                    this.cboWebApp.SelectedIndex = 0;
            }
        }

        protected void InitStatus()
        {
            this.UpdateStatus("Initializing...", InstallUtility.NoPctComplete);
        }

        protected void EndStatus()
        {
            this.UpdateStatus("", InstallUtility.NoPctComplete);
        }

        protected void EnableFields(bool enable)
        {
            this.txtServerName.Enabled = enable;
            this.txtServerPort.Enabled = enable;
            this.txtUsername.Enabled = enable;
            this.txtSysDb.Enabled = enable;
            this.txtCompId.Enabled = enable;
            this.cboWebsite.Enabled = enable;
            this.cboWebApp.Enabled = enable && InstallUtility.Helper.WebsiteInfoList.ContainsKey(this.cboWebsite.Text);
            this.txtApiDb.Enabled = enable;
            this.btnProceed.Enabled = enable;
        }
        #endregion Protected Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                this.LoadWebSiteCombo();
                this.LoadWebApp();
                this.EnableFields(true);

                this.bindMain.RaiseListChangedEvents = false;
                this.bindMain.DataSource = ApiConfigInfo.Configuration;
                this.bindMain.RaiseListChangedEvents = true;
                this.bindMain.ResetBindings(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion Overrides

        #region Properties
        protected BackgroundWorker ProcessWorker
        {
            get
            {
                if (_processWorker == null)
                {
                    _processWorker = new BackgroundWorker();
                    _processWorker.DoWork += ProcessWorkerDoWork;
                    _processWorker.RunWorkerCompleted += ProcessWorkerCompleted;
                }
                return _processWorker;
            }
        }

        protected ProcessWebInstall ProcessEngine
        {
            get => _processEngine != null ? _processEngine : (_processEngine = new ProcessWebInstall());
        }
        #endregion Properties
    }
}
