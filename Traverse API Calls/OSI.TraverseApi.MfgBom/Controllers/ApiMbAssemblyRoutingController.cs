#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
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
    public class ApiMbAssemblyRoutingController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "assembly/{assemblyid:int}/routing/{id:int?}", typeof(AssemblyRouting))]
        public async Task<IHttpActionResult> Get(int assemblyId, int? id = null)
        {
            return Ok(await this.Load(assemblyId, id));
        }

        [ApiRoute(FunctionID, 2f, "assembly/{assemblyid:int}/routing/{id:int?}", typeof(AssemblyRouting))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int assemblyId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, assemblyId, id));
        }

        [ApiRoute(FunctionID, 2f, "assembly/{assemblyid:int}/routing", typeof(AssemblyRouting))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int assemblyId)
        {
            return Ok(await ProcessEditRequest(true, body, assemblyId, null));
        }

        [ApiRoute(FunctionID, 2f, "assembly/{assemblyid:int}/routing/{id:int}", typeof(AssemblyRouting))]
        public async Task Delete(int assemblyId, int id)
        {
            await this.MarkToDelete(assemblyId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //Routing Property Changes
            PropertyDictionary.Add(AssemblyRoutingBase.Columns.OperationId.ToString(), (entity) => entity.SetOperationDefaults());
            PropertyDictionary.Add(AssemblyRoutingBase.Columns.SubContractorId.ToString(), (entity) => entity.SetSubcontractDefaults());

            //Batch type only property
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.MaxQuantity.ToString(), ValidateBatchTypeProperty);

            //Subcontract type only properties
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.SubContractorId.ToString(), ValidateSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.SubUnitCost.ToString(), ValidateSubcontractTypeProperty);

            //Non valid 'Subcontract Operation type' properties
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.WorkCenterId.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.SetupLaborTypeId.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.LaborTypeId.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.MachineGroupId.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.Media.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.YieldPct.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.OperatorCount.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.QueueTime.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.QueueTimeIn.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.MachSetup.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.MachSetupIn.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.MachRunTime.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.MachRunTimeIn.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.LaborSetup.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.LaborSetupIn.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.LaborRunTime.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.LaborRunTimeIn.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.WaitTime.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.WaitTimeIn.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.MoveTime.ToString(), ValidateNonSubcontractTypeProperty);
            EntityPropertyDictionary.Add(AssemblyRoutingBase.Columns.MoveTimeIn.ToString(), ValidateNonSubcontractTypeProperty);

            //Detail Property Changes
            DetailPropertyDictionary.Add(AssemblyDetailBase.Columns.ComponentId.ToString(), ComponentIdPropertyChanged);
            DetailPropertyDictionary.Add(AssemblyDetailBase.Columns.LocId.ToString(), (entity) =>
            {
                if (entity.ItemInfo != null)
                    entity.SetCostDefaults();
            });
            DetailPropertyDictionary.Add(AssemblyDetailBase.Columns.UOM.ToString(), (entity) =>
            {
                if (entity.ItemInfo != null)
                    entity.SetCostDefaults();
            });
            DetailPropertyDictionary.Add(AssemblyDetailBase.Columns.DetailType.ToString(), (entity) => entity.SetDescriptionDefaults());
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is AssemblyRouting routing)
            {
                if (StringHelper.AreEqual(args.FieldName, "AssemblyDetailList", false))
                {
                    args.ActualValue = routing.Parent.AssemblyDetailList?.FindAll(x => x.RoutingId == routing.Id);
                }
            }
        }
        #endregion Overrides

        protected virtual async Task<EntityList<AssemblyRouting>> Load(int assemblyId, int? id)
        {
            var list = CurrentAssembly?.AssemblyRoutingList;

            if (CurrentAssembly == null || CurrentAssembly.Id != assemblyId)
            {
                var builder = new SqlFilterBuilder<AssemblyHeaderBase.Columns>();
                builder.AppendEquals(AssemblyHeaderBase.Columns.Id, assemblyId.ToString());
                Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiMbAssemblyHeaderController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Assembly ID '{0}' could not be found.", assemblyId));

                CurrentAssembly = Provider.Items[0];

                list = CurrentAssembly.AssemblyRoutingList;
                await this.FilterEntityListAsync(list, FunctionID);
            }

            if (id.HasValue)
                return list.FindAll(AssemblyRoutingBase.Columns.Id, id.Value);

            return list;
        }

        protected virtual async Task<AssemblyRouting> Find(int assemblyId, int id)
        {
            var list = await Load(assemblyId, id);
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<AssemblyRouting>> ProcessEditRequest(bool isCreate, dynamic body, int assemblyId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Routing ID is provided along with more than one record.");

            var entityList = new List<AssemblyRouting>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, assemblyId, id);
                this.Provider.Items[0].AssemblyRoutingList.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<AssemblyRouting> ProcessBodyItem(bool isCreate, dynamic bodyItem, int assemblyId, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(assemblyId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = this.CurrentAssembly?.AssemblyRoutingList.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Routing ID '{0}' could not be found on Assembly ID '{1}'.", code, assemblyId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int assemblyId, int id)
        {
            var entity = await this.Find(assemblyId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Rounting ID '{0}' could not be found on Assembly ID '{1}'.", id, assemblyId));

            EntityList<AssemblyDetail> detailList = entity.Parent.AssemblyDetailList?.FindAll(x => x.RoutingId == entity.Id);

            if (detailList?.Count > 0)
                throw new InvalidValueException("Unable to delete. Components exist for this routing step.");

            this.Provider.Items[0].AssemblyRoutingList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (StringHelper.AreEqual(args.PropertyName, "AssemblyDetailList", false))
            {
                if (((AssemblyRouting)args.ParentObject).IsNew)
                    return this.CreateDetail((AssemblyRouting)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDetail((AssemblyRouting)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Routing
        protected virtual void ValidateBatchTypeProperty(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is AssemblyRouting routing)
                {
                    if ((RoutingOperationType)routing.OperationType != RoutingOperationType.Batch)
                        e.Handled = true;
                }
            }
        }

        protected virtual void ValidateSubcontractTypeProperty(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is AssemblyRouting routing)
                {
                    if ((RoutingOperationType)routing.OperationType != RoutingOperationType.Subcontract)
                        e.Handled = true;
                }
            }
        }

        protected virtual void ValidateNonSubcontractTypeProperty(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is AssemblyRouting routing)
                {
                    if ((RoutingOperationType)routing.OperationType == RoutingOperationType.Subcontract)
                        e.Handled = true;
                }
            }
        }
        #endregion Routing

        #region Detail
        protected virtual AssemblyDetail UpdateDetail(AssemblyRouting parent, dynamic bodyItem)
        {
            AssemblyDetail entity = parent.Parent.AssemblyDetailList?.Find(x => x.Id == Convert.ToInt32(bodyItem.Id) && x.RoutingId == parent.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Detail ID '{0}' for Routing ID '{1}' AND Assembly ID '{2}' could not be found.",
                    bodyItem.Id, parent.Id, parent.HeaderId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual AssemblyDetail CreateDetail(AssemblyRouting parent, dynamic bodyItem)
        {
            AssemblyDetail entity = parent.Parent.AssemblyDetailList.AddNew();
            entity.RoutingId = parent.Id;
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as AssemblyDetail;
            entity.PropertyChanged -= Detail_PropertyChanged;
        }

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
            Action<AssemblyRouting> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as AssemblyRouting);
        }

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<AssemblyDetail> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as AssemblyDetail);
        }
        #endregion Event Handlers

        #region Properties
        private AssemblyHeaderProvider Provider { get; } = new AssemblyHeaderProvider();

        protected AssemblyHeader CurrentAssembly{ get; set; }

        protected SortedDictionary<string, Action<AssemblyRouting>> PropertyDictionary { get; } = new SortedDictionary<string, Action<AssemblyRouting>>();

        protected SortedDictionary<string, Action<AssemblyDetail>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<AssemblyDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties 

        #region Fields
        public const string FunctionID = "08A3502B-489E-4409-A436-8C2DE72BF311";
        #endregion Fields
    }
}
