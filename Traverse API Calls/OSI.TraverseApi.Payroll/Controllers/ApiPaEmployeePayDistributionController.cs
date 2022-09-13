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
    public class ApiPaEmployeePayDistributionController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/paydistribution/{id:int?}", typeof(PayDistribution))]
        public async Task<IHttpActionResult> Get(string employeeId = null, int? id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/paydistribution/{id:int?}", typeof(PayDistribution))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/paydistribution", typeof(PayDistribution))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, null));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/paydistribution/{id:int}", typeof(PayDistribution))]
        public async Task Delete(string employeeId, int id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion

        protected virtual async Task<EntityList<PayDistribution>> Load(string employeeId, int? id)
        {
            var list = this.CurrentEmployee?.Detail?.PaymentDistributions;

            if (this.CurrentEmployee == null || this.CurrentEmployee.Detail.EmployeeId != employeeId)
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(this.Provider.Items);

                if (this.Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee ID '{0}' could not be found.", employeeId));

                this.CurrentEmployee = this.Provider.Items[0];

                list = CurrentEmployee.Detail.PaymentDistributions;
                await this.FilterEntityListAsync(list, FunctionID);
            }

            if (id.HasValue)
                return list.FindAll(PayDistribution.Columns.Id, id);

            return list;
        }

        protected virtual async Task<PayDistribution> Find(string employeeId, int id)
        {
            var list = await Load(employeeId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<PayDistribution>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Payment Distribution ID is provided along with more than one record.");

            var entityList = new List<PayDistribution>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, employeeId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            await ValidateEntityAsync(this.CurrentEmployee);
            
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<PayDistribution> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, int? id)
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

                entity = CurrentEmployee?.Detail?.PaymentDistributions?.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new NothingToProcessException(string.Format("Pay Distribution ID '{0}' for Employee '{1}' does not exist.", code, employeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string employeeId, int id)
        {
            var payDistribution = await this.Find(employeeId, id);

            if (payDistribution == null)
                throw new NothingToProcessException(string.Format("Pay Distribution ID '{0}' for Employee '{1}' does not exist.", id, employeeId));

            this.CurrentEmployee.Detail?.PaymentDistributions.Remove(payDistribution);
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
            Action<PayDistribution> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PayDistribution);
        }
        #endregion Event Handlers

        #region Properties
        protected EmployeeProvider Provider { get; } = new EmployeeProvider();

        protected Employee CurrentEmployee { get; set; }

        protected SortedDictionary<string, Action<PayDistribution>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PayDistribution>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "4b7f1656-ae4e-40a1-88ff-8174bf592544";
        #endregion Fields
    }
}