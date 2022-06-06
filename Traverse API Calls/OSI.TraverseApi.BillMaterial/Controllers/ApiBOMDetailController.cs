#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.BillMaterial;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.BillMaterial.Controllers
{
    public class ApiBOMDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bom/{bmbomid}/component", typeof(BomDetail))]
        [ApiRoute(FunctionID, 2f, "bom/{bmbomid}/component/{itemid}/location/{locationid}", typeof(BomDetail))]
        public async Task<IHttpActionResult> Get(int bmBomId = 0,string itemId = null, string locationId = null)
        {
            return Ok(await this.Load(bmBomId, itemId, locationId));
        }

        [ApiRoute(FunctionID, 2f, "bom/{bmbomid}/component", typeof(BomDetail))]
        [ApiRoute(FunctionID, 2f, "bom/{bmbomid}/component/{itemid}/location/{locationid}", typeof(BomDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int bomId = 0, string itemId = null, string locationId = null)
        {
            return Ok(await ProcessEditRequest(false, body, bomId, itemId, locationId));
        }

        [ApiRoute(FunctionID, 2f, "bom/{bmbomid}/component", typeof(BomDetail))]
        [ApiRoute(FunctionID, 2f, "bom/{bmbomid}/component/{itemid}/location/{locationid}", typeof(BomDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body,int bomId = 0, string itemId = null, string locationId = null)
        {
            return Ok(await ProcessEditRequest(true, body,bomId, itemId, locationId));
        }

        [ApiRoute(FunctionID, 2f, "bom/{bmbomid}/component/{itemid}/location/{locationid}", typeof(BomDetail))]
        public async Task Delete(int bmBomId, string itemId, string locationId)
        {
            await this.MarkToDelete(bmBomId, itemId, locationId);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion

        protected virtual async Task<EntityList<BomDetail>> Load(int bmBomId, string itemId, string locId)
        {
            EntityList<BomDetail> bomDetails = new EntityList<BomDetail>();
            SqlFilterBuilder<BomDetailBase.Columns> builder = new SqlFilterBuilder<BomDetailBase.Columns>();
            builder.AppendEquals(BomDetailBase.Columns.BmBomId, bmBomId.ToString());
                        
            this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (this.Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The Bill of Material '{0}' could not be found.", bmBomId));

            this.CurrentBom = this.Provider[0];

            if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(locId))
            {
                bomDetails = this.Provider.Items[0]?.Components?.FindAll(x => StringHelper.AreEqual(x.ItemId, itemId, false)
                   && StringHelper.AreEqual(x.LocId, locId, false));
            }
            else
                bomDetails = this.Provider.Items[0]?.Components;            

            await this.FilterEntityListAsync(bomDetails);

            return bomDetails;
        }

        protected virtual async Task<BomDetail> Find(int bomId, string itemId, string locationId)
        {
            var list = await this.Load(bomId,itemId, locationId);
            return list?.Find(x => x.BmBomId == bomId && x.ItemId == itemId && x.LocId == locationId);
        }        

        protected virtual async Task<List<BomDetail>> ProcessEditRequest(bool isCreate, dynamic body,int bomId, string itemId, string locationId)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(locationId))
                throw new InvalidValueException("Call is ambiguous. Location ID is provided along with more than one record.");

            if (list.Length > 1 && bomId != 0)
                throw new InvalidValueException("Call is ambiguous. Bom Id ID is provided along with more than one record.");

            if (list.Length > 1 && !string.IsNullOrEmpty(itemId))
                throw new InvalidValueException("Call is ambiguous. Item ID is provided along with more than one record.");

            var entityList = new List<BomDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item,bomId, itemId, locationId);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<BomDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem,int bomId, string itemId, string locationId)
        {
            string code = locationId;
            string itemCode = itemId;
            int bomCode = bomId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId) || bodyItem.LocId == null)
                bodyItem.LocId = code;
            else
                code = bodyItem.LocId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemId) || bodyItem.ItemId == null)
                bodyItem.ItemId = itemCode;
            else
                itemCode = bodyItem.ItemId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.BmBomId) || bodyItem.BmBomId == null)
                bodyItem.BmBomId = bomCode;
            else
                bomCode = Convert.ToInt32( bodyItem.BmBomId);

            var entity = await this.Find(bomCode,itemCode, code);

            if (isCreate)
            {
                if (entity != null && bomCode != 0)
                    return entity;

                entity = this.CurrentBom.Components.AddNew();
            }
            else if (entity == null)
                throw new NothingToProcessException(string.Format("Bill of Material with Item ID '{0}' and Location ID '{1}' does not exist.", itemCode, code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }        

        protected virtual async Task MarkToDelete(int bmBomId, string itemId, string locId)
        {
            var bom = await this.Find(bmBomId, itemId, locId);

            if (bom == null)
                throw new NothingToProcessException(string.Format("The Bill of Material '{0}' with Item ID '{1}' and Location ID '{2}' could not be found.", bmBomId, itemId, locId));
            else
            {
                this.Provider.Items[0].Components.Remove(bom);
                this.Provider?.Update(CompId);
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
            Action<BomDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as BomDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected SortedDictionary<string, Action<BomDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<BomDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        protected Bom CurrentBom { get; set; }

        protected BomProvider Provider { get; } = new BomProvider();

        public const string FunctionID = "F20AE7A0-3AB6-4586-8C5C-6A57F995778D";
        #endregion Properties
    }
}
