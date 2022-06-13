#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Pricing;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Pricing.Controllers
{
    public class ApiSoPriceStructureDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "pricestructure/{priceid}/detail/{custlevel?}", typeof(PriceStructureDetail))]
        public async Task<IHttpActionResult> Get(string priceId = null, string custLevel = null)
        {
            return Ok(await this.Load(priceId, custLevel));
        }

        [ApiRoute(FunctionID, 2f, "pricestructure/{priceid}/detail/{custlevel?}", typeof(PriceStructureDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string priceId = null, string custLevel = null)
        {
            return Ok(await ProcessEditRequest(false, body, priceId, custLevel));
        }

        [ApiRoute(FunctionID, 2f, "pricestructure/{priceid}/detail/{custlevel?}", typeof(PriceStructureDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string priceId = null, string custLevel = null)
        {
            return Ok(await ProcessEditRequest(true, body, priceId, custLevel));
        }

        [ApiRoute(FunctionID, 2f, "pricestructure/{priceid}/detail/{custlevel}", typeof(PriceStructureDetail))]
        public async Task Delete(string priceId = null, string custLevel = null)
        {
            await this.MarkToDelete(priceId, custLevel);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<PriceStructureHeader> Find(string priceId)
        {
            var price = this.Provider.Items.Find(x => StringHelper.AreEqual(x.PriceId, priceId, false));
            if (price == null)
            {
                price = EntityProvider.GetEntity<PriceStructureHeader, PriceStructureHeaderProvider>(new string[] { priceId }, this.CompId, null);
                if (price != null)
                    this.Provider.Items.Add(price);

                await FilterEntityListAsync(Provider.Items, ApiSoPriceStructureHeaderController.FunctionID);
                if (!Provider.Items.Contains(price))
                    price = null;
            }
            return price;
        }

        protected virtual async Task<EntityList<PriceStructureDetail>> Load(string priceId, string custLevel)
        {
            var header = await Find(priceId);

            if (header == null)
                return new EntityList<PriceStructureDetail>();

            await this.FilterEntityListAsync(header.DetailRecords);
            
            if (!string.IsNullOrEmpty(custLevel))
            {
                return header.DetailRecords.FindAll(x => StringHelper.AreEqual(x.CustLevel, custLevel, false));
            }
            return header.DetailRecords;
        }

        protected virtual async Task<PriceStructureDetail> Find(PriceStructureHeader header, string custLevel)
        {
            if (header == null)
                return null;

            var list = header.DetailRecords;
            await FilterEntityListAsync(list);
            return list.Find(x => StringHelper.AreEqual(x.CustLevel, custLevel, false));
        }

        protected virtual async Task<List<PriceStructureDetail>> ProcessEditRequest(bool isCreate, dynamic body, string priceId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Price Structure is provided along with more than one record.");

            var entityList = new List<PriceStructureDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, priceId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<PriceStructureDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string priceId, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.CustLevel) || string.IsNullOrWhiteSpace(bodyItem.CustLevel))
                bodyItem.CustLevel = code;
            else
                code = bodyItem.CustLevel;

            var header = await this.Find(priceId);
            var entity = await this.Find(header, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = header.DetailRecords.AddNew();
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

        protected virtual async Task MarkToDelete(string priceId, string id)
        {
            var header = await this.Find(priceId);
            var entity = await this.Find(header, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Price Structure '{0}' could not be found.", id));

            entity.Parent.DetailRecords.Remove(entity);
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
            Action<PriceStructureDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PriceStructureDetail);
        }
        #endregion Event Handler

        #region Properties
        private PriceStructureHeaderProvider Provider { get; } = new PriceStructureHeaderProvider();

        protected SortedDictionary<string, Action<PriceStructureDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PriceStructureDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private const string FunctionID = "C906E03D-1D1D-4F95-B466-F214F9C4A7D9";
        #endregion Properties
    }
}
