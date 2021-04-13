#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMContainersController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "location/{locationid}/containers/{id?}", typeof(ExtLocationContainer))]
        public async Task<IHttpActionResult> Get(string locationId = null, string id = null)
        {
            return Ok(await this.Load(locationId, id));
        }

        [ApiRoute(FunctionID, 2f, "location/{locationid}/containers/{id?}", typeof(ExtLocationContainer))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string locationId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, locationId, id));
        }

        [ApiRoute(FunctionID, 2f, "location/{locationid}/containers/{id?}", typeof(ExtLocationContainer))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string locationId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, locationId, id));
        }

        [ApiRoute(FunctionID, 2f, "location/{locationid}/containers/{id}", typeof(ExtLocationContainer))]
        public async Task Delete(string locationId, string id)
        {
            await this.MarkToDelete(locationId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.EntityPropertyDictionary.Add(ExtLocationContainer.Columns.ExtLocARef.ToString(), BinPropertyChanged);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            var extLocation = new ExtLocationBin();

            if ((args.Entity as ExtLocationContainer)?.ExtLocARef != null)
                extLocation = EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, null, null)?.Find(x => x.Id == (args.Entity as ExtLocationContainer)?.ExtLocARef);

            if (args.FieldName == "LocId")
                args.ActualValue = extLocation.LocId;

            if (args.FieldName == "ExtLocARef")
                args.ActualValue = extLocation.ExtLocId;
        }
        #endregion

        protected virtual async Task<EntityList<ExtLocationContainer>> Load(string locationId, string id)
        {
            if (Provider.Items.Count <= 0 || !Provider.Items.Exists(i => i.ExtLocId == id))
            {
                var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();

                if (id != null)
                {
                    builder.AppendIsNull(ExtLocationBase.Columns.LocId);
                    builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, id);
                    var list = new ExtLocationContainerProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                    Provider.Items.AddRange(list.FindAll(container => container.ExtLocationBin?.LocId == locationId));
                }
                else
                {                   
                    builder.AppendIsNotNull(ExtLocationBase.Columns.ExtLocARef);
                    var containerList = EntityProvider.GetEntityList<ExtLocationContainer, ExtLocationContainerProvider>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), null);
                    Provider.Items.AddRange(containerList.FindAll(container => container.ExtLocationBin?.LocId == locationId));
                }
                await this.FilterEntityListAsync(Provider.Items, FunctionID);
            }
            return Provider.Items;
        }

        protected virtual async Task<ExtLocationContainer> Find(string locationId, string id)
        {
            var list = await Load(locationId, id);
            return list.Find(x => StringHelper.AreEqual(x.ExtLocId, id, false));
        }

        protected virtual async Task<List<ExtLocationContainer>> ProcessEditRequest(bool isCreate, dynamic bodyItem, string locationId, string id = null)
        {
            object[] list;

            if (bodyItem is object[])
                list = bodyItem;
            else
                list = new object[1] { bodyItem };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Container is provided along with more than one record.");

            var entityList = new List<ExtLocationContainer>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, locationId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);

                await ValidateEntityListAsync(entityList);
            }
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ExtLocationContainer> ProcessBodyItem(bool isCreate, dynamic bodyItem, string locationId, string id)
        {
            string code = ApiUserSkipped.IsApiUserSkipped(bodyItem.ExtLocId) || !string.IsNullOrWhiteSpace(id) ? (bodyItem.ExtLocId = id) : bodyItem.ExtLocId;
            string location = ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId) || !string.IsNullOrWhiteSpace(locationId) ? (bodyItem.LocId = locationId) : bodyItem.LocId;

            var entity = await this.Find(locationId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new ExtLocationContainer(this.CompId);
                entity.Id = this.Provider.GetNextId();
                entity.LocId = location;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Container '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string locationId, string id)
        {
            var containers = await this.Find(locationId, id);

            if (containers == null)
                throw new NothingToProcessException(string.Format("Container '{0}' could not be found.", id));
            else
            {
                this.Provider.Items.Remove(containers);
                this.Provider.Update(this.CompId);
            }
        }

        protected virtual void BinPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is ExtLocationContainer transactionExt)
            {
                args.ActualValue = (EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, null, null)?.
                    Find(x => StringHelper.AreEqual(x.ExtLocId, bodyItem.bin, false) 
                           && StringHelper.AreEqual(x.LocId, (string)bodyItem.LocId)) as ExtLocationBin).Id;
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
            Action<ExtLocationContainer> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ExtLocationContainer);
        }
        #endregion  Event Handlers

        #region Properties
        protected ExtLocationContainerProvider Provider { get; } = new ExtLocationContainerProvider();

        protected SortedDictionary<string, Action<ExtLocationContainer>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ExtLocationContainer>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "50802ece-0c7e-4077-9f8a-cc070215af95";
        #endregion Fields
    }
}
