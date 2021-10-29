#region Using Directives
using System;
using System.ComponentModel;
using System.Threading;
using TRAVERSE.Business;
using TRAVERSE.Business.Validation;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public partial class ApiUserFunction
    {
        #region Constructors
        static ApiUserFunction()
        {
            _processor = new Semaphore(0, 1);
            _processor.Release(1);
        }
        #endregion Constructors

        #region Methods
        protected virtual bool ValidateIfNotNull(string propertyName)
        {
            switch (propertyName)
            {
                case "FunctionId":
                    return !SkipLookupValidation && FunctionId != null && FunctionId != Guid.Empty;
                case "UserId":
                    return true;
                default:
                    return false;
            }
        }

        protected virtual bool ValidateUser(string propertyName, ref string errorDescription)
        {
            if (propertyName == Columns.UserId.ToString())
            {
                if (!SkipLookupValidation && this.Parent == null)
                {
                    errorDescription = "Invalid user.";
                    return false;
                }
                return true;
            }
            return false;
        }

        protected virtual ApiUserFunctionAccess LoadLastAccessInfo()
        {
            if (_requestInfo == null)
                _requestInfo = ApiUserFunctionAccessProvider.GetLastAccess(this) ?? new ApiUserFunctionAccess() { Parent = this };
            return _requestInfo;
        }

        protected virtual ApiFunctionHeader LoadFunctionInfo()
        {
            if (_functionInfo == null)
                _functionInfo = ApiFunctionHeaderProvider.GetFunction(this);
            return _functionInfo;
        }

        protected virtual EntityList<ApiUserFunctionSchema> LoadSchemaList()
        {
            if (_schemaList == null)
            {
                _schemaList = ApiUserFunctionSchemaProvider.GetFunctionSchemaList(this);
                _schemaList.ListChanged += SchemaList_Changed;
            }
            return _schemaList;
        }

        protected virtual EntityList<ApiUserFunctionComp> LoadCompanyList()
        {
            if (_companyList == null)
            {
                _companyList = ApiUserFunctionCompProvider.GetFunctionCompanyList(this);
                _companyList.ListChanged += CompanyList_Changed;
            }
            return _companyList;
        }

        private void SchemaList_Changed(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
                SchemaList[e.NewIndex].Parent = this;
        }

        private void CompanyList_Changed(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
                CompanyList[e.NewIndex].Parent = this;
        }

        public virtual async System.Threading.Tasks.Task UpdateRequestInfo(string method)
        {
            DateTime current = DateTime.Now;

            _processor.WaitOne();
            if (RequestInfo.IsNew || !RequestInfo.FirstAccessTime.HasValue)
                RequestInfo.FirstAccessTime = current;

            RequestInfo.LastAccessTime = current;
            RequestInfo.AccessCount++;
            switch (method.ToLower())
            {
                case "get":
                    RequestInfo.LastAccessMethod = 1;
                    break;
                case "post":
                    RequestInfo.LastAccessMethod = 2;
                    break;
                case "put":
                    RequestInfo.LastAccessMethod = 4;
                    break;
                case "delete":
                    RequestInfo.LastAccessMethod = 8;
                    break;
                case "head":
                    RequestInfo.LastAccessMethod = 16;
                    break;
                case "schema":
                    RequestInfo.LastAccessMethod = 32;
                    break;
            }

            await System.Threading.Tasks.Task.Run(() => ApiUserFunctionAccessProvider.UpdateLastAccess(RequestInfo));
            _processor.Release();
        }
        #endregion Methods

        #region Overrides
        protected override void AddValidationRules()
        {
            base.AddValidationRules();
            base.ValidationRules.AddRule(CommonRules.NotNull, Columns.UserId.ToString());
            base.ValidationRules.AddRule(EntityRules.ValidEntityRule, new EntityRulesArgs(Columns.UserId.ToString(), ValidateUser));
            //base.ValidationRules.AddRule(EntityRules.ForeignKeyRule<ApiUser, ApiUserProvider>, new EntityRulesArgs(Columns.UserId.ToString(), CompId, Columns.Id, ValidateIfNotNull));
            base.ValidationRules.AddRule(EntityRules.ForeignKeyRule<ApiFunctionHeader, ApiFunctionHeaderProvider>, new EntityRulesArgs(Columns.FunctionId.ToString(), CompId, Columns.Id, ValidateIfNotNull));
        }

        public override string CompId { get => Parent != null ? Parent.CompId : base.CompId; set => base.CompId = value; }

        public override long? UserId { get => Parent != null ? Parent.Id : base.UserId; set => base.UserId = value; }

        public override TransactionManager TransMan { get => Parent != null ? Parent.TransMan : base.TransMan; set => base.TransMan = value; }
        #endregion Overrides

        #region Properties
        public virtual EntityList<ApiUserFunctionSchema> SchemaList { get => LoadSchemaList(); }

        public virtual EntityList<ApiUserFunctionComp> CompanyList { get => LoadCompanyList(); }

        public ApiUser Parent
        {
            get => _parent;
            set => ParentEntity = _parent = value;
        }

        public virtual ApiFunctionHeader FunctionInfo { get => LoadFunctionInfo(); }

        [Bindable(true), Description]
        public DateTime? LastAccess { get => RequestInfo == null ? null : RequestInfo.LastAccessTime; }

        public string FunctionName { get => FunctionInfo?.Name; }

        private ApiUserFunctionAccess RequestInfo { get => LoadLastAccessInfo(); }

        internal bool SkipLookupValidation { get; set; }
        #endregion Properties

        #region Fields
        private ApiUser _parent;
        private ApiUserFunctionAccess _requestInfo;
        private ApiFunctionHeader _functionInfo;
        private EntityList<ApiUserFunctionSchema> _schemaList;
        private EntityList<ApiUserFunctionComp> _companyList;
        private static Semaphore _processor;
        #endregion Fields
    }
}
