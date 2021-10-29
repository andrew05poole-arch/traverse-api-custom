#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.Data;
using TRAVERSE.Business;
using TRAVERSE.Business.FormPrinter;
using TRAVERSE.Client;
using TRAVERSE.Client.FormPrinter;
using TRAVERSE.Client.Report;
using TRAVERSE.Controls;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Client
{
    public partial class ApiUserAccountControl : PluginControlBase, ITravNavigatable, IFormPrinter, IFormPrinter2, IFormPrinter3
    {
        #region Constructors
        public ApiUserAccountControl()
            : this(null)
        { }

        public ApiUserAccountControl(IPlugin host)
            : base(host)
        {
            InitializeComponent();
            layoutMain.SetFont();
            this.chkResetPwd.Properties.ReadOnly = true;

            if (!DevEnvironment.InVSDesigner)
                base.HostPlugin = host;
        }
        #endregion Constructors

        #region Event Handlers
        private void lkpEmail_LookupValueSelected(object sender, LookupValueSelectedEventArgs e)
        {
            try
            {
                this.OnlkpEmail_LookupValueSelected(e);
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
                this.OnBindingSourceCurrentChanged(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnDisable_TravNavItemClick(object sender, TravNavigatorEventArgs e)
        {
            try
            {
                this.OnDisableButtonTravNavItemClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnEnable_TravNavItemClick(object sender, TravNavigatorEventArgs e)
        {
            try
            {
                this.OnEnableButtonTravNavItemClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnResetPwd_TravNavItemClick(object sender, TravNavigatorEventArgs e)
        {
            try
            {
                this.OnResetPwdButtonTravNavItemClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnFunction_TravNavItemClick(object sender, TravNavigatorEventArgs e)
        {
            try
            {
                this.OnFunctionButtonTravNavItemClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void ButtonNotifyClick(object sender, EventArgs e)
        {
            try
            {
                OnButtonNotifyClick(e);
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
        }

        private void btnSearchDocumentQueue_Click(object sender, EventArgs e)
        {
            try
            {
                this.ShowDocumentQueue();
            }
            catch (Exception exception)
            {
                ClientContext.HandleError(exception, this);
            }
        }

        protected virtual void OnDisableButtonTravNavItemClick(TravNavigatorEventArgs e)
        {
            if (this.CurrentApiUser == null)
                return;

            this.CurrentApiUser.BeginEdit();
            this.CurrentApiUser.UserStatus = ApiUserStatus.Disabled;
            this.CurrentApiUser.ResetPassword = false;
            this.CurrentApiUser.ClientId = null;
            this.CurrentApiUser.ClientSecret = null;
            this.SaveCurrentItem();
            this.EnableButtons();
        }

        protected virtual void OnEnableButtonTravNavItemClick(TravNavigatorEventArgs e)
        {
            if (this.CurrentApiUser == null)
                return;

            var result = TravMessageBox.Show("", "This action will reactivate the user. The user will need to generate new API credentials through the portal. Would you like to continue?",
                System.Windows.Forms.MessageBoxButtons.YesNo);

            if (result == System.Windows.Forms.DialogResult.No)
                return;

            this.CurrentApiUser.BeginEdit();
            this.CurrentApiUser.UserStatus = ApiUserStatus.Renew;
            this.SaveCurrentItem();
            this.EnableButtons();
        }

        protected virtual void OnResetPwdButtonTravNavItemClick(TravNavigatorEventArgs e)
        {
            if (this.CurrentApiUser == null)
                return;

            var result = TravMessageBox.Show("", "This action will allow you to reset the password and force the user to change the password as well as generate new credentials through the portal. Would you like to continue?",
                System.Windows.Forms.MessageBoxButtons.YesNo);

            if (result == System.Windows.Forms.DialogResult.No)
                return;

            this.CurrentApiUser.BeginEdit();
            this.CurrentApiUser.ResetPassword = true;
            this.CurrentApiUser.UserStatus = ApiUserStatus.Renew;
            this.SaveCurrentItem();
            this.EnableButtons();
            this.SetReadOnly(false);
        }

        protected virtual void OnFunctionButtonTravNavItemClick(TravNavigatorEventArgs e)
        {
            if (CurrentApiUser == null)
                return;

            if (CurrentApiUser.IsDirty)
                SaveCurrentItem();

            if (CurrentApiUser.IsDirty)
                return;

            ShowFunctionList(CurrentApiUser);
            this.LoadDataById(this.CurrentApiUser.Id);
        }

        protected virtual void OnlkpEmail_LookupValueSelected(LookupValueSelectedEventArgs e)
        {
            if (this.CurrentApiUser != null && this.CurrentApiUser.IsNew)
                return;

            if (!e.NotInList && this.lkpEmailAddr.Properties.SelectedRow != null)
            {
                this.LoadDataById((long)this.lkpEmailAddr.Properties.SelectedRow["ID"]);
            }
            else
                this.LoadDataByEmail(this.lkpEmailAddr.EditValue as string);
        }

        protected virtual void OnBindingSourceCurrentChanged(EventArgs e)
        {
            this.CurrentApiUser = BindingSource.Current as ApiUser;
            this.EnableButtons();
        }

        protected virtual void OnnavUserRefreshData(EventArgs e)
        {
            try
            {
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
                if (this.CurrentApiUser != null)
                {
                    if (this.CurrentApiUser.IsDirty &&
                        System.Windows.Forms.DialogResult.Yes == TravMessageBox.Show("SaveAndRefresh", "Save changes and refresh data?", System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Question))
                    {
                        this.SaveCurrentItem();
                    }
                    this.LoadDataById(this.CurrentApiUser.Id);
                }
            }
            finally
            {
                this.Cursor = System.Windows.Forms.Cursors.Default;
            }
        }

        protected virtual void OnButtonNotifyClick(EventArgs e)
        {
            ProcessEmailUser();
        }

        protected virtual void ShowDocumentQueue()
        {
            LoadFunctionEventArgs args = new LoadFunctionEventArgs("TRAVERSE.Client.ReportViewer.dll", "TRAVERSE.Client.Report.ManageDocumentQueuePopupPlugin");
            string filter = string.Format("FunctionID = {0}", SqlUtil.Encode(this.FunctionId, true));
            args.Parameters.Add("FunctionFilter", filter);
            this.LoadFunction(this, args);
        }
        #endregion Event Handlers

        #region Protected Methods
        protected virtual void LoadEmailLookup()
        {
            var lookup = Lookup.CreateLookupDef("ApiUser");
            this.lkpEmailAddr.LoadList(CompId, lookup, null, null);
            this.lkpEmailAddr.DisplayMember = "Email Address";
            this.lkpEmailAddr.ValueMember = "Email Address";
        }

        protected virtual ApiUser GetUser(string email)
        {
            SqlFilterBuilder<ApiUserBase.Columns> builder = new SqlFilterBuilder<ApiUserBase.Columns>();
            builder.AppendEquals(ApiUserBase.Columns.EmailAddress, email);
            ProviderItem.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));
            if (ProviderItem.Items.Count > 0)
                return ProviderItem.Items[0];

            return null;
        }

        protected virtual void LoadDataByEmail(string email)
        {
            try
            {
                Provider = this.ProviderItem;
                BindingSource.RaiseListChangedEvents = false;
                SqlFilterBuilder<ApiUserBase.Columns> builder = new SqlFilterBuilder<ApiUserBase.Columns>();
                builder.AppendEquals(ApiUserBase.Columns.EmailAddress, email);
                BindingSource.DataSource = ProviderItem.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));
                BindingSource.RaiseListChangedEvents = true;
                BindingSource.ResetBindings(false);
                txtName.Focus();
                SetReadOnly(BindingSource.List.Count == 0);
                OnUpdateTravNavigator();
            }
            catch (Exception exception)
            {
                ClientContext.HandleError(exception, this);
            }
        }

        protected virtual void LoadDataById(long id)
        {
            Provider = this.ProviderItem;
            BindingSource.RaiseListChangedEvents = false;
            SqlFilterBuilder<ApiUserBase.Columns> builder = new SqlFilterBuilder<ApiUserBase.Columns>();
            builder.AppendEquals(ApiUserBase.Columns.Id, id.ToString());
            BindingSource.DataSource = ProviderItem.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));
            BindingSource.RaiseListChangedEvents = true;
            BindingSource.ResetBindings(false);
            txtName.Focus();
            SetReadOnly(BindingSource.List.Count == 0);
            OnUpdateTravNavigator();
        }

        protected virtual bool NotifyButtonAllowed()
        {
            if (!_allowNotify.HasValue)
            {
                DocumentDeliverySettingsProvider provider = new DocumentDeliverySettingsProvider();
                SqlFilterBuilder<DocumentDeliverySettingsBase.Columns> builder = new SqlFilterBuilder<DocumentDeliverySettingsBase.Columns>();
                builder.AppendEquals(DocumentDeliverySettingsBase.Columns.FormId, this.FormId);
                provider.Load(ApplicationContext.CompId, new FilterCriteria(builder.ToString(), "") { RecordCount = 1 });

                if ((_allowNotify = (provider.Items.Count > 0)).GetValueOrDefault())
                {
                    DocumentName = string.IsNullOrEmpty(provider.Items[0].DocumentName) ? "Api Consumer Guide" : provider.Items[0].DocumentName;
                }

            }
            return _allowNotify.Value;
        }

        protected virtual void EnableButtons()
        {
            btnDisable.Enabled = (CurrentApiUser != null && CurrentApiUser.UserStatus != ApiUserStatus.Disabled);
            btnResetPwd.Enabled = (CurrentApiUser != null && !CurrentApiUser.ResetPassword.GetValueOrDefault() && CurrentApiUser.UserStatus != ApiUserStatus.Disabled);
            btnEnable.Enabled = (CurrentApiUser != null && CurrentApiUser.UserStatus == ApiUserStatus.Disabled);
            btnNotify.Enabled = (CurrentApiUser != null && CurrentApiUser.ResetPassword.GetValueOrDefault() && AllowNotifyButton);
        }

        protected virtual void CreateNewUser()
        {
            ApiUser user = CreateNew();
            if (user == null)
                return;

            ProviderItem.Items.Clear();
            ProviderItem.Items.Add(user);
            BindingSource.DataSource = ProviderItem.Items;
            BindingSource.ResetBindings(false);
            txtName.Focus();
            SetReadOnly(false);
            OnUpdateTravNavigator();
        }

        protected virtual ApiUser CreateNew()
        {
            using (AddNewUserForm form = new AddNewUserForm(CompId))
            {
                if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    return form.NewUserInfo;
            }
            return null;
        }

        protected virtual void SetReadOnly(bool readOnly)
        {
            lkpEmailAddr.ReadOnly = readOnly && string.IsNullOrEmpty(ApiUtility.CurrentApiDb);
            navUser.Enabled = !string.IsNullOrEmpty(ApiUtility.CurrentApiDb);
            dtExpirationDate.ReadOnly = readOnly;
            txtAddlInfo.ReadOnly = readOnly;
            txtName.ReadOnly = readOnly;
            txtPassword.ReadOnly = (CurrentApiUser == null || (!CurrentApiUser.IsNew && !CurrentApiUser.ResetPassword.GetValueOrDefault()) || readOnly);
            cboRoleType.ReadOnly = readOnly;
        }

        protected virtual void CreateSearchDocQueueButton()
        {
            var btnSrchDocQueue = new TravNavItem() { Text = Localizer.GetLocalizedString("btnSearchDocumentQueue"), DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text };
            btnSrchDocQueue.Click += new EventHandler(this.btnSearchDocumentQueue_Click);
            navUser.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            navUser.Items.Add(btnSrchDocQueue);
        }

        protected virtual DataSet BuildEmailDataset()
        {
            if (_emailSet == null)
            {
                _emailSet = new DataSet();
                var table = _emailSet.Tables.Add();
                table.Columns.AddRange(new DataColumn[]
                {
                    new DataColumn("Id", typeof(long)),
                    new DataColumn("Name", typeof(string)),
                    new DataColumn("Password", typeof(string)),
                    new DataColumn("EmailAddress", typeof(string)),
                    new DataColumn("ExpirationDate", typeof(DateTime)),
                    new DataColumn("AdditionalInfo", typeof(string)),
                    new DataColumn("Role", typeof(byte))
                });
            }

            //Clear out the old data and repopulate with the current user information
            _emailSet.Tables[0].Rows.Clear();
            if (this.CurrentApiUser != null)
            {
                _emailSet.Tables[0].Rows.Add(new object[]
                {
                    CurrentApiUser.Id,
                    CurrentApiUser.Name,
                    CurrentApiUser.UnencryptedPassword,
                    CurrentApiUser.EmailAddress,
                    CurrentApiUser.ExpirationDate,
                    CurrentApiUser.AdditionalInfo,
                    CurrentApiUser.Role
                });
            }

            return _emailSet;
        }

        protected virtual List<IMergeFieldDefinition> GetFieldDefinitions()
        {
            if (_fieldDefinitions == null)
            {
                _fieldDefinitions = new List<IMergeFieldDefinition>();
                _fieldDefinitions.Add(new MergeFieldDefinition("Name", MergeFieldType.Undefined));
                _fieldDefinitions.Add(new MergeFieldDefinition("Password", MergeFieldType.Undefined));
                _fieldDefinitions.Add(new MergeFieldDefinition("EmailAddress", MergeFieldType.Undefined));
                _fieldDefinitions.Add(new MergeFieldDefinition("ExpirationDate", MergeFieldType.Date));
                _fieldDefinitions.Add(new MergeFieldDefinition("AdditionalInfo", MergeFieldType.Undefined));

                var roleKeys = new Dictionary<MergeFieldDefinition.ExtensionKey, object>();
                roleKeys.Add(MergeFieldDefinition.ExtensionKey.EnumLocalizerId, "0;Normal;1;Administrator");
                _fieldDefinitions.Add(new MergeFieldDefinition("Role", MergeFieldType.Enum, roleKeys));
            }
            return _fieldDefinitions;
        }

        protected void ExportAsPDF(TravReportBase report, string tempFileName)
        {
            DevExpress.XtraPrinting.PdfExportOptions pdfExportOption = new DevExpress.XtraPrinting.PdfExportOptions()
            {
                Compressed = true
            };
            pdfExportOption.DocumentOptions.Author = ApplicationContext.CurrentUser;
            report.CreateDocument();
            report.ExportToPdf(tempFileName, pdfExportOption);
        }

        protected virtual void ProcessEmailUser()
        {
            try
            {
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
                var manager = new DocumentDeliveryManger() { FormPrinterDef = this };

                var report = TravReportBase.GenerateSingleDocument(this.ReportObjectType(true), this.ReportParameters, this.DataSet, this.TableName, string.Empty, null, new ReportExtension() { LayoutId = this.FunctionId });

                DocumentQueue queue = new DocumentQueue(ApplicationContext.CompId)
                {
                    FormId = this.FormId,
                    DeliveryMethod = DeliveryMethod.Email,
                    DeliveryName = this.CurrentApiUser.Name,
                    DeliveryDestination = this.CurrentApiUser.EmailAddress,
                    ContactId = string.Empty,
                    FunctionId = Guid.Parse(this.FunctionId)
                };

                string tempPath1 = System.IO.Path.GetTempPath();
                var path = System.IO.Path.Combine(tempPath1, string.Concat(Guid.NewGuid().ToString(), ".pdf"));
                this.ExportAsPDF(report, path);

                var attachment = queue.AttachmentList.AddNew();
                attachment.SetFile(path);
                attachment.DisplayName = string.Format("{0}.pdf", this.DocumentName);

                manager.ProcessQueue(queue, this.DataSet.Tables[0].Rows[0]);
                manager.Commit();
                TravMessageBox.Show(string.Empty, Localizer.GetLocalizedString("ProcessComplete"),
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Asterisk);
            }
            finally
            {
                this.Cursor = System.Windows.Forms.Cursors.Default;
            }
        }
        #endregion Protected Methods

        #region Public Methods
        public virtual void ShowFunctionList(ApiUser user)
        {
            if (user == null)
                return;

            LoadFunctionEventArgs args = new LoadFunctionEventArgs("OSI.TraverseApi.Client.dll", "ApiUserFunctionPlugin");
            args.FunctionCaption = "Function Setup";
            args.Parameters.Add("CompanyId", CompId);
            args.Parameters.Add("User", user);
            LoadFunction(this, args);
        }

        public void AssignDocumentNumber()
        { }

        public Type ReportObjectType(bool forArchive)
        {
            return typeof(ApiConsumerReport);
        }

        public void HandleDocumentDeliveryFieldFormatting(IMergeFieldFormatInfo formatInfo)
        { }
        #endregion Public Methods

        #region Overrides
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                if (!DevEnvironment.InVSDesigner)
                {
                    ApiDbSelectForm.SelectCompany();
                    if (string.IsNullOrEmpty(ApiUtility.CurrentApiDb))
                        throw new InvalidValueException("No Api database could be found");

                    this.CompId = ApiUtility.CurrentApiDb;
                    this.LoadEmailLookup();
                    this.CreateSearchDocQueueButton();
                    if (base.HostPlugin.Parameters != null && base.HostPlugin.Parameters.ContainsKey("EmailAddress"))
                    {
                        string str = base.HostPlugin.Parameters["EmailAddress"].ToString();
                        this.LoadDataByEmail(str);
                        this.BindingSource.Position = this.BindingSource.Find("EmailAddress", str);
                    }
                    else if (base.HostPlugin.Parameters != null && base.HostPlugin.Parameters.ContainsKey("Id"))
                    {
                        string str = base.HostPlugin.Parameters["Id"].ToString();
                        this.LoadDataById(long.Parse(str));
                        this.BindingSource.Position = this.BindingSource.Find("Id", str);
                    }
                    else if (this.HasFindKey())
                    {
                        string item = this.GetFindKeyValues<string>()[0];
                        this.IsMaintenance = true;
                        this.LoadDataById(long.Parse(item));
                    }
                    base.OnLoad(e);
                }
            }
            catch (Exception ex)
            {
                ClientContext.HandleError(ex, this);
            }
            finally
            {
                this.SetReadOnly(BindingSource.Count == 0);
                this.EnableButtons();
            }
        }

        protected override void RefreshData()
        {
            this.OnnavUserRefreshData(null);
        }

        protected override void UpdateData(int index)
        {
            try
            {
                if (index < 0)
                {
                    if (this.ProviderItem.Items.IsDeletedCount > 0)
                    {
                        this.ProviderItem.Update(CompId);
                    }
                }
                else
                {
                    if (this.ProviderItem[index].IsDirty)
                    {
                        this.ProviderItem[index].Validate();
                        if (!this.ProviderItem[index].IsValid)
                            TravMessageBox.Show("", this.ProviderItem[index].Error, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                        else
                            this.ProviderItem.Update(CompId);
                    }
                    this.ProviderItem[index].SetErrorText(null);
                }
            }
            catch (Exception ex)
            {
                if (ex is ProviderException && index > -1)
                {
                    this.ProviderItem[index].SetErrorText(ex.Message);
                }
                ClientContext.HandleError(ex, this);
            }
            finally
            {
                if (this.ProviderItem.Items.IsDeletedCount > 0)
                {
                    this.ProviderItem.Items.AddRange(this.ProviderItem.Items.DeletedItems);
                    this.ProviderItem.Items.DeletedItems.Clear();
                    this.BindingSource.ResetBindings(false);
                }
                EntityProvider.InvalidateLookupObject("ApiUser");
                this.SetReadOnly(this.BindingSource.List.Count == 0);
                this.OnUpdateTravNavigator();
            }
        }

        void ITravNavigatable.AddRow()
        {
            if (this.CurrentApiUser != null && this.CurrentApiUser.IsDirty)
                this.SaveCurrentItem();

            if (this.CurrentApiUser == null || !this.CurrentApiUser.IsDirty)
            {
                this.Provider = this.ProviderItem;
                this.CreateNewUser();
            }
        }

        protected override void InitExtension()
        {
            Extension.Root = layoutMain;
            Extension.LayoutRoot = layoutMain;
            Extension.Navigator = navUser;
        }

        public override object DesignRoot => layoutMain;
        #endregion Overrides

        #region Properties
        protected virtual ApiUserProvider ProviderItem { get; } = new ApiUserProvider();

        protected virtual ApiUser CurrentApiUser { get; set; }

        protected virtual bool IsMaintenance { get; set; }

        protected virtual bool AllowNotifyButton { get => NotifyButtonAllowed(); }

        public virtual string DocumentName { get; set; }

        public DataSet DataSet => BuildEmailDataset();

        public string TableName
        {
            get
            {
                if (this.DataSet == null || this.DataSet.Tables.Count <= 0)
                {
                    return string.Empty;
                }
                return this.DataSet.Tables[0].TableName;
            }
        }

        public string FormId => Business.Properties.Resources.DocDeliveryFormId;

        public string ContactColumnName => Business.Properties.Resources.DocDeliveryContactName;

        public object ReportParameters => null;

        public string FieldName => Business.Properties.Resources.DocDeliveryFieldName;

        public string SourceIdColumn => Business.Properties.Resources.DocDeliverySourceIdColumn;

        public string DocumentNoColumn => Business.Properties.Resources.DocDeliverySourceIdColumn;

        public bool ArchiveEnabled => false;

        public string ArchiveWatermarkText => string.Empty;

        public string KeyFieldName => FieldName;

        public List<IMergeFieldDefinition> FieldDefinitions => GetFieldDefinitions();
        #endregion Properties

        #region Fields
        private DataSet _emailSet;
        private List<IMergeFieldDefinition> _fieldDefinitions;
        private bool? _allowNotify;
        #endregion Fields
    }
}
