#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMZonesController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "zones/{zoneid?}", typeof(LocationZone))]
        [ApiRoute(FunctionID, 2f, "zones/{zoneid}/location/{id?}", typeof(LocationZone))]
        public async Task<IHttpActionResult> Get(string zoneId = null, string id = null)
        {            
            return Ok(await Load(zoneId, id));
        }

        [ApiRoute(FunctionID, 2f, "zones/{zoneid?}", typeof(LocationZone))]
        [ApiRoute(FunctionID, 2f, "zones/{zoneid}/location/{id?}", typeof(LocationZone))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string zoneId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, zoneId, id));
        }

        [ApiRoute(FunctionID, 2f, "zones/{zoneid?}", typeof(LocationZone))]
        [ApiRoute(FunctionID, 2f, "zones/{zoneid}/location/{id?}", typeof(LocationZone))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string zoneId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, zoneId, id));
        }
        
        [ApiRoute(FunctionID, 2f, "zones/{zoneid}/location/{id?}", typeof(LocationZone))]
        public async Task Delete(string zoneId, string id)
        {
            await this.MarkToDelete(zoneId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion

        protected virtual async Task<EntityList<LocationZone>> Load(string zoneId, string id)
        {
            if (Provider.Items.Count <= 0
                || (!string.IsNullOrEmpty(zoneId) && !this.Provider.Items.Exists(i => StringHelper.AreEqual(i.ZoneId, zoneId, false)))
                || (!string.IsNullOrEmpty(id) && !this.Provider.Items.Exists(i => StringHelper.AreEqual(i.LocId, id, false))))
            {
                if (string.IsNullOrEmpty(zoneId))
                    await Provider.Load<LocationZone>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<LocationZoneBase.Columns>();
                    builder.AppendEquals(LocationZoneBase.Columns.ZoneId, zoneId);
                    if (!string.IsNullOrEmpty(id))
                        builder.AppendEquals(LocationZoneBase.Columns.LocId, id);
                    var list = new LocationZoneProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }
            return Provider.Items;
        }

        protected virtual async Task<LocationZone> Find(string zoneId, string id)
        {
            var list = await Load(zoneId, id);
            return list.Find(x => StringHelper.AreEqual(x.ZoneId, zoneId, false) && StringHelper.AreEqual(x.LocId, id, false));
        }

        protected virtual async Task<List<LocationZone>> ProcessEditRequest(bool isCreate, dynamic body, string zoneId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Call is ambiguous. Location ID is provided along with more than one record.");

            var entityList = new List<LocationZone>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, zoneId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<LocationZone> ProcessBodyItem(bool isCreate, dynamic bodyItem, string zoneId, string id)
        {
            string zone = zoneId;
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ZoneId) || string.IsNullOrWhiteSpace(bodyItem.ZoneId))
                bodyItem.ZoneId = zone;
            else
                zone = bodyItem.ZoneId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId) || string.IsNullOrWhiteSpace(bodyItem.LocId))
                bodyItem.LocId = code;
            else
                code = bodyItem.LocId;

            if (string.IsNullOrEmpty(code))
                throw new InvalidValueException($"Location ID is required.");

            var entity = await this.Find(zone, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new LocationZone(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException($"Zone ID '{zone}' with Location '{code}' could not be found.");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string zoneId, string id)
        {
            var entity = await this.Find(zoneId, id);

            if (entity == null)
                throw new InvalidValueException($"Zone ID '{zoneId}' with Location '{id}' could not be found.");

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
            Action<LocationZone> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as LocationZone);
        }
        #endregion Event Handlers

        #region Properties
        protected LocationZoneProvider Provider { get; } = new LocationZoneProvider();

        protected SortedDictionary<string, Action<LocationZone>> PropertyDictionary { get; } = new SortedDictionary<string, Action<LocationZone>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "299d8d5b-19db-4cbf-88f2-b1c4094b1f36";
        #endregion Fields
    }
}
