#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Business.API;
using TRAVERSE.Business.Sys2;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.AccountsReceivable.Controllers
{
    public class ApiArPaymentMethodController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "paymentmethod/{id?}", typeof(PaymentMethod))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "paymentmethod/{id?}", typeof(PaymentMethod))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "paymentmethod/{id?}", typeof(PaymentMethod))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "paymentmethod/{id}", typeof(PaymentMethod))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(PaymentMethodBase.Columns.PmtType.ToString(), OnPaymentTypeUpdated);
            PropertyDictionary.Add(PaymentMethodBase.Columns.BankId.ToString(), OnBankIdUpdated);
            EntityPropertyDictionary.Add(PaymentMethodBase.Columns.BankId.ToString(), ProcessBankId);
        }

        protected virtual async Task<EntityList<PaymentMethod>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.PmtMethodId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<PaymentMethod>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<PaymentMethodBase.Columns>();
                    builder.AppendEquals(PaymentMethodBase.Columns.PmtMethodId, id);
                    var list = new PaymentMethodProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        private async Task<PaymentMethod> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.PmtMethodId, id, false));
        }

        protected virtual async Task<List<PaymentMethod>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Payment Method is provided along with more than one record.");

            var entityList = new List<PaymentMethod>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<PaymentMethod> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PmtMethodId) || string.IsNullOrWhiteSpace(bodyItem.PmtMethodId))
                bodyItem.PmtMethodId = code;
            else
                code = bodyItem.PmtMethodId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new PaymentMethod(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Payment Method '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (!Currency.IsBaseCurrency(entity.CurrencyId))
                entity.MobileEnabled = false;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Payment Method ID '{0}' could not be found.", id));
            
            this.Provider.Items.Remove(entity);
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
            Action<PaymentMethod> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PaymentMethod);
        }

        protected virtual void OnPaymentTypeUpdated(PaymentMethod entity)
        {
            if (entity.PaymentType == null || entity.PaymentType == PaymentType.WriteOff || entity.PaymentType == PaymentType.Other)
                entity.BankId = null;
            else
            {
                string userBankID = ConfigurationValueProvider.GetRule<string>("SM", "BankID", this.CompId);
                ApiUtility.TryGetApiUserDefault(this.CompId, AppId.AR, "BankId", ref userBankID);
                entity.BankId = userBankID;
            }
            entity.CustId = null;
        }

        protected virtual void OnBankIdUpdated(PaymentMethod entity)
        {
            if (!string.IsNullOrEmpty(entity.BankId))
            {
                entity.CustId = null;
                entity.GLAcctDebit = null;
            }
        }

        protected virtual void ProcessBankId(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (((PaymentMethod)e.Entity).PaymentType == PaymentType.WriteOff || ((PaymentMethod)e.Entity).PaymentType == PaymentType.Other)
                    e.Handled = true;
            }
        }
        #endregion Event Handlers

        #region Properties
        protected PaymentMethodProvider Provider { get; } = new PaymentMethodProvider();

        protected SortedDictionary<string, Action<PaymentMethod>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PaymentMethod>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "521D9FAB-F8BA-4F88-B9F9-C34F7D6B186B";
        #endregion Fields
    }
}
