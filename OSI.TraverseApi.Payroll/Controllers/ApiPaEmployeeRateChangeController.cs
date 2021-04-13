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
using SM = TRAVERSE.Business.CompanySetup;
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaEmployeeRateChangeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/ratechange/{id:int?}", typeof(PayChange))]
        public async Task<IHttpActionResult> Get(string employeeId, int? id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/ratechange/{id:int?}", typeof(PayChange))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/ratechange", typeof(PayChange))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, null));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/ratechange/{id:int}", typeof(PayChange))]
        public async Task Delete(string employeeId, int id)
        {
            await this.MarkToDelete(employeeId,id);
        }
        #endregion  Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<PayChange>> Load(string employeeId, int? id)
        {
            var list = this.CurrentEmployee?.Detail?.PayChanges;

            if (this.CurrentEmployee == null || this.CurrentEmployee.Detail.EmployeeId != employeeId)
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                this.Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(this.Provider.Items);

                if (this.Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee ID '{0}' could not be found.", employeeId));

                this.CurrentEmployee = this.Provider.Items[0];

                list =  this.CurrentEmployee.Detail?.PayChanges;
                await this.FilterEntityListAsync(list, FunctionID);
            }

            if (id.HasValue)
                return list.FindAll(PayChange.Columns.Id, id);

            return list;
        }

        protected virtual async Task<PayChange> Find(string employeeId, int id)
        {
            var list = await this.Load(employeeId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<PayChange>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Rate Change ID is provided along with more than one record.");

            var entityList = new List<PayChange>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, employeeId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);

                await this.ValidateEntityListAsync(entityList);
                this.Provider?.Update(this.CompId);
            }

            return entityList;
        }

        protected virtual async Task<PayChange> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(employeeId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentEmployee?.Detail?.PayChanges?.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Rate Change ID {0} could not be found on Employee ID '{1}'.", code, employeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        private async Task MarkToDelete(string employeeId, int id)
        {
            var entity = await this.Find(employeeId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Rate Change ID '{0}' for Employee '{1}' does not exist.", id, employeeId));

            this.CurrentEmployee.Detail?.PayChanges.Remove(entity);
            this.Provider.Update(CompId);

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
            Action<PayChange> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PayChange);
        }
        #endregion Event Handlers

        #region Properties
        private Employee CurrentEmployee { get; set; }
        private EmployeeProvider Provider { get; } = new EmployeeProvider();
        protected SortedDictionary<string, Action<PayChange>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PayChange>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "44b01e7a-1712-484d-af07-af6626a000d0";
        #endregion Fields
    }
}
