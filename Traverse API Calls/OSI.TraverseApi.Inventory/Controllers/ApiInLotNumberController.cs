#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.InventoryLookup.Controllers
{
    public class ApiInLotNumberController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "item/{itemid}/location/{locid}/lot/{id?}", typeof(Lot))]
        public async Task<IHttpActionResult> Get(string itemId = null, string locId = null, string id = null)
        {
            return Ok(await this.Load(itemId, locId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemid}/location/{locid}/lot/{id?}", typeof(Lot))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId = null, string locId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, locId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemid}/location/{locid}/lot/{id?}", typeof(Lot))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId = null, string locId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, locId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemid}/location/{locid}/lot/{id}", typeof(Lot))]
        public async Task Delete(string itemId, string locId, string id)
        {
            await this.MarkToDelete(itemId, locId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(LotBase.Columns.VendId.ToString(), OnVerdIdUpdated);
        }

        protected virtual async Task<EntityList<Lot>> Load(string itemId, string locationId, string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.LotNum, false))))
            {
                var builder = new SqlFilterBuilder<LotBase.Columns>();
                builder.AppendEquals(LotBase.Columns.ItemId, itemId);
                builder.AppendEquals(LotBase.Columns.LocId, locationId);

                if (!string.IsNullOrEmpty(id))
                    builder.AppendEquals(LotBase.Columns.LotNum, id.ToString());
                
                var list = new LotProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                Provider.Items.AddRange(list);

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Lot> Find(string itemId, string locationId, string id)
        {
            var list = await Load(itemId, locationId, id);
            return list.Find(x => StringHelper.AreEqual(id, x.LotNum, false));
        }

        protected virtual async Task<List<Lot>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string locationId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Lot Number is provided along with more than one record.");

            var entityList = new List<Lot>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, locationId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Lot> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string locationId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LotNum) || string.IsNullOrWhiteSpace(bodyItem.LotNum))
                bodyItem.LotNum = code;
            else
                code = bodyItem.LotNum;

            var entity = await this.Find(itemId, locationId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Lot(this.CompId)
                {
                    ItemId = itemId,
                    LocId = locationId
                };
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Lot '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string locationId, string id)
        {
            var entity = await this.Find(itemId, locationId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Lot '{0}' could not be found.", id));

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
            Action<Lot> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Lot);
        }

        protected virtual void OnVerdIdUpdated(Lot entity)
        {
            if (!string.IsNullOrEmpty(entity.VendId))
            {
                Vendor vendor = EntityProvider.GetEntity<Vendor, VendorProvider>(new string[] { entity.VendId }, this.CompId, null);

                if (vendor == null)
                    throw new InvalidValueException(string.Format("Vendor ID '{0}' is invalid.", entity.VendId));
            }
        }
        #endregion Event Handlers

        #region Properties
        protected LotProvider Provider { get; } = new LotProvider();

        protected SortedDictionary<string, Action<Lot>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Lot>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "354EBCCB-AABA-4977-8C3D-5F86628FAB2F";
        #endregion Fields
    }
}
