#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using SM = TRAVERSE.Business.CompanySetup;
#endregion Using Directives

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaEmployeeLeaveController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/leave/{id?}", typeof(Leave))]
        public async Task<IHttpActionResult> Get(string employeeId, string id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/leave/{id?}", typeof(Leave))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/leave/{id}", typeof(Leave))]
        public async Task Delete(string employeeId, string id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Leave employeeLeave)
            {
                if (StringHelper.AreEqual(args.FieldName, "RemainingHours", false))
                {
                    args.ActualValue = employeeLeave.GetLeaveBalance()?.Remaining;
                }
            }
        }
        #endregion Overrides

        protected virtual async Task<EntityList<Leave>> Load(string employeeId, string id)
        {
            var list = this.CurrentEmployee?.Detail?.LeaveCodes;

            if (this.CurrentEmployee == null || this.CurrentEmployee.Detail.EmployeeId != employeeId)
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(this.Provider.Items);

                if (this.Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee ID '{0}' could not be found.", employeeId));

                this.CurrentEmployee = this.Provider.Items[0];

                list = CurrentEmployee.Detail.LeaveCodes;
                await this.FilterEntityListAsync(list, FunctionID);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(LeaveBase.Columns.LeaveCodeId, id);

            return list;
        }

        protected virtual async Task<Leave> Find(string employeeId, string id)
        {
            var list = await Load(employeeId, id);
            return list?.Find(x => StringHelper.AreEqual(x.LeaveCodeId, id, false));
        }

        protected virtual async Task<List<Leave>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Leave Code ID is provided along with more than one record.");

            var entityList = new List<Leave>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, employeeId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Leave> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LeaveCodeId) || string.IsNullOrEmpty(bodyItem.LeaveCodeId))
                bodyItem.LeaveCodeId = code;
            else
                code = bodyItem.LeaveCodeId;

            var entity = await this.Find(employeeId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CurrentEmployee?.Detail?.LeaveCodes?.AddNew();
                entity.SetDefaults();
            }

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string employeeId, string id)
        {
            var entity = await this.Find(employeeId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Leave Code ID '{0}' for Employee ID '{1}' could not be found.", id, employeeId));

            this.CurrentEmployee.Detail?.LeaveCodes.Remove(entity);
            this.Provider.Update(this.CompId);
        }
        #endregion Helper Methods

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Leave> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Leave);
        }
        #endregion Event Handlers

        #region Properties
        private EmployeeProvider Provider { get; } = new EmployeeProvider();

        protected Employee CurrentEmployee { get; set; }

        protected SortedDictionary<string, Action<Leave>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Leave>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "5f388695-333c-4184-bd27-a56110dbecd3";
        #endregion Fields
    }
}
