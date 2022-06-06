#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.BillMaterial;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.BillMaterial.Controllers
{
    public class ApiBOMHeaderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bom/{itemid}", typeof(Bom))]
        [ApiRoute(FunctionID, 2f, "bom/{itemid}/location/{locationid}", typeof(Bom))]
        public async Task<IHttpActionResult> Get(string itemId = null, string locationId = null)
        {
            return Ok(await this.Load(itemId, locationId));
        }

        [ApiRoute(FunctionID, 2f, "bom", typeof(Bom))]
        [ApiRoute(FunctionID, 2f, "bom/{itemid}", typeof(Bom))]
        [ApiRoute(FunctionID, 2f, "bom/{itemid}/location/{locationid}", typeof(Bom))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId = null, string locationId = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, locationId));
        }

        [ApiRoute(FunctionID, 2f, "bom", typeof(Bom))]
        [ApiRoute(FunctionID, 2f, "bom/{itemid}", typeof(Bom))]
        [ApiRoute(FunctionID, 2f, "bom/{itemid}/location/{locationid}", typeof(Bom))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId = null, string locationId = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, locationId));
        }

        [ApiRoute(FunctionID, 2f, "bom/{itemid}/location/{locationid}", typeof(Bom))]
        public async Task Delete(string itemId, string locationId)
        {
            await this.MarkToDelete(itemId,locationId);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.PropertyDictionary.Add(BomBase.Columns.BmItemId.ToString(), this.OnBmItemIdPropertyChanged);
        }        
        #endregion

        protected virtual async Task<EntityList<Bom>> Load(string itemId, string locationId)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(itemId) && !Provider.Items.Exists(i => StringHelper.AreEqual(itemId, i.BmItemId, false))))
            {
                if (string.IsNullOrEmpty(locationId))
                {
                    var builder = new SqlFilterBuilder<BomBase.Columns>();
                    builder.AppendEquals(BomBase.Columns.BmItemId, itemId);                    
                    await Provider.Load<Bom>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);
                }
                else
                {
                    var builder = new SqlFilterBuilder<BomBase.Columns>();
                    builder.AppendEquals(BomBase.Columns.BmItemId, itemId);
                    builder.AppendEquals(BomBase.Columns.BmLocId, locationId);                    
                    var list = new BomProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }
            return Provider.Items;
        }

        protected virtual async Task<Bom> Find(string itemId, string locationId)
        {
            var list = await this.Load(itemId, locationId);
            return list?.Find(x => x.BmItemId == itemId && x.BmLocId == locationId);
        }

        protected virtual async Task<List<Bom>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string locationId)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(locationId))
                throw new InvalidValueException("Call is ambiguous. Location ID is provided along with more than one record.");

            var entityList = new List<Bom>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, locationId);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Bom> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string locationId)
        {
            string code = locationId;
            string itemCode = itemId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.BmLocId) || bodyItem.BmLocId == null)
                bodyItem.BmLocId = code;
            else
                code = bodyItem.BmLocId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.BmItemId) || bodyItem.BmItemId == null)
                bodyItem.BmItemId = itemCode;
            else
                itemCode = bodyItem.BmItemId;

            var entity = await this.Find(itemCode, code);

            if (isCreate)
            {
                if (entity != null && !string.IsNullOrEmpty(code))
                    return entity;

                entity = new Bom(this.CompId);
                
            }
            else if (entity == null)
                throw new NothingToProcessException(string.Format("Bill of Material with Item ID '{0}' and Location ID '{1}' does not exist.", itemCode, code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string locationId)
        {
            var entity = await this.Find(itemId, locationId);
            if (entity == null)
                throw new NothingToProcessException(string.Format("The Bill of Material for Item ID '{0}' and Location ID {1} could not be found.", itemId, locationId));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.ParentObject is Bom header)
            {
                if (StringHelper.AreEqual(args.PropertyName, "Components", false))
                {
                    if (header.IsNew)
                        return this.CreateBomDetailComponent(header);
                    else
                        return this.UpdateBomDetailComponent(header, args.ItemModel);
                }
            }
            return null;
        }

        protected virtual object UpdateBomDetailComponent(Bom header, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemId))
                throw new InvalidValueException("Component Item ID is required.");

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId))
                throw new InvalidValueException("Component Location ID is required.");

            string itemId = bodyItem.ItemId;
            string locationId = bodyItem.LocId;

            this.FilterEntityListAsync(header.Components, ApiBOMDetailController.FunctionID);

            BomDetail bomDetail = header.Components.Find(x => StringHelper.AreEqual( x.ItemId, itemId, false) && StringHelper.AreEqual(x.LocId, locationId, false));
            if (bomDetail == null)
                throw new InvalidValueException($"Component with Item Id  '{itemId}' and Location Id '{locationId}' could not be found");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            Request.RegisterForDispose(bomDetail);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return bomDetail;
        }

        protected virtual object CreateBomDetailComponent(Bom header)
        {
            return header.Components.AddNew();
        }        
        #endregion Helper Methods

        #region Event Handlers
        protected virtual void OnBmItemIdPropertyChanged(Bom entity)
        {
            if (entity.IsNew && entity.ReferencedItem != null)
            {
                entity.Description = entity.ReferencedItem.Description;
                entity.Uom = entity.ReferencedItem.UomBase;
            }
        }
        #endregion Event Handlers

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Bom> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Bom);
        }
        #endregion Event Handlers

        #region Properties
        protected BomProvider Provider { get; } = new BomProvider();

        protected SortedDictionary<string, Action<Bom>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Bom>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "8E35AF8E-7737-48A7-8C68-5E5D15CCDC93";
        #endregion
    }
}
