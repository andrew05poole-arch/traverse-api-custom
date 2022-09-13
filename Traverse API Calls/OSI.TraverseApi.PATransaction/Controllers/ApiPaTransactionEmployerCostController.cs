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
#endregion Using Directives

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaTransactionEmployerCostController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{employeeid}/employercost/{id:int?}", typeof(TransCost))]
        public async Task<IHttpActionResult> Get(string employeeId, int? id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{employeeid}/employercost/{id:int?}", typeof(TransCost))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));

        }

        [ApiRoute(FunctionID, 2f, "transaction/{employeeid}/employercost", typeof(TransCost))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, null));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{employeeid}/employercost/{id:int}", typeof(TransCost))]
        public async Task Delete(string employeeId, int id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion Web Methods

        #region Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion

        protected virtual async Task<EntityList<TransCost>> Load(string employeeId, int? id)
        {
            var list = CurrentTransaction?.TransCostList;

            if (this.CurrentTransaction == null || !StringHelper.AreEqual(CurrentTransaction.EmployeeId, employeeId, false))
            {
                var builder = new SqlFilterBuilder<TransCostBase.Columns>();
                builder.AppendEquals(TransCostBase.Columns.EmployeeId, employeeId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPaTransactionEmployerCostController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction Employer Cost ID '{0}' could not be found.", id));

                this.CurrentTransaction = this.Provider.Items[0];

                list = this.CurrentTransaction.TransCostList;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(TransCost.Columns.Id, id);

            return list;
        }
        
        protected virtual async Task<TransCost> Find(string employeeId, int id)
        {
            var list = await this.Load(employeeId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<TransCost>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Transaction Employer Cost ID is provided along with more than one record.");

            var entityList = new List<TransCost>();
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

        protected virtual async Task<TransCost> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, int? id)
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

                entity = this.CurrentTransaction?.TransCostList?.AddNew();
                entity.SetDefaultValues();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction Employer Cost ID {0} could not be found on Employee ID '{1}'.", code, employeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string employeeId, int id)
        {
            var entity = await this.Find(employeeId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Employer Cost ID '{0}' for Employee '{1}' does not exist", employeeId, id));

            this.CurrentTransaction.TransCostList?.Remove(entity);
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
            Action<TransCost> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransCost);
        }
        #endregion Event Handlers

        #region Properties
        private Transaction CurrentTransaction { get; set; }
        private TransactionProvider Provider { get; } = new TransactionProvider();
        protected SortedDictionary<string, Action<TransCost>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransCost>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "EB6097FF-F323-42CA-9D3A-7401D55D6A58";
        #endregion Fields
    }
}
