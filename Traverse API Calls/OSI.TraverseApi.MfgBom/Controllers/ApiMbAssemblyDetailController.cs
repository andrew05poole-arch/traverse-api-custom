#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Manufacturing.Controllers
{
    public class ApiMbAssemblyDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "assembly/{assemblyid:int}/routing/{routingid:int}/detail/{id:int?}", typeof(AssemblyRouting))]
        public async Task<IHttpActionResult> Get(int assemblyId, int routingId, int? id = null)
        {
            return Ok(await this.Load(assemblyId, routingId, id));
        }

        [ApiRoute(FunctionID, 2f, "assembly/{assemblyid:int}/routing/{routingid:int}/detail/{id:int?}", typeof(AssemblyRouting))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int assemblyId, int routingId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, assemblyId, routingId, id));
        }

        [ApiRoute(FunctionID, 2f, "assembly/{assemblyid:int}/routing/{routingid:int}/detail", typeof(AssemblyRouting))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int assemblyId, int routingId)
        {
            return Ok(await ProcessEditRequest(true, body, assemblyId, routingId, null));
        }

        [ApiRoute(FunctionID, 2f, "assembly/{assemblyid:int}/routing/{routingid:int}/detail/{id:int}", typeof(AssemblyRouting))]
        public async Task Delete(int assemblyId, int routingId, int id)
        {
            await this.MarkToDelete(assemblyId, routingId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Detail Property Changes
            PropertyDictionary.Add(AssemblyDetailBase.Columns.ComponentId.ToString(), ComponentIdPropertyChanged);
            PropertyDictionary.Add(AssemblyDetailBase.Columns.LocId.ToString(), (entity) =>
            {
                if (entity.ItemInfo != null)
                    entity.SetCostDefaults();
            });
            PropertyDictionary.Add(AssemblyDetailBase.Columns.UOM.ToString(), (entity) =>
            {
                if (entity.ItemInfo != null)
                    entity.SetCostDefaults();
            });
            PropertyDictionary.Add(AssemblyDetailBase.Columns.DetailType.ToString(), (entity) => entity.SetDescriptionDefaults());
        }

        protected virtual async Task<EntityList<AssemblyDetail>> Load(int assemblyId, int routingId, int? id)
        {
            var list = CurrentAssembly?.AssemblyDetailList;

            if (CurrentAssembly == null || CurrentAssembly.Id != assemblyId)
            {
                var builder = new SqlFilterBuilder<AssemblyHeaderBase.Columns>();
                builder.AppendEquals(AssemblyHeaderBase.Columns.Id, assemblyId.ToString());
                Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiMbAssemblyHeaderController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Assembly ID '{0}' could not be found.", assemblyId));

                CurrentAssembly = Provider.Items[0];

                var filteredList = CurrentAssembly.AssemblyDetailList?.FindAll(x => x.RoutingId == routingId);

                await FilterEntityListAsync(filteredList, FunctionID);  
                
                if (filteredList?.Count <= 0)
                    throw new InvalidValueException(string.Format("Routing ID '{0}' could not be found on Assembly ID '{1}'.", routingId, assemblyId));

                list = filteredList;
            }

            if (id.HasValue)
                return list.FindAll(AssemblyRoutingBase.Columns.Id, id.Value);

            return list;
        }

        protected virtual async Task<AssemblyDetail> Find(int assemblyId, int routingId, int id)
        {
            var list = await Load(assemblyId, routingId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<AssemblyDetail>> ProcessEditRequest(bool isCreate, dynamic body, int assemblyId, int routingId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Detail ID is provided along with more than one record.");

            var entityList = new List<AssemblyDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, assemblyId, routingId, id);
                this.Provider.Items[0].AssemblyDetailList.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<AssemblyDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, int assemblyId, int routingId, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(assemblyId, routingId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;
                
                entity = this.CurrentAssembly?.AssemblyDetailList?.AddNew();
                entity.RoutingId = routingId;
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Detail ID '{0}' could not be found on Routing ID '{1}' and Assembly ID '{2}'.", code, routingId, assemblyId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int assemblyId, int routingId, int id)
        {
            var entity = await this.Find(assemblyId, routingId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Rounting ID '{0}' could not be found on Routing ID '{1}' and  Assembly ID '{2}'.", id, routingId, assemblyId));

            this.Provider.Items[0].AssemblyDetailList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        #region Detail
        protected virtual void ComponentIdPropertyChanged(AssemblyDetail entity)
        {
            if (entity.ItemInfo != null)
            {
                entity.SetItemDefaults();
                if (entity.ItemInfo.InventoryStatus == InventoryStatus.Obsolete)
                {
                    throw new InvalidValueException("The selected item has a status of Obsolete.");
                }
                else if (entity.ItemInfo.InventoryStatus == InventoryStatus.Discontinued)
                {
                    throw new InvalidValueException("The selected item has a status of Discontinued.");
                }
                else if (entity.ItemInfo.AllLocations.Count < 1)
                {
                    entity.ComponentId = null;
                    throw new InvalidValueException("The item is not setup at any locations.");
                }
                else if (entity.ItemInfo.InventoryStatus == InventoryStatus.Superseded)
                {
                    entity.ComponentId = entity.ItemInfo.SuperId;
                    entity.SetItemDefaults();
                }
            }
            else if (!string.IsNullOrEmpty(entity.ComponentId))
            {
                this.AddWarnings(string.Format("Item ID '{0}' does not exist in Inventory.", entity.ComponentId));
                entity.SetItemDefaults();
            }
            entity.SetDescriptionDefaults();
        }
        #endregion Detail
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
            Action<AssemblyDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as AssemblyDetail);
        }
        #endregion Event Handlers

        #region Properties
        private AssemblyHeaderProvider Provider { get; } = new AssemblyHeaderProvider();

        protected AssemblyHeader CurrentAssembly { get; set; }

        protected SortedDictionary<string, Action<AssemblyDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<AssemblyDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties 

        #region Fields
        public const string FunctionID = "444CB952-4541-4AD5-8ABB-8F60E2EDF03B";
        #endregion Fields
    }
}
