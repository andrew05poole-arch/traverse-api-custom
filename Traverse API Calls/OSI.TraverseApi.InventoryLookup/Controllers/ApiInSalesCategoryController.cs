#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.InventoryLookup.Controllers
{
    public class ApiInSalesCategoryController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "salescategory/{id?}", typeof(SalesCategory))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "salescategory/{id?}", typeof(SalesCategory))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "salescategory/{id?}", typeof(SalesCategory))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "salescategory/{id}", typeof(SalesCategory))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //No special property changed events to set
        }

        protected virtual async Task<EntityList<SalesCategory>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.SalesCat, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<SalesCategory>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<SalesCategory.Columns>();
                    builder.AppendEquals(SalesCategory.Columns.SalesCat, id);
                    var list = new SalesCategoryProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<SalesCategory> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.SalesCat, id, false));
        }

        protected virtual async Task<List<SalesCategory>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Sales Category is provided along with more than one record.");

            var entityList = new List<SalesCategory>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<SalesCategory> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SalesCat) || string.IsNullOrWhiteSpace(bodyItem.SalesCat))
                bodyItem.SalesCat = code;
            else
                code = bodyItem.SalesCat;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new SalesCategory(this.CompId);
                entity.SalesCat = code;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Sales Category '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Sales Category '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
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
            Action<SalesCategory> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as SalesCategory);
        }
        #endregion Event Handlers

        #region Properties
        protected SalesCategoryProvider Provider { get; } = new SalesCategoryProvider();

        protected SortedDictionary<string, Action<SalesCategory>> PropertyDictionary { get; } = new SortedDictionary<string, Action<SalesCategory>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "66CAC34F-4F96-4BD8-8442-3B7CC9CA5AB4";
        #endregion Fields
    }
}
