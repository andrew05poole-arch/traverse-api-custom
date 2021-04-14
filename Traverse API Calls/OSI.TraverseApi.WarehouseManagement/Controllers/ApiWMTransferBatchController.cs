#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Batching;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMTransferBatchController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "batch/transfer/{id?}", typeof(TransferBatch))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "batch/transfer/{id?}", typeof(TransferBatch))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "batch/transfer/{id?}", typeof(TransferBatch))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "batch/transfer/{id}", typeof(TransferBatch))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.PropertyDictionary.Add(BatchBase.Columns.DefaultYn.ToString(), ProcessDefaultChange);
            this.PropertyDictionary.Add(BatchBase.Columns.Lock.ToString(), ProcessLockChange);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.Entity is TransferBatch batch)
            {
                if (StringHelper.AreEqual(args.FieldName, BatchBase.Columns.Lock.ToString(), false))
                {
                    args.ActualValue = batch.Lock;
                }
            }
        }
        #endregion

        protected virtual async Task<EntityList<TransferBatch>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.BatchId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<TransferBatch>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TransferBatch.Columns>();
                    builder.AppendEquals(TransferBatch.Columns.BatchId, id);
                    var list = new TransferBatchProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<TransferBatch> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.BatchId, id, false));
        }

        protected virtual async Task<List<TransferBatch>> ProcessEditRequest(bool isCreate, dynamic bodyItem, string id = null)
        {
            object[] list;

            if (bodyItem is object[])
                list = bodyItem;
            else
                list = new object[1] { bodyItem };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Batch Code is provided along with more than one record.");

            var entityList = new List<TransferBatch>();
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

        protected virtual async Task<TransferBatch> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = ApiUserSkipped.IsApiUserSkipped(bodyItem.BatchId) ? (bodyItem.BatchId = id) : bodyItem.BatchId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new TransferBatch(this.CompId);
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
            TransferBatch entity = null;
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

        protected virtual void ProcessDefaultChange(TransferBatch entity)
        {
            SaveChanges();
            Batch.SetDefaultBatch(entity.FunctionId, entity.BatchId, entity.DefaultYn, this.CompId, null);
        }

        protected virtual void ProcessLockChange(TransferBatch entity)
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
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransferBatch> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransferBatch);
        }
        #endregion  Event Handlers

        #region Properties
        protected TransferBatchProvider Provider { get; } = new TransferBatchProvider();

        protected SortedDictionary<string, Action<TransferBatch>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransferBatch>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "20201e44-04cd-4b08-936e-af018a24a77a";
        #endregion Fields
    }
}
