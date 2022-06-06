#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Pricing;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.Pricing.Controllers
{
    public class ApiSoPriceStructureHeaderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "pricestructure/{priceid?}", typeof(PriceStructureHeader))]
        public async Task<IHttpActionResult> Get(string priceId = null)
        {
            return Ok(await this.Load(priceId));
        }

        [ApiRoute(FunctionID, 2f, "pricestructure/{priceid?}", typeof(PriceStructureHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string priceId = null)
        {
            return Ok(await ProcessEditRequest(false, body, priceId));
        }

        [ApiRoute(FunctionID, 2f, "pricestructure/{priceid?}", typeof(PriceStructureHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string priceId = null)
        {
            return Ok(await ProcessEditRequest(true, body, priceId));
        }

        [ApiRoute(FunctionID, 2f, "pricestructure/{priceid}", typeof(PriceStructureHeader))]
        public async Task Delete(string priceId = null)
        {
            await this.MarkToDelete(priceId);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<PriceStructureHeader>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.PriceId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<PriceStructureHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<PriceStructureHeaderBase.Columns>();
                    builder.AppendEquals(PriceStructureHeaderBase.Columns.PriceId, id);
                    var list = new PriceStructureHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<PriceStructureHeader> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.PriceId, id, false));
        }

        protected virtual async Task<List<PriceStructureHeader>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Price Structure is provided along with more than one record.");

            var entityList = new List<PriceStructureHeader>();
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

        protected virtual async Task<PriceStructureHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PriceId) || string.IsNullOrWhiteSpace(bodyItem.PriceId))
                bodyItem.PriceId = code;
            else
                code = bodyItem.PriceId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new PriceStructureHeader(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Price Structure '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;
            entity.PropertyChanged -= Entity_PropertyChanged;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Price Structure '{0}' could not be found.", id));

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
            Action<PriceStructureHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PriceStructureHeader);
        }
        #endregion Event Handlers

        #region Properties
        protected PriceStructureHeaderProvider Provider { get; } = new PriceStructureHeaderProvider();

        protected SortedDictionary<string, Action<PriceStructureHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PriceStructureHeader>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "F9355878-E277-486B-AC94-EB7F587B249A";
        #endregion Properties
    }
}
