#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.GeneralLedger;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.GeneralLedger.Controllers
{
    public class ApiGlTransactionController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "year/{fiscalyear:range(0,32767)}/transaction/{id?}", typeof(TransactionHeader))]
        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader))]
        public async Task<IHttpActionResult> Get(string id = null, short? fiscalYear = null)
        {
            return Ok(await this.Load(id, fiscalYear));
        }

        [ApiRoute(FunctionID, 2f, "year/{fiscalyear:range(0,32767)}/transaction/{id?}", typeof(TransactionHeader))]
        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null, short? fiscalYear = null)
        {
            return Ok(await ProcessEditRequest(false, body, id, fiscalYear));
        }

        [ApiRoute(FunctionID, 2f, "year/{fiscalyear:range(0,32767)}/transaction/{id?}", typeof(TransactionHeader))]
        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null, short? fiscalYear = null)
        {
            return Ok(await ProcessEditRequest(true, body, id, fiscalYear));
        }

        [ApiRoute(FunctionID, 2f, "year/{fiscalyear:range(0,32767)}/transaction/{id?}", typeof(TransactionHeader))]
        [ApiRoute(FunctionID, 2f, "transaction/{id}", typeof(TransactionHeader))]
        public async Task Delete(string id, short? fiscalYear = null)
        {
            await this.MarkToDelete(id, fiscalYear);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TransactionBase.Columns.TransDate.ToString(), (entity) => entity.SetFiscalPeriodYearFromDate(entity.TransDate));
            PropertyDictionary.Add(TransactionBase.Columns.AcctId.ToString(), (entity) => { entity.AllocateYn = IsAcctAllocated(entity.AcctId); });
        }

        protected virtual async Task<EntityList<TransactionHeader>> Load(string id, short? year)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrWhiteSpace(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.TransId.ToString(), false))))
            {
                var builder = new SqlFilterBuilder<TransactionBase.Columns>();
                if (year.GetValueOrDefault() == 0)
                    builder.AppendEquals(TransactionBase.Columns.FiscalYear, Account.AccountInfo(this.CompId).CurrentFiscalYear.ToString());
                else
                    builder.AppendEquals(TransactionBase.Columns.FiscalYear, year.ToString());

                if (string.IsNullOrWhiteSpace(id))
                    Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                else
                {
                    builder.AppendEquals(TransactionBase.Columns.TransId, id);
                    var list = new ApiTransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<TransactionHeader> Find(Guid id, short? year)
        {
            var entity = Provider.Items.Find(x => x.TransId.Equals(id) && (year.GetValueOrDefault() == 0 || x.FiscalYear == year.Value));

            if (entity == null)
            {
                var list = await Load(id.ToString(), year);
                entity = Provider.Items.Find(x => x.TransId.Equals(id) && (year.GetValueOrDefault() == 0 || x.FiscalYear == year.Value));
            }

            return entity;
        }

        protected virtual async Task<List<TransactionHeader>> ProcessEditRequest(bool isCreate, dynamic body, string id = null, short? year = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Transaction ID is provided along with more than one record.");

            var entityList = new List<TransactionHeader>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id, year);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id, short? year)
        {
            Guid code = Guid.Empty;
            Guid.TryParse(id, out code);

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || string.IsNullOrWhiteSpace(bodyItem.TransId))
            {
                if (code != Guid.Empty)
                    bodyItem.TransId = code.ToString();
            }
            else
                code = Guid.Parse(bodyItem.TransId);

            var entity = await this.Find(code, year);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CreateNew(year, bodyItem);
            }           
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction '{0}' could not be found.", code));
            
            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            LastTransaction = entity;
            if (!UserBalanceInfo.ContainsKey(entity.UserId))
            {
                UserBalanceInfo.Add(entity.UserId, 0M);
            }
            UserBalanceInfo[entity.UserId] += entity.DebitAmount - entity.CreditAmount;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id, short? fiscalYear)
        {
            var entity = await this.Find(Guid.Parse(id), fiscalYear);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transaction '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
        }

        protected virtual void AcctIdChanged(TransactionHeader header)
        {
            header.ExchRate = TRAVERSE.Business.Sys2.Currency.GetExchangeRate(header.CurrencyId, header.TransDate);
            if (header.IsNew)
                SetBalanceAmount(header);

            header.AllocateYn = IsAcctAllocated(header.AcctId);
        }

        protected virtual bool IsAcctAllocated(string acctID)
        {
            var builder = new SqlFilterBuilder<AllocationHeaderBase.Columns>();
            builder.AppendEquals(AllocationHeaderBase.Columns.AcctId, acctID);
            return new AllocationHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty)).Count > 0;
        }

        protected virtual TransactionHeader CreateNew(short? year, dynamic bodyItem)
        {
            var entity = new TransactionHeader(this.CompId);
            if (year.GetValueOrDefault() == 0)
                entity.FiscalYear = Account.AccountInfo(this.CompId).CurrentFiscalYear;
            else
                entity.FiscalYear = year.Value;

            if (!ApiUserSkipped.IsApiUserSkipped(bodyItem.BatchId) && !string.IsNullOrWhiteSpace(bodyItem.BatchId))
                entity.BatchId = bodyItem.BatchId;
            else
                entity.BatchId = TRAVERSE.Business.Batching.Batch.GetDefaultBatchId(Utility.FunctionIdTransaction, Utility.CanUseBatch(this.CompId), this.CompId, null);

            PopulateNewTransaction(entity);

            return entity;
        }

        protected virtual void PopulateNewTransaction(TransactionHeader entity)
        {
            if (LastTransaction != null)
            {
                entity.BatchId = LastTransaction.BatchId;
                if (!string.IsNullOrEmpty(LastTransaction.SourceCode))
                    entity.SourceCode = LastTransaction.SourceCode;

                entity.Description = LastTransaction.Description;
                entity.Reference = LastTransaction.Reference;
                entity.TransDate = LastTransaction.TransDate;
                entity.FiscalPeriod = LastTransaction.FiscalPeriod;
                entity.UserId = LastTransaction.UserId;

                SetBalanceAmount(entity);
            }
        }

        protected virtual void SetBalanceAmount(TransactionHeader entity)
        {
            if (entity == null || !UserBalanceInfo.ContainsKey(entity.UserId))
                return;

            decimal balAmt = UserBalanceInfo[entity.UserId];

            if (StringHelper.AreEqual(entity.CurrencyId, Utility.BaseCurrencyId, false))
                balAmt = Rounding.Round(balAmt * entity.ExchRate, Rounding.RoundingType.Amount, entity.CurrencyId, this.CompId);

            if (balAmt > 0M)
            {
                entity.CreditAmountFgn = balAmt;
                entity.DebitAmountFgn = 0M;
                return;
            }

            entity.DebitAmountFgn = -balAmt;
            entity.CreditAmountFgn = 0M;
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
            Action<Transaction> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Transaction);
        }
        #endregion Event Handlers

        #region Properties
        protected ApiTransactionHeaderProvider Provider { get; } = new ApiTransactionHeaderProvider();

        protected TransactionHeader LastTransaction { get; set; }

        private Dictionary<string, decimal> UserBalanceInfo { get; } = new Dictionary<string, decimal>();

        protected SortedDictionary<string, Action<Transaction>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Transaction>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "2EBAA4E5-C247-4081-96BA-6E701A26023F";
        #endregion Fields
    }
}

