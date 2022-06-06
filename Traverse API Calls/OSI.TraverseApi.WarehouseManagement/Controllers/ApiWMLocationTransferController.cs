#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMLocationTransferController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "locationtransfer/{batchid?}", typeof(Transfer))]
        public async Task<IHttpActionResult> Get(string batchId = null)
        {
            return Ok(await Load(batchId));
        }

        [ApiRoute(FunctionID, 2f, "locationtransfer/{batchid?}/detail/{id:long}", typeof(Transfer))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string batchId = null, long id = 0)
        {
            return Ok(await ProcessEditRequest(false, body, batchId, id));
        }

        [ApiRoute(FunctionID, 2f, "locationtransfer/{batchid?}", typeof(Transfer))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string batchId = null)
        {
            return Ok(await ProcessEditRequest(true, body, batchId));
        }

        [ApiRoute(FunctionID, 2f, "locationtransfer/{batchid}/detail/{id:long}", typeof(Transfer))]
        public async Task Delete(string batchId, long id)
        {
            await MarkToDelete(batchId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() {}
        #endregion Overrides

        protected virtual async Task<EntityList<Transfer>> Load(string batchId)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(batchId) && !Provider.Items.Exists(i => StringHelper.AreEqual(batchId, i.BatchId, false))))
            {
                if (string.IsNullOrEmpty(batchId))
                    await Provider.Load<Transfer>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TransferBase.Columns>();
                    builder.AppendEquals(TransferBase.Columns.BatchId, batchId);
                    var list = new TransferProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Transfer> Find(string batchId, long? id = 0)
        {
            var list = await Load(batchId);
            return list.Find(x => x.TranKey == id);
        }

        protected virtual async Task<List<Transfer>> ProcessEditRequest(bool isCreate, dynamic body, string batchId = null, long? id = 0)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Transfer is provided along with more than one record.");

            var entityList = new List<Transfer>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, batchId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Transfer> ProcessBodyItem(bool isCreate, dynamic bodyItem, string batchId, long? id = 0)
        {
            string code = batchId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.BatchId) || string.IsNullOrWhiteSpace(bodyItem.BatchId))
                bodyItem.BatchId = code;
            else
                code = bodyItem.BatchId;

            var entity = await this.Find(code, id);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Transfer(this.CompId);
                entity.BatchId =  code ?? Utility.GlobalBatchId;
                entity.TransDate = !ApiUserSkipped.IsApiUserSkipped(bodyItem.TransDate) ? bodyItem.TransDate : ApplicationContext.SessionDate;
                entity.EntryDate = ApplicationContext.SessionDate;
            }           
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transfer ID '{0}' could not be found.", id));
            else if (entity.Status == 2)
                throw new Exception("Completed status of location transfer cannot be changed.");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);

            entity.SetItemDefault();
            entity.SetDefaultBatch(entity.BatchId);

            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string batchId, long id)
        {
            var entity = await this.Find(batchId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transfer '{0}' could not be found.", id));

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
            Action<Transfer> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Transfer);
        }
        #endregion Event Handlers

        #region Properties
        protected TransferProvider Provider { get; } = new TransferProvider();

        protected SortedDictionary<string, Action<Transfer>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Transfer>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "80db8c35-e296-473b-80ff-7b47c082d55a";
        #endregion Fields
    }
}
