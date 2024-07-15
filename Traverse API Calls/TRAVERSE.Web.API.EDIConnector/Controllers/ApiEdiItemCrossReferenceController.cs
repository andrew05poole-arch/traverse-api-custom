using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.EDI;
using TRAVERSE.Core;

namespace TRAVERSE.Web.API.EDIConnector.Controllers
{
    public class ApiEdiItemCrossReferenceController : ApiControllerBase
    {
        #region Web Methods

        [ApiRoute(FunctionID, 2f, "itemcrossref/partner/{partnerId:long?}", typeof(PartnerItemXRef))]
        [ApiRoute(FunctionID, 2f, "itemcrossref/item/{itemId}", typeof(PartnerItemXRef))]
        [ApiRoute(FunctionID, 2f, "itemcrossref/partner/{partnerId:long?}/item/{itemId}", typeof(PartnerItemXRef))]
        [ApiRoute(FunctionID, 2f, "itemcrossref/partner/{partnerid}/item/{itemid}/uom/{uom}/uir/{uir}/ediuom/{ediuom}", typeof(PartnerItemXRef))]
        public async Task<IHttpActionResult> Get(long? partnerId = null, string itemId = null, string uom = null, string uir = null, string ediUom = null)
        {
            return Ok(await this.Load(false, partnerId, itemId, uom, uir, ediUom));
        }

        [ApiRoute(FunctionID, 2f, "itemcrossref", typeof(PartnerItemXRef))]
        //[ApiRoute(FunctionID, 2f, "itemcrossref/partner/{partnerid:long?}", typeof(PartnerItemXRef))]
        //[ApiRoute(FunctionID, 2f, "itemcrossref/item/{itemid}", typeof(PartnerItemXRef))]
        //[ApiRoute(FunctionID, 2f, "itemcrossref/partner/{partnerid:long?}/item/{itemid}", typeof(PartnerItemXRef))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, long? partnerId = null, string itemId = null, string uom = null, string uir = null, string ediUom = null)
        {
            return Ok(await ProcessEditRequest(false, body, partnerId, itemId, uom, uir, ediUom));
        }

        [ApiRoute(FunctionID, 2f, "itemcrossref", typeof(PartnerItemXRef))]
        //[ApiRoute(FunctionID, 2f, "itemcrossref/partner/{partnerid:long?}", typeof(PartnerItemXRef))]
        //[ApiRoute(FunctionID, 2f, "itemcrossref/item/{itemid}", typeof(PartnerItemXRef))]
        //[ApiRoute(FunctionID, 2f, "itemcrossref/partner/{partnerid:long?}/item/{itemid}", typeof(PartnerItemXRef))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)   //, long? partnerId = null, string itemId = null
        {
            return Ok(await ProcessEditRequest(true, body));        //, partnerId, itemId
        }

        [ApiRoute(FunctionID, 2f, "itemcrossref/partner/{partnerid}/item/{itemid}/uom/{uom}/uir/{uir}/ediuom/{ediuom}", typeof(PartnerItemXRef))]
        public async Task Delete(long partnerId, string itemId, string uom, string uir, string ediUom)
        {
            await this.MarkToDelete(partnerId, itemId, uom, uir, ediUom);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(PartnerItemXRefBase.Columns.TRAVItemId.ToString(), OnTRAVItemIdChanged);
            PropertyDictionary.Add(PartnerItemXRefBase.Columns.ItemColor.ToString(), OnItemColorChanged);
        }

        protected virtual void OnTRAVItemIdChanged(PartnerItemXRef entity)
        {
            if (entity == null) return;
            if (!string.IsNullOrEmpty(entity.TRAVItemId))
                entity.SetItemDefaults();
        }

        protected virtual void OnItemColorChanged(PartnerItemXRef entity)
        {
            if (entity == null) return;
            if (entity.ItemColor == "0" || string.IsNullOrEmpty(entity.ItemColor))
                entity.ItemColor = null;
        }

        protected virtual async Task<EntityList<PartnerItemXRef>> Load(bool isCreate, long? partnerId, string itemId, string uom, string uir, string ediUom)
        {
            if (Provider.Items.Count <= 0
                || !Provider.Items.Exists(x => x.PartnerId == partnerId)
                || !Provider.Items.Exists(x => StringHelper.AreEqual(x.TRAVItemId, itemId, false))
                || !Provider.Items.Exists(x => StringHelper.AreEqual(x.TRAVUOM, uom, false))
                || !Provider.Items.Exists(x => StringHelper.AreEqual(x.UIR, uir, false))
                || !Provider.Items.Exists(x => StringHelper.AreEqual(x.EDIUOM, ediUom, false))
                )
            {
                var builder = new SqlFilterBuilder<PartnerItemXRef.Columns>();

                if (partnerId != null)
                    builder.AppendEquals(PartnerItemXRefBase.Columns.PartnerId, Convert.ToString(partnerId));
                if (!string.IsNullOrEmpty(itemId))
                    builder.AppendEquals(PartnerItemXRefBase.Columns.TRAVItemId, itemId);
                if (!string.IsNullOrEmpty(uom))
                    builder.AppendEquals(PartnerItemXRefBase.Columns.TRAVUOM, uom);
                if (!string.IsNullOrEmpty(uir))
                    builder.AppendEquals(PartnerItemXRefBase.Columns.UIR, uir);
                if (!string.IsNullOrEmpty(ediUom))
                    builder.AppendEquals(PartnerItemXRefBase.Columns.EDIUOM, ediUom);

                var list = new PartnerItemXRefProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                Provider.Items.AddRange(list);

                if (Provider.Items.Count <= 0 && !isCreate)
                {
                    if (partnerId == null && !string.IsNullOrEmpty(itemId))
                        throw new NothingToProcessException(string.Format("Item ID '{0}' could not be found.", itemId));
                    if (partnerId != null && string.IsNullOrEmpty(itemId))
                        throw new NothingToProcessException(string.Format("Partner ID '{0}' could not be found.", partnerId));
                    else
                        throw new NothingToProcessException(string.Format("Partner ID '{0}' with Item ID '{1}' could not be found.", partnerId, itemId));
                }

                await this.FilterEntityListAsync(Provider.Items);
            }
            return Provider.Items;
        }

        protected virtual async Task<List<PartnerItemXRef>> ProcessEditRequest(bool isCreate, dynamic body, long? partnerId = null, string itemId = null, string uom = null, string uir = null, string ediUom = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && (partnerId != null
                && !string.IsNullOrEmpty(itemId)
                && !string.IsNullOrEmpty(uom)
                && !string.IsNullOrEmpty(uir)
                && !string.IsNullOrEmpty(ediUom)))
                throw new InvalidValueException("Call is ambiguous. Parnter ID, Item ID, Uom, UIR and EDIUOM are provided along with more than one record.");

            var entityList = new List<PartnerItemXRef>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, partnerId, itemId, uom, uir, ediUom);
                if (isCreate) this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<PartnerItemXRef> ProcessBodyItem(bool isCreate, dynamic bodyItem, long? partnerId, string itemId, string uom, string uir, string ediUom)
        {
            long partner = partnerId.GetValueOrDefault();
            string item = itemId;
            string travUom = uom;
            string ur = uir;
            string edUom = ediUom;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PartnerId) || bodyItem.PartnerId == null)
                bodyItem.PartnerId = partner;
            else
                partner = bodyItem.PartnerId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TRAVItemId) || string.IsNullOrWhiteSpace(bodyItem.TRAVItemId))
                bodyItem.TRAVItemId = item;
            else
                item = bodyItem.TRAVItemId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TRAVUOM) || string.IsNullOrWhiteSpace(bodyItem.TRAVUOM))
                bodyItem.TRAVUOM = travUom;
            else
                travUom = bodyItem.TRAVUOM;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.UIR) || string.IsNullOrWhiteSpace(bodyItem.UIR))
                bodyItem.UIR = ur;
            else
                ur = bodyItem.UIR;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EDIUOM) || string.IsNullOrWhiteSpace(bodyItem.EDIUOM))
                bodyItem.EDIUOM = edUom;
            else
                edUom = bodyItem.EDIUOM;

            var entity = await this.Find(isCreate, partner, item, travUom, ur, edUom);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new PartnerItemXRef(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Partner ID '{0}' with Item ID '{1}' could not be found.", partner, item));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(long partnerId, string itemId, string uom, string uir, string ediUom)
        {
            var entity = await this.Find(false, partnerId, itemId, uom, uir, ediUom);

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual async Task<PartnerItemXRef> Find(bool isCreate, long partnerId, string itemId, string uom, string uir, string ediUom)
        {
            var list = await Load(isCreate, partnerId, itemId, uom, uir, ediUom);
            return list.Find(x => x.PartnerId == partnerId
            && StringHelper.AreEqual(x.TRAVItemId, itemId, false)
            && StringHelper.AreEqual(x.TRAVUOM, uom, false)
            && StringHelper.AreEqual(x.UIR, uir, false)
            && StringHelper.AreEqual(x.EDIUOM, ediUom, false));
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
            Action<PartnerItemXRef> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PartnerItemXRef);
        }
        #endregion Event Handlers

        #region Properties
        protected PartnerItemXRefProvider Provider { get; } = new PartnerItemXRefProvider();

        protected PartnerItemXRef CurrentItem { get; set; }

        protected SortedDictionary<string, Action<PartnerItemXRef>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PartnerItemXRef>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "034d0c50-57ab-4271-8613-e18fb9b798c8";
        #endregion Fields
    }
}
