#region Using Diretives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Batching;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TraverseApi;
using Task = System.Threading.Tasks.Task;
#endregion Using Diretives

namespace OSI.TraverseApi.ProjectCosting.Controllers
{
    public class ApiPcTransactionBatchCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transactionbatch/{id?}", typeof(TransactionBatch))]
        public async Task <IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "transactionbatch/{id?}", typeof(TransactionBatch))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transactionbatch/{id?}", typeof(TransactionBatch))]
        public async Task <IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transactionbatch/{id}", typeof(TransactionBatch))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            PropertyDictionary.Add(BatchBase.Columns.DefaultYn.ToString(), ProcessDefaultChange);
            PropertyDictionary.Add(BatchBase.Columns.Lock.ToString(), ProcessLockChange);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if(args.Entity is TransactionBatch batch)
            {
                if(StringHelper.AreEqual(args.FieldName,BatchBase.Columns.Lock.ToString(),false))
                {
                    args.ActualValue = batch.Lock;
                }
            }
        }

        protected virtual async Task<EntityList<TransactionBatch>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.BatchId, false))))
            {
                var builder = new SqlFilterBuilder<BatchBase.Columns>();

                if (string.IsNullOrEmpty(id))
                    await Provider.Load<TransactionBatch>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);
                else
                {
                    builder.AppendEquals(BatchBase.Columns.BatchId, id);
                    var list = new TransactionBatchProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }
            return Provider.Items;
        }

        protected virtual async Task<TransactionBatch> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.BatchId, id, false));
        }

        protected virtual async Task<List<TransactionBatch>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Batch Code is provided along with more than one record.");

            var entityList = new List<TransactionBatch>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);

                await this.ValidateEntityListAsync(entityList);
                this.Provider?.Update(this.CompId);
            }

            return entityList;
        }

        protected virtual async Task<TransactionBatch> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.BatchId) || string.IsNullOrWhiteSpace(bodyItem.BatchId))
                bodyItem.BatchId = code;
            else
                code = bodyItem.BatchId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new TransactionBatch(this.CompId);              
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Batch Code '{0}' could not be found.", code));           

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.Lock = (!ApiUserSkipped.IsApiUserSkipped(bodyItem.Lock)) ? (bool)bodyItem.Lock : false;
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            TransactionBatch entity = null;
            var list = await Load(id);
            if (list.Count > 0)
                entity = list[0];

            if (entity == null)
                throw new InvalidValueException(string.Format("Batch Code '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void SaveChanges()
        {
            ValidateEntityList(Provider.Items.ChangedItems);
            Provider.Update(this.CompId);
        }

        protected virtual void ProcessDefaultChange(TransactionBatch entity)
        {
            SaveChanges();
            Batch.SetDefaultBatch(entity.FunctionId, entity.BatchId, entity.DefaultYn, this.CompId, null);
        }

        protected virtual void ProcessLockChange(TransactionBatch entity)
        {
            SaveChanges();
            if (entity.Lock)
                entity.LockBatch(null);
            else
                entity.UnLockBatch(null);
        }
        #endregion Helper Methods

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out Action<dynamic, ApiEntityPropertyChangingArgs> action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionBatch> action))
                action.Invoke(sender as TransactionBatch);
        }
        #endregion Event Handlers

        #region Properties
        private TransactionBatchProvider Provider { get; } = new TransactionBatchProvider();

        protected SortedDictionary<string, Action<TransactionBatch>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionBatch>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "121C2912-669E-4126-84CC-329AA27D17AC";
        #endregion Fields
    }
}
