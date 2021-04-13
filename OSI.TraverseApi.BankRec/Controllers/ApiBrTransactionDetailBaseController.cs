#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Business.BankRec;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.BankRec.Controllers
{
    public class ApiBrTransactionDetailBaseController : ApiControllerBase
    {
        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates(){}
        #endregion Overrides

        protected virtual async Task<EntityList<JournalDetail>> Load(string bankId, string transId,int? entryNumber, TransactionType type)
        {
            SqlFilterBuilder<JournalHeaderBase.Columns> builder = new SqlFilterBuilder<JournalHeaderBase.Columns>();
            builder.AppendEquals(JournalHeaderBase.Columns.TransType, ((short?)type).ToString());
            builder.AppendEquals(JournalHeaderBase.Columns.BankId, bankId);
            builder.AppendEquals(JournalHeaderBase.Columns.TransId, transId);

            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(bankId) && !Provider.Items.Exists(i => StringHelper.AreEqual(bankId, i.BankId, false))))
            {
                if (!entryNumber.HasValue)
                {
                    Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    await this.FilterEntityListAsync(Provider.Items);
                    return Provider.Items[0]?.DetailRecords;
                }
                else
                {
                    var list = new JournalHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                    await this.FilterEntityListAsync(Provider.Items);
                    return Provider.Items[0]?.DetailRecords?.FindAll(x => x.EntryNum == entryNumber);
                }       
            }

            return Provider.Items[0]?.DetailRecords;
        }

        protected virtual async Task<JournalDetail> Find(string bankId, string transId, int? entryNumber, TransactionType type)
        {
            var list = await Load(bankId, transId, entryNumber, type);
            return list.Find(x => x.EntryNum == entryNumber);
        }

        protected virtual async Task<EntityList<JournalDetail>> ProcessEditRequest(bool isCreate, dynamic body, string bankId, string transId, int? entryNumber, TransactionType type)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && entryNumber.HasValue)
                throw new InvalidValueException("Call is ambiguous. Entry Number is provided along with more than one record.");

            var entityList = new EntityList<JournalDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, bankId, transId, entryNumber, type);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<JournalDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string bankId, string transId, int? entryNumber, TransactionType type)
        {
            int code = entryNumber.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum) || bodyItem.EntryNum == null)
                bodyItem.EntryNum = code;
            else
                code = Convert.ToInt32(bodyItem.EntryNum);

            var entity = await this.Find(bankId, transId, code, type);
           
            if (entity == null)
                throw new InvalidValueException(string.Format("Entry Number '{0}' could not be found on Transaction ID '{1}'.",code , transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
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
            Action<JournalDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as JournalDetail);
        }
        #endregion Event Handlers

        #region Properties
        private JournalHeaderProvider Provider { get; } = new JournalHeaderProvider();
        protected SortedDictionary<string, Action<JournalDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<JournalDetail>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties
    }
}
