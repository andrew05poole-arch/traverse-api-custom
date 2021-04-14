using TRAVERSE.Business;
using TRAVERSE.Core;

namespace OSI.TraverseApi.Business
{
    public class ApiUserCopyProcess : ProcessBase
    {
        #region Constructors
        public ApiUserCopyProcess()
            : this(ApiUtility.CurrentApiDb)
        { }

        public ApiUserCopyProcess(string compId)
            : this(compId, ProcessBase.GenerateProcessId())
        { }

        public ApiUserCopyProcess(string compId, string processId)
            : base(compId, processId)
        { }
        #endregion Constructors

        #region Methods
        protected virtual void ValidateUserInfo()
        {
            this.RaiseStatus("Validating parameters");
            if (UserTo == null)
                throw new InvalidValueException("No user to copy to was provided");

            if ((CopyName || CopyNotes || CopyFunctionList || CopyCompanyList || CopyCompanyFilter || CopyEntitySchema || CopyCustomFieldSchema) && UserFrom == null)
                throw new InvalidValueException("No user to copy from was provided");

            var provider = new ApiUserProvider();
            var builder = new SqlFilterBuilder<ApiUserBase.Columns>();
            builder.AppendEquals(ApiUserBase.Columns.EmailAddress, UserTo.EmailAddress);

            var criteria = new FilterCriteria(builder.ToString(), "");
            criteria.RecordCount = 1;

            provider.Load(CompId, criteria);
            if (provider.Count > 0)
                throw new InvalidValueException("Email address already exists as a user. Email Addresses must be unique per account.");
        }

        protected virtual void CopyNameInfo()
        {
            if (!CopyName)
                return;

            this.RaiseStatus("Copying name");
            UserTo.Name = UserFrom.Name;
            UserTo.ExpirationDate = UserFrom.ExpirationDate;
        }

        protected virtual void CopyNotesInfo()
        {
            if (!CopyNotes)
                return;

            this.RaiseStatus("Copying notes");
            UserTo.AdditionalInfo = UserFrom.AdditionalInfo;
        }

        protected virtual void CopyFunctionInfo()
        {
            if (!CopyFunctionList)
                return;

            foreach (var fncFrom in UserFrom.FunctionList)
            {
                this.RaiseStatus(string.Format("Copying {0}", fncFrom.FunctionName));
                var fncTo = UserTo.FunctionList.AddNew();
                fncTo.FunctionId = fncFrom.FunctionId;
                fncTo.AccessExpireDate = fncFrom.AccessExpireDate;
                this.CopyCompanyInfo(fncFrom, fncTo);
                this.CopyEntitySchemaInfo(fncFrom, fncTo);
                this.CopyCustomFieldInfo(fncFrom, fncTo);
            }
        }

        protected virtual void CopyCompanyInfo(ApiUserFunction functionFrom, ApiUserFunction functionTo)
        {
            if (!this.CopyCompanyList)
                return;

            foreach (var compFrom in functionFrom.CompanyList)
            {
                var compTo = functionTo.CompanyList.AddNew();
                compTo.CompanyId = compFrom.CompanyId;
                compTo.Scope = compFrom.Scope;

                if (CopyCompanyFilter)
                {
                    compTo.DisplayFilter = compFrom.DisplayFilter;
                    compTo.Filter = compFrom.Filter;
                }
            }
        }

        protected virtual void CopyEntitySchemaInfo(ApiUserFunction functionFrom, ApiUserFunction functionTo)
        {
            if (!CopyEntitySchema)
                return;

            foreach (var schemaFrom in functionFrom.SchemaList)
            {
                if (schemaFrom.AccessFieldType == AccessFieldType.Custom)
                    continue;

                var schemaTo = functionTo.SchemaList.AddNew();
                schemaTo.ApiFieldName = schemaFrom.ApiFieldName;
                schemaTo.FieldType = schemaFrom.FieldType;
                schemaTo.FunctionSchemaId = schemaFrom.FunctionSchemaId;
                schemaTo.Hidden = schemaFrom.Hidden;
                schemaTo.DefaultValue = schemaFrom.DefaultValue;
                schemaTo.Notes = schemaFrom.Notes;
            }
        }

        protected virtual void CopyCustomFieldInfo(ApiUserFunction functionFrom, ApiUserFunction functionTo)
        {
            if (!CopyCustomFieldSchema)
                return;

            foreach (var schemaFrom in functionFrom.SchemaList)
            {
                if (schemaFrom.AccessFieldType == AccessFieldType.Entity)
                    continue;

                var schemaTo = functionTo.SchemaList.AddNew();
                schemaTo.ApiFieldName = schemaFrom.ApiFieldName;
                schemaTo.FieldType = schemaFrom.FieldType;
                schemaTo.CustomFieldName = schemaFrom.CustomFieldName;
                schemaTo.Hidden = schemaFrom.Hidden;
                schemaTo.DefaultValue = schemaFrom.DefaultValue;
                schemaTo.Notes = schemaFrom.Notes;
            }
        }
        #endregion Methods

        #region Overrides
        public override void Execute(Status status)
        {
            this.ValidateUserInfo();
            this.CopyNameInfo();
            this.CopyNotesInfo();
            this.CopyFunctionInfo();
        }
        #endregion Overrides

        #region Properties
        public ApiUser UserTo { get; set; }

        public ApiUser UserFrom { get; set; }

        public virtual bool CopyName { get; set; }

        public virtual bool CopyNotes { get; set; }

        public virtual bool CopyFunctionList { get; set; }

        public virtual bool CopyCompanyList { get; set; }

        public virtual bool CopyCompanyFilter { get; set; }

        public virtual bool CopyEntitySchema { get; set; }

        public virtual bool CopyCustomFieldSchema { get; set; }
        #endregion Properties
    }
}
