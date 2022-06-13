
#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.Sys;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Inventory.Controllers
{
    public class ApiInItemUnitController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "item/{itemid}/unit/{id?}", typeof(ItemUnit))]
        public async Task<IHttpActionResult> Get(string itemId, string id = null)
        {
            return Ok(await Load(itemId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemid}/unit/{id?}", typeof(ItemUnit))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemid}/unit/{id?}", typeof(ItemUnit))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemid}/unit/{id}", typeof(ItemUnit))]
        public async Task Delete(string itemId, string id)
        {
            await this.MarkToDelete(itemId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        { }

        protected virtual async Task<EntityList<ItemUnit>> Load(string itemId, string id)
        {
            var list = CurrentItem?.Units;

            if (CurrentItem == null || !StringHelper.AreEqual(CurrentItem.ItemId, itemId, false))
            {
                var builder = new SqlFilterBuilder<ItemBase.Columns>();
                builder.AppendEquals(ItemBase.Columns.ItemId, itemId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiInItemController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Item '{0}' could not be found.", id));

                CurrentItem = Provider.Items[0];

                list = CurrentItem.Units;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(ItemUnitBase.Columns.Uom, id, true);

            return list;
        }

        protected virtual async Task<ItemUnit> Find(string itemId, string id)
        {
            var list = await Load(itemId, id);
            return list.Find(x => StringHelper.AreEqual(x.Uom, id, false));
        }

        protected virtual async Task<List<ItemUnit>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Call is ambiguous. UOM is provided along with more than one record.");

            var entityList = new List<ItemUnit>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ItemUnit> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom) || string.IsNullOrWhiteSpace(bodyItem.Uom))
                bodyItem.Uom = code;
            else
                code = bodyItem.Uom;

            var entity = await this.Find(itemId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CurrentItem.Units.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("UOM '{0}' could not be found on item '{1}'", code, itemId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string id)
        {
            var entity = await this.Find(itemId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("UOM '{0}' could not be found on item '{1}'", id, itemId));

            CurrentItem.Units.Remove(entity);
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
            Action<ItemUnit> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemUnit);
        }
        #endregion Event Handlers

        #region Properties
        protected ItemProvider Provider { get; } = new ItemProvider();

        protected Item CurrentItem { get; set; }

        protected SortedDictionary<string, Action<ItemUnit>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ItemUnit>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "58296D2D-95F3-4681-AD2A-A7AEED57F064";
        #endregion Properties
    }
}
