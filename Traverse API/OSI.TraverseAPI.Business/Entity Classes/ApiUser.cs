#region Using Directives
using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using TRAVERSE.Business;
using TRAVERSE.Business.Validation;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public partial class ApiUser : IPrincipal, IIdentity
    {
        #region Methods
        protected virtual EntityList<ApiUserFunction> LoadFunctionList()
        {
            if (_functionList == null)
            {
                _functionList = ApiUserFunctionProvider.RetreiveFunctionList(this);
                _functionList.ListChanged += FunctionList_Changed;
            }
            return _functionList;
        }

        protected virtual ApiUserToken LoadTokenInfo()
        {
            if (_tokenInfo == null)
            {
                _tokenInfo = ApiUserTokenProvider.RetrieveUserToken(this);
            }
            return _tokenInfo;
        }

        protected virtual ApiUserFunction LoadLastFunctionAccessed()
        {
            if (this.FunctionList != null && this.FunctionList.Count > 0)
            {
                var all = this.FunctionList.FindAll(f => f.LastAccess.HasValue);
                if (all != null && all.Count > 0)
                    return all.OrderByDescending(f => f.LastAccess.Value).FirstOrDefault();
            }
            return null;
        }

        private void FunctionList_Changed(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
                FunctionList[e.NewIndex].Parent = this;
        }

        private string GenerateSecret()
        {
            string guid = Guid.NewGuid().ToString("N");
            string time = DateTime.UtcNow.ToString("MMddyyyyHHmmssffff");
            string role = RoleType.ToString();

            string valueToEncrypt = string.Format("{0}|{1}|{2}", guid, time, role);

            SHA256 crypto = SHA256CryptoServiceProvider.Create();
            string result = Convert.ToBase64String(crypto.ComputeHash(Encoding.Unicode.GetBytes(valueToEncrypt)));

            return result;
        }

        protected virtual bool ValidateEmail(string propertyName, ref string errorDescription)
        {
            if (propertyName == Columns.EmailAddress.ToString())
            {
                try
                {
                    var mail = new System.Net.Mail.MailAddress(this.EmailAddress);
                }
                catch
                {
                    errorDescription = "Invalid email address";
                    return false;
                }
                return true;
            }
            return false;
        }

        public ApiUserToken UpdateRefreshToken()
        {
            DateTime issueTime = DateTime.UtcNow;

            string token = Guid.NewGuid().ToString("n");
            TokenInfo.TokenId = ApiUtility.GetHashValue(token);
            TokenInfo.IssueTime = issueTime;

            if (issueTime.AddDays(ApiUtility.ApiConfig.RefreshExpireDays.GetValueOrDefault()) > ExpirationDate.GetValueOrDefault(DateTime.MaxValue))
                TokenInfo.ExpireTime = ExpirationDate.Value;
            else
                TokenInfo.ExpireTime = issueTime.AddDays(ApiUtility.ApiConfig.RefreshExpireDays.GetValueOrDefault());

            return TokenInfo;
        }

        public bool ChangePassword(string newPassword)
        {
            try
            {
                UnencryptedPassword = newPassword;
                ResetPassword = false;
                ApiUserProvider.UpdateUser(this);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool GenerateUserToken(bool setUserAsActive)
        {
            try
            {
                if (UserStatus == ApiUserStatus.Disabled)
                    return true;

                ClientId = Guid.NewGuid();
                ClientSecret = GenerateSecret();
                if (setUserAsActive)
                    this.UserStatus = ApiUserStatus.Active;

                ApiUserProvider.UpdateUser(this);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion Methods

        #region Overrides
        protected override void AddValidationRules()
        {
            base.AddValidationRules();
            base.ValidationRules.AddRule(CommonRules.StringRequired, Columns.EmailAddress.ToString());
            base.ValidationRules.AddRule(CommonRules.StringRequired, Columns.Password.ToString());
            base.ValidationRules.AddRule(EntityRules.ValidEntityRule, new EntityRulesArgs(Columns.EmailAddress.ToString(), ValidateEmail));
            base.ValidationRules.AddRule(CommonRules.InRange<byte>, new CommonRules.RangeRuleArgs<byte>(Columns.Status.ToString(), new CommonRules.Range<byte>(0, 3)));
            base.ValidationRules.AddRule(CommonRules.InRange<byte>, new CommonRules.RangeRuleArgs<byte>(Columns.Role.ToString(), new CommonRules.Range<byte>(0, 1)));
        }

        public bool IsInRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role) || role.ToLower() == ApiUserRoleType.Normal.ToString().ToLower())
                return true;

            byte type = (byte)Enum.Parse(typeof(ApiUserRoleType), role);
            return ((byte)RoleType & type) == type;
        }
        #endregion Overrides

        #region Properties
        public virtual EntityList<ApiUserFunction> FunctionList { get => LoadFunctionList(); }

        public ApiUserToken TokenInfo { get => LoadTokenInfo(); }

        public ApiUserRoleType RoleType
        {
            get => (ApiUserRoleType)Role.GetValueOrDefault(1);
            set => Role = (byte)value;
        }

        public ApiUserStatus UserStatus
        {
            get => (ApiUserStatus)Status.GetValueOrDefault();
            set => Status = (byte)value;

        }

        [Bindable(true), Description]
        public DateTime? LastAccess { get => LastFunctionAccessed == null ? null : LastFunctionAccessed.LastAccess; }

        [Bindable(true), Description]
        public string LastFunction { get => LastFunctionAccessed == null ? null : LastFunctionAccessed.FunctionName; }

        [Bindable(true), Description]
        public string UnencryptedPassword
        {
            get => DataSecurity.DecryptData(Password);
            set => Password = DataSecurity.EncryptData(value);
        }

        public override byte? Role { get => base.Role.GetValueOrDefault(); set => base.Role = value; }

        public override byte? Status { get => base.Status.GetValueOrDefault(); set => base.Status = value; }

        public override DateTime? DateCreated
        {
            get => base.DateCreated ?? DateTime.Now;
            set => base.DateCreated = value;
        }

        public override DateTime? DateModified
        {
            get
            {
                if (this.IsDirty)
                    return DateTime.Now;

                return base.DateModified ?? DateCreated;
            }
            set => base.DateModified = value;
        }

        public override string ModifiedBy
        {
            get
            {
                if (this.IsDirty)
                    return ApplicationContext.CurrentUser;

                return base.ModifiedBy;
            }
            set => base.ModifiedBy = value;
        }

        public IIdentity Identity => this;

        string IIdentity.Name { get => this.Id.ToString(); }

        public string AuthenticationType => "TraverseAPI";

        public bool IsAuthenticated => true;

        private ApiUserFunction LastFunctionAccessed { get => LoadLastFunctionAccessed(); }
        #endregion Properties

        #region Fields
        private EntityList<ApiUserFunction> _functionList;
        private ApiUserToken _tokenInfo;
        #endregion Fields
    }
}
