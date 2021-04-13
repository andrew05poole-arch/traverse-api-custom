#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.AccountsReceivable.Controllers
{
    public class ApiArPaymentsController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transId}/payment/{id:int?}", typeof(TransactionPayment))]
        public async Task<IHttpActionResult> Get(string transId, int? id = null)
        {
            return Ok(await Load(transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/payment/{id:int?}", typeof(TransactionPayment))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/payment/{id:int?}", typeof(TransactionPayment))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/payment/{id:int}", typeof(TransactionPayment))]
        public async Task Delete(string transId, int id)
        {
            await this.MarkToDelete(transId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TransactionPaymentBase.Columns.PmtMethodId.ToString(), PmtMethodIdPropertyChanged);
            PropertyDictionary.Add(TransactionPaymentBase.Columns.ExchRate.ToString(), (entity) => { if (!string.IsNullOrEmpty(entity.PmtMethodId)) entity.Calculate(); });
            PropertyDictionary.Add(TransactionPaymentBase.Columns.PmtAmtFgn.ToString(), (entity) =>
            {
                entity.Calculate();
                CurrentTransaction.CalculateTotals();
            });
            PropertyDictionary.Add(TransactionPaymentBase.Columns.CurrencyId.ToString(), (entity) => CurrentTransaction.CalculateTotals());
        }

        protected virtual async Task<EntityList<TransactionPayment>> Load(string transId, int? id)
        {
            var list = CurrentTransaction?.PaymentList;

            if (CurrentTransaction == null || !StringHelper.AreEqual(CurrentTransaction.TransId, transId, false))
            {
                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiArTransactionOrderController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction '{0}' could not be found.", id));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.PaymentList;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(TransactionPayment.Columns.PmtNum, id.Value);

            return list;
        }

        protected virtual async Task<TransactionPayment> Find(string transId, int id)
        {
            var list = await Load(transId, id);
            return list.Find(x => x.PmtNum == id);
        }

        protected virtual async Task<List<TransactionPayment>> ProcessEditRequest(bool isCreate, dynamic body, string transId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Payment Num is provided along with more than one record.");

            var entityList = new List<TransactionPayment>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionPayment> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, int? id)
        {
            int code = ApiUserSkipped.IsApiUserSkipped(bodyItem.PmtNum) ? id.GetValueOrDefault() : bodyItem.PmtNum;

            var entity = await this.Find(transId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CurrentTransaction.PaymentList.AddNew() as TransactionPayment;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Payment Num {0} could not be found on transaction '{1}'", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, int id)
        {
            var entity = await this.Find(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Payment Num {0} could not be found on transaction '{1}'", id, transId));

            CurrentTransaction.PaymentList.Remove(entity);
            CurrentTransaction.CalculateTotals();
            this.Provider.Update(this.CompId);
        }

        protected virtual void PmtMethodIdPropertyChanged(TransactionPayment entity)
        {
            entity.SetPaymentMethodDefaults(entity.PmtMethodId);

            if (entity.PaymentMethod != null
                && entity.PaymentMethod.PaymentType != PaymentType.Cash
                && entity.PaymentMethod.PaymentType != PaymentType.Check
                && entity.PaymentMethod.PaymentType != PaymentType.Other
                && entity.PaymentMethod.PaymentType != PaymentType.WriteOff)
                throw new InvalidValueException(string.Format("The selected payment method '{0}' is not supported via the API", entity.PmtMethodId));
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
            Action<TransactionPayment> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionPayment);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected TransactionHeader CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransactionPayment>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionPayment>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "8DFCFF2C-5EF5-457E-BF2B-4C79BE171AE8";
        #endregion Properties
    }
}
