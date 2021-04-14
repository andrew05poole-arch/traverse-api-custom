#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.CompanySetup.Controllers
{
    public class ApiSmDescriptionItemController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "descriptionitem/{id?}", typeof(Item))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok( await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "descriptionitem/{id?}", typeof(Item))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "descriptionitem/{id?}", typeof(Item))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "descriptionitem/{id}", typeof(Item))]
        public async Task Delete(string id)
        {
             await MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overriders
        protected override void AddPropertyDelegates() { }
        #endregion Overriders

        protected virtual async Task<EntityList<Item>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => i.ItemCode == id)))
            {
                if (id == null)
                    await Provider.Load<Item>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<ItemBase.Columns>();
                    builder.AppendEquals(ItemBase.Columns.ItemCode, id.ToString());
                    var list = new ItemProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<Item> Find(string id)
        {
            var list = await Load(id);
            return list?.Find(x => x.ItemCode == id);
        }

        protected virtual async Task<List<Item>> ProcessEditRequest(bool isCreate, dynamic body, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Item ID is provided along with more than one record.");

            var entityList = new List<Item>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Item> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemCode) || bodyItem.ItemCode == null)
                bodyItem.ItemCode = code;
            else
                code = bodyItem.ItemCode;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Item(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Item ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Item ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
        }
        #endregion Helper Methods

        #region Event Handler
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Item> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Item);
        }
        #endregion Event Handler

        #region Properties
        private ItemProvider Provider { get; } = new ItemProvider();
        protected SortedDictionary<string, Action<Item>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Item>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "7283A2B0-5690-474B-A7B2-2EFD65DCE242";
        #endregion Fields
    }
}
