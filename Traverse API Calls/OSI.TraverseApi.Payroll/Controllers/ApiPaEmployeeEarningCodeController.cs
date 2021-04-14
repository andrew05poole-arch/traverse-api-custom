#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TraverseApi;
using TBC = TRAVERSE.Business.CompanySetup;
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaEmployeeEarningCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/earningcode/{id?}", typeof(ValidEarnCode))]
        public async Task<IHttpActionResult> Get(string employeeId = null, string id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/earningcode/{id?}", typeof(ValidEarnCode))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/earningcode/{id?}", typeof(ValidEarnCode))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, null));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/earningcode/{id}", typeof(ValidEarnCode))]
        public async Task Delete(string employeeId, string id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<ValidEarnCode>> Load(string employeeId, string id)
        {
            var list = CurrentEmployee?.Detail?.ValidEarningCodes;

            if (CurrentEmployee == null || CurrentEmployee.Detail.EmployeeId != employeeId)
            {
                var builder = new SqlFilterBuilder<TBC.EmployeeBase.Columns>();
                builder.AppendEquals(TBC.EmployeeBase.Columns.EmployeeId, employeeId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(this.Provider.Items);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee ID '{0}' could not be found.", employeeId));

                CurrentEmployee = Provider.Items[0];

                list = CurrentEmployee.Detail.ValidEarningCodes;
                await this.FilterEntityListAsync(list, FunctionID);
            }

            if (id != null)
                return list.FindAll(ValidEarnCodeBase.Columns.EarnCodeId, id);

            return list;
        }

        protected virtual async Task<ValidEarnCode> Find(string employeeId, string id)
        {
            var list = await Load(employeeId, id);
            return list?.Find(x => x.EarnCodeId == id);
        }

        protected virtual async Task<List<ValidEarnCode>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Earning Code ID is provided along with more than one record.");

            var entityList = new List<ValidEarnCode>();
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

        protected virtual async Task<ValidEarnCode> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EarnCodeId) || bodyItem.EarnCodeId == null)
                bodyItem.EarnCodeId = code;
            else
                code = bodyItem.EarnCodeId;

            var entity = await this.Find(employeeId, code);

            if (isCreate)
            {
                if (entity != null && code != null)
                    return entity;

                entity = CurrentEmployee?.Detail?.ValidEarningCodes?.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Earning Code ID '{0}' could not be found on Employee {1}", code, employeeId));

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
                throw new NothingToProcessException(string.Format("Contact Address ID '{0}' could not be found.", id));

            this.Provider.Items[0]?.Detail?.ValidEarningCodes?.Remove(entity);
            this.Provider.Items[0]?.MarkAsDirty();
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
            Action<ValidEarnCode> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ValidEarnCode);
        }
        #endregion Event Handlers

        #region Properties
        private EmployeeProvider Provider { get; } = new EmployeeProvider();

        protected Employee CurrentEmployee { get; set; }

        protected SortedDictionary<string, Action<ValidEarnCode>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ValidEarnCode>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "7B347195-0F5D-4C31-AE93-870003CADD1F";
        #endregion Fields
    }
}
