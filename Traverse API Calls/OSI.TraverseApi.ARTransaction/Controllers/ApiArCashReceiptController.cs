#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.AccountsReceivable.Controllers
{
    public class ApiArCashReceiptController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "cashreceipt/{id:int?}", typeof(PaymentHeader))]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "cashreceipt/{id:int?}", typeof(PaymentHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "cashreceipt", typeof(PaymentHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "cashreceipt/{id:int}", typeof(PaymentHeader))]
        public async Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Header Property Changes
            PropertyDictionary.Add(PaymentHeaderBase.Columns.CustId.ToString(), CustomerPropertyChanged);
            PropertyDictionary.Add(PaymentHeaderBase.Columns.CurrencyId.ToString(), CurrencyPropertyChanged);
            PropertyDictionary.Add(PaymentHeaderBase.Columns.PmtMethodId.ToString(), (entity) => entity.SetPaymentMethodDefaults(entity.PmtMethodId));
            PropertyDictionary.Add(PaymentHeaderBase.Columns.PmtDate.ToString(), (entity) => entity.SetFiscalPeriodYearFromDate(entity.PmtDate.GetValueOrDefault(DateTime.Today)));

            //Detail Property Changes
            DetailPropertyDictionary.Add("Payment", (entity) =>
            {
                entity.Calculate();
                entity.PayHeader.UpdateDetailListTotals();
            });
            DetailPropertyDictionary.Add("Discount", (entity) => entity.Calculate());
            DetailPropertyDictionary.Add("InvoiceNumber", (entity) => entity.SetOrginalInvoiceId());
        }

        protected virtual async Task<EntityList<PaymentHeader>> Load(int? id)
        {
            if (Provider.Items.Count <= 0 || (id.HasValue && !Provider.Items.Exists(i => id == i.RcptHeaderId)))
            {
                if (!id.HasValue)
                    await Provider.Load<PaymentHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<PaymentHeader.Columns>();
                    builder.AppendEquals(PaymentHeader.Columns.RcptHeaderId, id.ToString());
                    var list = new PaymentHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<PaymentHeader> Find(int id)
        {
            var list = await Load(id);
            return list.Find(x => x.RcptHeaderId == id);
        }

        protected virtual async Task<List<PaymentHeader>> ProcessEditRequest(bool isCreate, dynamic body, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Cash Receipt is provided along with more than one record.");

            var entityList = new List<PaymentHeader>();
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

        protected virtual async Task<PaymentHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
        {
            int code = ApiUserSkipped.IsApiUserSkipped(bodyItem.RcptHeaderId) ? id.GetValueOrDefault() : (int)bodyItem.RcptHeaderId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new PaymentHeader(this.CompId);
                entity.SetDefaults();
                entity.PmtDate = DateTime.Today;
                entity.SetFiscalPeriodYearFromDate(entity.PmtDate.Value);
                entity.SetExchangeRateBase();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Cash Receipt '{0}' could not be found.", code));
            else
            {
                //Clear out all payments so that we can recreate them
                for (int i = entity.DetailList.Count - 1; i >= 0; i--)
                    entity.DetailList.RemoveAt(i);
                entity.CalculateTotals();
            }

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (entity.RemainingAmount > 0)
            {
                var detail = new PaymentDetailExt(entity);
                detail.SetDefaults();
                detail.SetOrginalInvoiceId();
                entity.DetailListExt.Add(detail);
                detail.Payment = new decimal?(entity.RemainingAmount);
                detail.Calculate();
                entity.UpdateDetailListTotals();
            }

            return entity;
        }

        protected virtual async Task MarkToDelete(int id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Cash Receipt '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailListExt")
                return this.CreatePayment((PaymentHeader)args.ParentObject, args.ItemModel);

            return null;
        }

        private PaymentDetailExt CreatePayment(PaymentHeader paymentHeader, dynamic bodyItem)
        {
            string code = (ApiUserSkipped.IsApiUserSkipped(bodyItem.InvoiceNumber) || string.IsNullOrWhiteSpace(bodyItem.InvoiceNumber)) ?
                    ConfigurationValue.GetRule<string>(AppId.AR, ConfigurationValue.OnAcctInvc, this.CompId) :
                    bodyItem.InvoiceNumber;

            var entity = paymentHeader.DetailListExt.Find(x => StringHelper.AreEqual(x.InvoiceNumber, code, false) && x.NetDueFgn > 0 && x.InvoiceCurrencyId == paymentHeader.CurrencyId);

            if (entity == null)
            {
                entity = new PaymentDetailExt(paymentHeader);
                entity.InvoiceNumber = code;
                entity.SetDefaults();
                entity.SetOrginalInvoiceId();
                paymentHeader.DetailListExt.Add(entity);
                entity.Payment = paymentHeader.RemainingAmount;
                entity.Calculate();
                paymentHeader.UpdateDetailListTotals();
            }
            else
            {
                paymentHeader.ApplyPayment(paymentHeader.RemainingAmount, entity);
                paymentHeader.UpdateDetailListTotals();
            }

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void CurrencyPropertyChanged(PaymentHeader entity)
        {
            entity.SetExchangeRateBase();
            PaymentDetailExt.RecalculateAll(entity);
        }

        protected virtual void CustomerPropertyChanged(PaymentHeader entity)
        {
            entity.SetCustomerDefaults(entity.CustId);

            if (entity.IsNew)
            {
                entity.InvalidateList();
                OpenInvoiceProvider.InvalidateList();
                entity.UpdateDetailListTotals();
            }
            entity.SetPaymentMethodDefaults(entity.PmtMethodId);
            entity.SetExchangeRateBase();
        }
        #endregion Helper Methods

        #region Handler Methods
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<PaymentHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PaymentHeader);
        }

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<PaymentDetailExt> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PaymentDetailExt);
        }
        #endregion Handler Methods

        #region Properties
        protected PaymentHeaderProvider Provider { get; } = new PaymentHeaderProvider();

        protected SortedDictionary<string, Action<PaymentHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PaymentHeader>>();

        protected SortedDictionary<string, Action<PaymentDetailExt>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<PaymentDetailExt>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "0CD2DFD9-6635-49E6-9865-A3DAEFCB5E30";
        public const string DetailFunctionID = "be83f12d-c193-4491-94f6-c680fad279f7";
        #endregion Fields
    }
}
