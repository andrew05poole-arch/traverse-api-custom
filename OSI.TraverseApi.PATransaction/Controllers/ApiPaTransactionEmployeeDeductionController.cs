#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaTransactionEmployeeDeductionController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{employeeid}/deduction/{id:int?}", typeof(TransDeduct))]
        public async Task<IHttpActionResult> Get(string employeeId, int? id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{employeeid}/deduction/{id:int?}", typeof(TransDeduct))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{employeeid}/deduction", typeof(TransDeduct))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, null));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{employeeid}/deduction/{id:int}", typeof(TransDeduct))]
        public async Task Delete(string employeeId, int id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion Web Methods

        #region Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion

        protected virtual async Task<EntityList<TransDeduct>> Load(string employeeId, int? id)
        {
            var list = CurrentTransaction?.TransDeductList;

            if (this.CurrentTransaction == null || !StringHelper.AreEqual(CurrentTransaction.EmployeeId, employeeId, false))
            {
                var builder = new SqlFilterBuilder<TransDeductBase.Columns>();
                builder.AppendEquals(TransDeductBase.Columns.EmployeeId, employeeId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPaTransactionEmployeeDeductionController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction Deduction ID '{0}' could not be found.", id));

                this.CurrentTransaction = this.Provider.Items[0];

                list = this.CurrentTransaction.TransDeductList;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(TransEarn.Columns.Id, id);

            return list;
        }

        protected virtual async Task<TransDeduct> Find(string employeeId, int id)
        {
            var list = await this.Load(employeeId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<TransDeduct>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Transaction Deduction ID is provided along with more than one record.");

            var entityList = new List<TransDeduct>();
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

        protected virtual async Task<TransDeduct> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, int? id)
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

                entity = this.CurrentTransaction?.TransDeductList?.AddNew();
                entity.SetDefaultValues();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction Deduction ID {0} could not be found on Employee ID '{1}'.", code, employeeId));

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
                throw new NothingToProcessException(string.Format("Deduction ID '{0}' for Employee '{1}' does not exist.", id, employeeId));
            
            this.CurrentTransaction.TransDeductList?.Remove(entity);
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
            Action<TransDeduct> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransDeduct);
        }
        #endregion Event Handlers

        #region Properties
        private Transaction CurrentTransaction { get; set; }
        private TransactionProvider Provider { get; } = new TransactionProvider();
        protected SortedDictionary<string, Action<TransDeduct>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransDeduct>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "3DC96758-A62B-4C56-9585-3C3D73BC0850";
        #endregion Fields
    }
}
