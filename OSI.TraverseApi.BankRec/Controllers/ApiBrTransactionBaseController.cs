#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Business.BankRec;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.BankRec.Controllers
{
    public class ApiBrTransactionBaseController : ApiControllerBase
    {
        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() 
        {
            this.EntityPropertyDictionary.Add(JournalHeaderBase.Columns.TransId.ToString(),ProcessTransId);
        }
        #endregion Overrides

        protected virtual async Task<EntityList<JournalHeader>> Load (string bankId, string transId, TransactionType type) 
        {
            SqlFilterBuilder<JournalHeaderBase.Columns> builder = new SqlFilterBuilder<JournalHeaderBase.Columns>();
            builder.AppendEquals(JournalHeaderBase.Columns.TransType, ((short?)type).ToString());
            builder.AppendEquals(JournalHeaderBase.Columns.BankId, bankId);

            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(bankId) && !Provider.Items.Exists(i => StringHelper.AreEqual(bankId, i.BankId, false))))
            {
                if (string.IsNullOrEmpty(transId))
                    Provider.Load(this.CompId, new FilterCriteria(builder.ToString(),string.Empty));
                else
                {                    
                    builder.AppendEquals(JournalHeaderBase.Columns.TransId, transId);                    
                    var list = new JournalHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<JournalHeader> Find(string bankId, string transId, TransactionType type)
        {
            var list = await Load(bankId, transId, type);
            return list.Find(x => x.TransId == transId);
        }

        protected virtual async Task<List<JournalHeader>> ProcessEditRequest(bool isCreate, dynamic body, string bankId, string transId, TransactionType type)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && transId != null)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new List<JournalHeader>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, bankId, transId, type);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<JournalHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string bankId, string transId, TransactionType type)
        {
            string code = transId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || bodyItem.TransId == null)
                bodyItem.TransId = code;
            else
                code = bodyItem.TransId;

            var entity = await this.Find(bankId, code, type);

            if (isCreate)
            {
                if (entity != null )
                    return entity;

                entity = new JournalHeader();
                entity.SourceId = bodyItem.SourceId;
                entity.BankId = bankId;
                entity.TransId = entity.GetNextTransId();
                entity.TransactionType = type;

                if (type == TransactionType.Adjustment)
                {
                    var recurHeader = this.GetRecurrAdjList(entity.BankId)?.Find(RecurHeaderBase.Columns.SourceId, entity.SourceId);
                    if(recurHeader != null)
                        entity.AmountFgn = new decimal?(recurHeader.Amount.Value);

                    if (ApiUserSkipped.IsApiUserSkipped(bodyItem.DetailRecords))
                        CreateJournalDetail(entity);
                }

                entity.SetExhangeRateDefaults();
                entity.SetExchangeRateBase();
                entity.Recalcuate();               
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction Number {0} could not be found on Bank account ID '{1}'.", code, bankId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string bankId, string transId, TransactionType type)
        {
            var entity = await this.Find(bankId, transId, type);

            if (entity == null)
                throw new InvalidValueException(string.Format("Transaction Number {0} could not be found on Bank account ID '{1}'", transId, bankId));

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
                        return this.CreateDetail(header);
                    else
                        return this.UpdateJournalDetail(header, args.ItemModel);
                }
            }
            return null;
        }

        protected virtual void ProcessTransId(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (((JournalHeader)e.Entity).TransId != null)
                    e.Handled = true;
            }
        }

        protected virtual EntityList<RecurHeader> GetRecurrAdjList(string bankId)
        {
            if (this._recurradjlist == null || this._recurradjlist.Count == 0)
            {
                RecurHeaderProvider provider = new RecurHeaderProvider();
                SqlFilterBuilder<RecurHeaderBase.Columns> sqlFilterBuilder = new SqlFilterBuilder<RecurHeaderBase.Columns>();
                sqlFilterBuilder.AppendEquals(RecurHeaderBase.Columns.BankId, bankId);
                this._recurradjlist = provider.Load(this.CompId, new FilterCriteria(sqlFilterBuilder.ToString(), "BankId"));
            }
            return this._recurradjlist;
        }

        #region Detail Method
        protected virtual JournalDetail UpdateJournalDetail(JournalHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry Number is required.");

            this.FilterEntityList(parent.DetailRecords, ApiBrAdjustmentDetailController.FunctionID);
            JournalDetail entity = parent?.DetailRecords?.Find(JournalDetailBase.Columns.EntryNum, (int)bodyItem.EntryNum);
            parent.Recalcuate();
            if (entity == null)
                throw new InvalidValueException(string.Format("Transaction Number {0} could not be found on Bank account ID'{1}'", bodyItem.TransId, parent.BankId));

            entity.SetDefaultAmounts();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;


            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual JournalDetail CreateDetail(JournalHeader hdr)
        {
            JournalDetail journalDetail = hdr.DetailRecords.AddNew();
          
            journalDetail.SetDefaultAmounts();
            {
                journalDetail.Description = hdr.Description;
                journalDetail.Reference = hdr.Reference;
            }
            journalDetail.TransId = hdr.TransId;
            hdr.Recalcuate();
            return journalDetail;
        }

        protected virtual void CreateJournalDetail(JournalHeader hdr)
        {
            
            var recurHeader = this.GetRecurrAdjList(hdr.BankId)?.Find(RecurHeaderBase.Columns.SourceId, hdr.SourceId);
            hdr.AmountFgn = new decimal?(recurHeader.Amount.Value);
            hdr.CurrencyId = recurHeader.CurrencyId;
            if(recurHeader.Reference != null)
                hdr.Reference =  recurHeader.Reference;
            if (recurHeader.Description != null)
                hdr.Description = recurHeader.Description;

            using (IEnumerator<RecurDetail> enumerator2 = recurHeader.DetailRecords.GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    JournalDetail journalDetail = hdr.DetailRecords.AddNew();
                    journalDetail.SetDefaultAmounts();
                    RecurDetail recurDetail = enumerator2.Current;
                    journalDetail.GLAcct = recurDetail.GLAcct;
                    journalDetail.Description = recurDetail.Description;
                    journalDetail.Reference = recurDetail.Reference;
                    journalDetail.DebitAmt = recurDetail.DebitAmt;
                    journalDetail.CreditAmt = recurDetail.CreditAmt;
                    journalDetail.DebitAmtFgn = recurDetail.DebitAmt;
                    journalDetail.CreditAmtFgn = recurDetail.CreditAmt;
                }
            }
            hdr.Recalcuate();
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as JournalDetail;
            entity.PropertyChanged -= Detail_PropertyChanged;
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
        #endregion Detail Method
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

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<JournalDetail> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as JournalDetail);
        }
        #endregion Event Handlers

        #region Properties
        private JournalHeaderProvider Provider { get; } = new JournalHeaderProvider();
        protected SortedDictionary<string, Action<JournalHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<JournalHeader>>();
        protected SortedDictionary<string, Action<JournalDetail>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<JournalDetail>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private EntityList<RecurHeader> _recurradjlist;
        #endregion Fields
    }
}
