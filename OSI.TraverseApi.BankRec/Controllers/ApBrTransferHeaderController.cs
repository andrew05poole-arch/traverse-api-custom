#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.BankRec;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Core;
using TraverseApi;
using System.Linq;
using System.Net.Http;
#endregion Using Directives

namespace OSI.TraverseApi.BankRec.Controllers
{
    public class ApBrTransferHeaderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bank/{bankid}/transfer/{id?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Get(string bankId = null, string id = null)
        {
            return Ok(await this.Load(bankId, id));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/transfer/{id?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string bankId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, bankId, id));
        }

        [ApiRoute(FunctionID, 2f, "transfer", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/transfer/{id}", typeof(JournalHeader))]
        public async Task Delete(string bankId, string id)
        {
            await this.MarkToDelete(bankId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.PropertyDictionary.Add(JournalHeaderBase.Columns.CurrencyId.ToString(), this.OnCurrencyPropertyChanged);
            this.PropertyDictionary.Add(JournalHeaderBase.Columns.TransDate.ToString(), this.OnTransDatePropertyChanged);
        }
        #endregion

        protected virtual async Task<EntityList<JournalHeader>> Load(string bankId, string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(bankId) && !Provider.Items.Exists(i => StringHelper.AreEqual(bankId, i.BankId, false))))
            {
                if (string.IsNullOrEmpty(id))
                {
                    var builder = new SqlFilterBuilder<JournalHeaderBase.Columns>();
                    builder.AppendEquals(JournalHeaderBase.Columns.BankId, bankId);
                    builder.AppendEquals(JournalHeaderBase.Columns.TransType, "-3");
                    await Provider.Load<JournalHeader>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);
                }
                else
                {
                    var builder = new SqlFilterBuilder<JournalHeaderBase.Columns>();
                    builder.AppendEquals(JournalHeaderBase.Columns.BankId, bankId);
                    builder.AppendEquals(JournalHeaderBase.Columns.TransId, id);
                    builder.AppendEquals(JournalHeaderBase.Columns.TransType, "-3");
                    var list = new JournalHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }
            return Provider.Items;
        }

        protected virtual async Task<JournalHeader> Find(string bankId, string id)
        {
            var list = await this.Load(bankId, id);
            return list?.Find(x => x.TransId == id && x.BankId == bankId);
        }

        protected virtual async Task<List<JournalHeader>> ProcessEditRequest(bool isCreate, dynamic body, string bankId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Trans ID is provided along with more than one record.");

            var entityList = new List<JournalHeader>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, bankId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<JournalHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string bankId, string id)
        {
            string code = id;
            string bankCode = bankId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || bodyItem.TransId == null)
                bodyItem.TransId = code;
            else
                code = bodyItem.TransId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.BankId) || bodyItem.BankId == null)
                bodyItem.BankId = bankCode;
            else
                bankCode = bodyItem.BankId;

            var entity = await this.Find(bankCode, code);

            if (isCreate)
            {
                if (entity != null && !string.IsNullOrEmpty(code))
                    return entity;

                entity = new JournalHeader(this.CompId);
                entity.TransType = -3;
                entity.SetExhangeRateDefaults();
                entity.TransId = entity.GetNextTransId();
            }
            else if (entity == null)
                throw new NothingToProcessException(string.Format("Trans ID '{0}' for Bank ID '{1}' does not exist.", code, bankId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);            
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string bankId, string id)
        {
            var entity = await this.Find(bankId, id);
            if (entity == null)
                throw new NothingToProcessException(string.Format("Trans ID '{1}' for Bank ID '{0}' does not exist.", bankId, id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.ParentObject is JournalHeader header)
            {
                if (StringHelper.AreEqual(args.PropertyName, "DetailRecords", false))
                {
                    if (header.IsNew)
                        return this.CreateJournalDetail(header);
                    else
                        return this.UpdateJournalDetail(header, args.ItemModel);
                }
            }
            return null;
        }

        protected virtual object UpdateJournalDetail(JournalHeader header, dynamic bodyItem)
        {
            this.FilterEntityList(header.DetailRecords, DetailFunctionId);

            if (header.DetailRecords.Count > 1)
                throw new Exception("Error Transfer Transaction are not allowed to have multiple details.");

            JournalDetail entity = (header.DetailRecords as EntityList<JournalDetail>).FirstOrDefault();
            if (entity == null)
                throw new InvalidValueException(string.Format("Detail for Transaction ID '{0}' could not be found.", header.TransId));

            entity.SetDefaultAmounts();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual JournalDetail CreateJournalDetail(JournalHeader hdr)
        {
            JournalDetail journalDetail = hdr.DetailRecords.AddNew();
            if (hdr.DetailRecords.Count > 1)
                throw new Exception("Error Transfer Transaction are not allowed to have multiple details.");
            journalDetail.SetDefaultAmounts();
            {
                journalDetail.Description = hdr.Description;
                journalDetail.Reference = hdr.Reference;
            }
            journalDetail.TransId = hdr.TransId;
            return journalDetail;
        }

        protected virtual void CheckPeriodClosed(DateTime dateVal)
        {
            PeriodConversion fiscalPeriod = PeriodConversion.GetFiscalPeriod(dateVal);
            if (fiscalPeriod != null)
            {
                if (fiscalPeriod.ClosedBR.Value || fiscalPeriod.ClosedGL.Value)
                {
                    throw new InvalidValueException(string.Format("Period {0} is closed for fiscal year {1}",
                        fiscalPeriod.FiscalPeriod.ToString(), fiscalPeriod.FiscalYear.ToString()));
                }
            }
        }        

        protected virtual void OnTransDatePropertyChanged(JournalHeader entity)
        {
            this.CheckPeriodClosed(entity.TransDate.Value);
        }

        protected virtual void OnCurrencyPropertyChanged(JournalHeader entity)
        {
            entity.SetExchangeRateBase();
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
            Action<JournalHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as JournalHeader);
        }
        #endregion Event Handlers

        #region Properties
        protected SortedDictionary<string, Action<JournalHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<JournalHeader>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        protected JournalHeaderProvider Provider { get; } = new JournalHeaderProvider();        
        #endregion Properties

        #region Fields
        public const string FunctionID = "60135511-7903-49FC-9990-3D3B74B86C9E";

        public const string DetailFunctionId = "08db86c1-3b10-4ea9-a4a4-c3fc80d32d4e";
        #endregion
    }
}
