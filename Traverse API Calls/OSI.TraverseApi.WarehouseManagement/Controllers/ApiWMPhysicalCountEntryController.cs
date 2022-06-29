#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.WarehouseManagement.Controllers
{
    public class ApiWMPhysicalCountEntryController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "physicalcount/{id:long?}", typeof(PhysicalCountEntry))]
        [ApiRoute(FunctionID, 2f, "physicalcount/item/{itemid?}", typeof(PhysicalCountEntry))]
        [ApiRoute(FunctionID, 2f, "physicalcount/batch/{batchid}", typeof(PhysicalCountEntry))]
        public async Task<IHttpActionResult> Get(long? id = null, string itemid = null, string batchid = null)
        {
            return Ok(await Load(id, itemid, batchid));
        }

        [ApiRoute(FunctionID, 2f, "physicalcount/{id:long?}", typeof(PhysicalCountEntry))]
        [ApiRoute(FunctionID, 2f, "physicalcount/item/{itemid?}", typeof(PhysicalCountEntry))]
        [ApiRoute(FunctionID, 2f, "physicalcount/batch/{batchid?}", typeof(PhysicalCountEntry))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, long? id = null, string itemId = null, string batchId = null)
        {
            return Ok(await ProcessEditRequest(false, body, id,itemId, batchId));
        }

        [ApiRoute(FunctionID, 2f, "physicalcount/", typeof(PhysicalCountEntry))]
        [ApiRoute(FunctionID, 2f, "physicalcount/item/{itemid?}", typeof(PhysicalCountEntry))]
        [ApiRoute(FunctionID, 2f, "physicalcount/batch/{batchid?}", typeof(PhysicalCountEntry))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId = null, string batchId = null)
        {
            return Ok(await ProcessEditRequest(true, body, null, itemId, batchId));
        }

        [ApiRoute(FunctionID, 2f, "physicalcount/{id:long}", typeof(PhysicalCountEntry))]
        public async Task Delete(long id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.Entity is PhysicalCountEntry entry)
            {
                if (StringHelper.AreEqual(args.FieldName, "Error",false))
                {
                    args.ActualValue = this.GetErrorFlagDescription(entry);
                }
            }
        }
        #endregion

        protected virtual async Task<EntityList<PhysicalCountEntry>> Load(long? id, string itemid, string batchid)
        {
            if (id != null)
                this.LoadByID(id);
            else if (!string.IsNullOrEmpty(itemid))
                this.LoadByItemId(itemid);
            else if (!string.IsNullOrEmpty(batchid))
                this.LoadByBatchCode(batchid);
            else if (this.Provider.Items.Count == 0)
                await Provider.Load<PhysicalCountEntry>(this.CompId, PageNumber, PageSize);


            await this.FilterEntityListAsync(this.Provider.Items);

            return Provider.Items;
        }

        protected virtual void LoadByBatchCode(string batchid)
        {
            if (this.Provider.Items.Count <= 0 || !this.Provider.Items.Exists(i => i.BatchId == batchid))
            {
                var builder = new SqlFilterBuilder<PhysicalCountEntryBase.Columns>();

                if (string.IsNullOrEmpty(batchid))
                    Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                else
                {
                    builder.AppendEquals(PhysicalCountEntryBase.Columns.BatchId, batchid);
                    var list = new PhysicalCountEntryProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }
            }
        }

        protected virtual void LoadByItemId(string itemid)
        {
            if (this.Provider.Items.Count <= 0 || !this.Provider.Items.Exists(i => i.ItemId == itemid))
            {
                var builder = new SqlFilterBuilder<PhysicalCountEntryBase.Columns>();

                if (string.IsNullOrEmpty(itemid))
                    this.Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                else
                {
                    builder.AppendEquals(PhysicalCountEntryBase.Columns.ItemId, itemid.ToString());
                    var list = new PhysicalCountEntryProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }
            }
        }

        protected virtual void LoadByID(long? id)
        {
            if (this.Provider.Items.Count <= 0 || !this.Provider.Items.Exists(i => i.Id == id))
            {
                var builder = new SqlFilterBuilder<PhysicalCountEntryBase.Columns>();

                if (id == null)
                    this.Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                else
                {
                    builder.AppendEquals(PhysicalCountEntryBase.Columns.Id, id.ToString());
                    var list = new PhysicalCountEntryProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }
            }
        }

        protected virtual async Task<PhysicalCountEntry> Find(long? id)
        {
            var list = await Load(id, string.Empty, string.Empty);                        
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<PhysicalCountEntry>> ProcessEditRequest(bool isCreate, dynamic body, long? id, string itemId, string batchId = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Physical Entry ID is provided along with more than one record.");

            if (list.Length > 1 && !string.IsNullOrEmpty (itemId))
                throw new InvalidValueException("Call is ambiguous. Item ID is provided along with more than one record.");

            if (list.Length > 1 && !string.IsNullOrEmpty(batchId))
                throw new InvalidValueException("Call is ambiguous. Batch Code is provided along with more than one record.");

            var entityList = new List<PhysicalCountEntry>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id, itemId, batchId);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<PhysicalCountEntry> ProcessBodyItem(bool isCreate, dynamic bodyItem, long? id, string itemId, string batchId)
        {
            long? code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = bodyItem.Id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemId) || bodyItem.ItemId == null)
                bodyItem.ItemId = itemId;
            else
                itemId = bodyItem.ItemId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.BatchId) || bodyItem.BatchId == null)
                bodyItem.BatchId = batchId;
            else
                batchId = bodyItem.BatchId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new PhysicalCountEntry(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException($"Physical Entry ID '{code}' could not be found.");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(long id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new InvalidValueException($"Physical Count Entry for ID'{id}' could not be found.");

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual string GetErrorFlagDescription(PhysicalCountEntry entry)
        {
            switch (entry.ErrorFlag)
            {
                case 0:
                    return "Valid";
                case 1:
                    return string.Format("Location ID must be Location ID of the batch '{0}' ", entry.BatchId);                    
                case 2:
                    return "Bin must have been frozen before counting it";
                case 3:
                    return "Item ID must have been frozen before counting it";                    
                case 4:
                    return "Lot No. is required.";
                case 5:
                    return "Serial No. is required.";
                case 6:
                    return string.Format("Unit {0} is invalid.", entry.CountUom);
                case 7:
                    return "Same item/serial number cannot exist in multiple batches";
                case 8:
                    return "Combination of Item ID, Serial Number and Counted must be unique";
                case 9:
                    return "Container is Invalid";
                default:
                    return string.Empty;
            }
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
            Action<PhysicalCountEntry> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PhysicalCountEntry);
        }
        #endregion Event Handlers

        #region Properties
        protected PhysicalCountEntryProvider Provider { get; } = new PhysicalCountEntryProvider();

        protected SortedDictionary<string, Action<PhysicalCountEntry>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PhysicalCountEntry>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "b4f6f157-9ded-47cb-a96a-2f598c8fd533";
        #endregion Fields
    }
}
