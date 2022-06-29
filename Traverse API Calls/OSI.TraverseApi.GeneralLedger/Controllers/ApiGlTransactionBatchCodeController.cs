#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Batching;
using TRAVERSE.Business.GeneralLedger;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.GeneralLedger.Controllers
{
    public class ApiGlGLTransactionBatchCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "batchcode/{id?}", typeof(GLTransactionBatch))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "batchcode/{id?}", typeof(GLTransactionBatch))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "batchcode/{id?}", typeof(GLTransactionBatch))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "batchcode/{id}", typeof(GLTransactionBatch))]
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
            if (args.Entity is GLTransactionBatch batch)
            {
                if (StringHelper.AreEqual(args.FieldName, BatchBase.Columns.Lock.ToString(), false))
                {
                    args.ActualValue = batch.Lock;
                }
            }
        }
        #endregion

        protected virtual async Task<EntityList<GLTransactionBatch>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.BatchId, false))))
            {
                var builder = new SqlFilterBuilder<GLTransactionBatch.Columns>();
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<GLTransactionBatch>(this.CompId, new FilterCriteria(builder.ToString(), ""), PageNumber, PageSize);
                else
                {                    
                    builder.AppendEquals(GLTransactionBatch.Columns.BatchId, id);
                    var list = new GLTransactionBatchProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<GLTransactionBatch> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.BatchId, id, false));
        }

        protected virtual async Task<List<GLTransactionBatch>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Batch Code is provided along with more than one record.");

            var entityList = new List<GLTransactionBatch>();
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

        protected virtual async Task<GLTransactionBatch> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
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

                entity = new GLTransactionBatch(this.CompId);
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
            GLTransactionBatch entity = null;
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

        protected virtual void ProcessDefaultChange(GLTransactionBatch entity)
        {
            SaveChanges();
            Batch.SetDefaultBatch(entity.FunctionId, entity.BatchId, entity.DefaultYn, this.CompId, null);
        }

        protected virtual void ProcessLockChange(GLTransactionBatch entity)
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
            Action<GLTransactionBatch> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as GLTransactionBatch);
        }
        #endregion  Event Handlers

        #region Properties
        protected GLTransactionBatchProvider Provider { get; } = new GLTransactionBatchProvider();

        protected SortedDictionary<string, Action<GLTransactionBatch>> PropertyDictionary { get; } = new SortedDictionary<string, Action<GLTransactionBatch>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "4461BC39-DE8B-4ED0-844B-763F22D0CF5D";
        #endregion Fields
    }
}
