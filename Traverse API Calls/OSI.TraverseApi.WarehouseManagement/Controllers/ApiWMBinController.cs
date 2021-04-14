#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WM;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMBinController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "location/{locationid}/bins/{id?}", typeof(ExtLocationBin))]
        public async Task<IHttpActionResult> Get(string locationId = null, string id = null)
        {
            return Ok(await Load(locationId,id));
        }

        [ApiRoute(FunctionID, 2f, "location/{locationid}/bins/{id?}", typeof(ExtLocationBin))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string locationId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, locationId, id));
        }

        [ApiRoute(FunctionID, 2f, "location/{locationid}/bins/{id?}", typeof(ExtLocationBin))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string locationId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body,locationId, id));
        }

        [ApiRoute(FunctionID, 2f, "location/{locationid}/bins/{id}", typeof(ExtLocationBin))]
        public async Task Delete(string locationId, string id)
        {
            await this.MarkToDelete(locationId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.EntityPropertyDictionary.Add(ExtLocationBin.Columns.ExtLocTypeId.ToString(), ExtLocationTypePropertyChanged);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            var extLocationType = new ExtLocationType();

            if ((args.Entity as ExtLocationBin)?.ExtLocTypeId != null)
                extLocationType = EntityProvider.GetEntityList<ExtLocationType, ExtLocationTypeProvider>(this.CompId, null, null)?.Find(x => x.Id == (args.Entity as ExtLocationBin)?.ExtLocTypeId);

            if (args.FieldName == "ExtLocTypeId")
                args.ActualValue = extLocationType.Description;
        }
        #endregion  Overrides

        protected virtual async Task<EntityList<ExtLocationBin>> Load(string locationId, string id)
        {
            if (Provider.Items.Count <= 0 || !Provider.Items.Exists(i => i.ExtLocId == id))
            {
                var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();
                builder.AppendEquals(ExtLocationBase.Columns.LocId, locationId);

                if (id == null)
                    Provider.Load(this.CompId, new FilterCriteria(builder.ToString(),string.Empty));
                else
                {              
                    builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, id);
                    var list = new ExtLocationBinProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<ExtLocationBin> Find(string locationId, string id)
        {
            var list = await Load(locationId, id);
            return list.Find(x => StringHelper.AreEqual(x.ExtLocId, id, false));
        }

        protected virtual async Task<List<ExtLocationBin>> ProcessEditRequest(bool isCreate, dynamic body, string locationId, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Bin ID is provided along with more than one record.");

            var entityList = new List<ExtLocationBin>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, locationId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<dynamic> ProcessBodyItem(bool isCreate, dynamic bodyItem,string locationId, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ExtLocId) || bodyItem.ExtLocId == null)
                bodyItem.ExtLocId = code;
            else
                code = bodyItem.ExtLocId;

            var entity = await this.Find(locationId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new ExtLocationBin(this.CompId);
                entity.Id = Provider.GetNextId();
                entity.LocId = locationId;
                
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Bin ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string locationId, string id)
        {
            var entity = await this.Find(locationId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Bin ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void ExtLocationTypePropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            var builder = new SqlFilterBuilder<ExtLocationTypeBase.Columns>();
            builder.AppendEquals(ExtLocationTypeBase.Columns.Description, bodyItem.ExtLocTypeId);
            LocationTypeProvider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (LocationTypeProvider.Items.Count > 0)
                args.ActualValue = LocationTypeProvider.Items[0].Id;
            else
                throw new NothingToProcessException(string.Format("Invalid Bin Type"));
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
            var entity = sender as ExtLocationBin;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<ExtLocationBin> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }
        #endregion Event Handlers

        #region Properties
        protected ExtLocationBinProvider Provider { get; } = new ExtLocationBinProvider();
        protected ExtLocationTypeProvider LocationTypeProvider { get; } = new ExtLocationTypeProvider();
        protected SortedDictionary<string, Action<ExtLocationBin>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ExtLocationBin>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "37fa9163-a68b-4a8e-901b-e30dee2c3d63";
        #endregion Fields
    }
}
