#region Using Directives
using System;
using System.Collections.Generic;
using TRAVERSE.Business;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public class ApiCreateMultiFunction : ProcessBase
    {
        #region Constructors
        public ApiCreateMultiFunction()
            : this(string.Empty)
        { }

        public ApiCreateMultiFunction(string compId)
            : this(compId, ProcessBase.GenerateProcessId())
        { }

        public ApiCreateMultiFunction(string compId, string processId)
            : base(compId, processId)
        { }
        #endregion Constructors

        #region Protected Methods
        protected virtual byte CalculateScope()
        {
            byte scope = 0;
            if (this.AllowRead)
                scope |= 1;
            if (this.AllowEdit)
                scope |= 2;
            if (this.AllowCreate)
                scope |= 4;
            if (this.AllowDelete)
                scope |= 8;

            return scope;
        }
        #endregion Protected Methods

        #region Public Methods
        public override void Execute(Status status)
        {
            try
            {
                this.ProcessStatus = status;
                this.RaiseStatus("Preparing");

                this.UserInfo.SuppressEntityEvents = true;
                this.UserInfo.FunctionList.RaiseListChangedEvents = false;

                var originalList = new List<ApiUserFunction>(UserInfo.FunctionList);
                var userScope = this.CalculateScope();

                double count = this.FunctionList.Count;
                double n = 1;

                this.UserInfo.FunctionList.RaiseListChangedEvents = false;
                foreach (ApiFunctionHeader function in this.FunctionList)
                {
                    this.RaiseStatus(string.Format("Processing {0} of {1} ({2:0.00%})", n, count, n / count));

                    ApiUserFunction userFunction = originalList.Find(f => f.FunctionId == function.Id);
                    if (userFunction == null)
                    {
                        userFunction = new ApiUserFunction() { Parent = this.UserInfo, FunctionId = function.Id };
                        this.UserInfo.FunctionList.Add(userFunction);
                    }

                    userFunction.AccessExpireDate = ExpirationDate;
                    byte scope = (byte)(userScope & function.Scope.GetValueOrDefault());

                    foreach (string company in SelectedCompanyList)
                    {
                        ApiUserFunctionComp comp = userFunction.CompanyList.Find(ApiUserFunctionCompBase.Columns.CompanyId, company, true);
                        if (comp == null)
                        {
                            comp = new ApiUserFunctionComp() { Parent = userFunction, CompanyId = company };
                            userFunction.CompanyList.Add(comp);
                        }

                        comp.Scope = scope;
                    }

                    n++;
                }
                this.RaiseStatus("Finalizing");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this.UserInfo.SuppressEntityEvents = false;
                this.UserInfo.FunctionList.RaiseListChangedEvents = true;
            }
        }
        #endregion Public Methods

        #region Properties
        public virtual DateTime? ExpirationDate { get; set; }

        public virtual List<string> SelectedCompanyList { get; } = new List<string>();

        public virtual List<ApiFunctionHeader> FunctionList { get; } = new List<ApiFunctionHeader>();

        public virtual bool AllowRead { get; set; }

        public virtual bool AllowCreate { get; set; }

        public virtual bool AllowEdit { get; set; }

        public virtual bool AllowDelete { get; set; }

        public virtual ApiUser UserInfo { get; set; }
        #endregion Properties
    }
}
