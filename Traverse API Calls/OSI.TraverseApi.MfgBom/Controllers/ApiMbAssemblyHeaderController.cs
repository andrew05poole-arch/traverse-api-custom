#region Using Directives
using OSI.TraverseApi.Business;
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
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMbAssemblyHeaderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "assembly/{id:int?}", typeof(AssemblyHeader))]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "assembly/{id:int?}", typeof(AssemblyHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "assembly", typeof(AssemblyHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "assembly/{id:int}", typeof(AssemblyHeader))]
        public async Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //Header Property Changes
            PropertyDictionary.Add(AssemblyHeaderBase.Columns.DfltRevYn.ToString(), DfltRevYnPropertyChanged);
            PropertyDictionary.Add(AssemblyHeaderBase.Columns.AssemblyId.ToString(), (entity) =>
            {
                if (entity.ItemInfo != null)
                    entity.SetItemDefaults();
            });

            //Routing Property Changes
            RoutingPropertyDictionary.Add(AssemblyRoutingBase.Columns.OperationId.ToString(), (entity) => entity.SetOperationDefaults());
            RoutingPropertyDictionary.Add(AssemblyRoutingBase.Columns.SubContractorId.ToString(), (entity) => entity.SetSubcontractDefaults());

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

        protected virtual async Task<EntityList<AssemblyHeader>> Load(int? id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => id == i.Id)))
            {
                if (id == null)
                    await Provider.Load<AssemblyHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<AssemblyHeaderBase.Columns>();
                    builder.AppendEquals(AssemblyHeaderBase.Columns.Id, id.ToString());
                    var list = new AssemblyHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<AssemblyHeader> Find(int id)
        {
            var list = await Load(id);
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<AssemblyHeader>> ProcessEditRequest(bool isCreate, dynamic body, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new List<AssemblyHeader>();
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

        protected virtual async Task<AssemblyHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = new AssemblyHeader(this.CompId);
                entity.SetDefaults();
                entity.DfltRevYn = false;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            //Verify is Header has no routing list. Default one if necessary
            if (entity.AssemblyRoutingList?.Count <= 0)
            {
                AssemblyRouting routing = entity.AssemblyRoutingList.AddNew();
                routing.SetDefaults();
                routing.Description = "Default";
            }

            return entity;
        }

        protected virtual async Task MarkToDelete(int id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (StringHelper.AreEqual(args.PropertyName, "AssemblyRoutingList", false))
            {
                if (((AssemblyHeader)args.ParentObject).IsNew)
                    return this.CreateRouting((AssemblyHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateRouting((AssemblyHeader)args.ParentObject, args.ItemModel);
            }
            else if (StringHelper.AreEqual(args.PropertyName, "AssemblyDetailList", false))
            {
                if (((AssemblyRouting)args.ParentObject).IsNew)
                    return this.CreateDetail((AssemblyRouting)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDetail((AssemblyRouting)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Header
        protected virtual void DfltRevYnPropertyChanged(AssemblyHeader entity)
        {
            if (entity.DfltRevYn)
            {
                EntityList<AssemblyHeader> assemblyHeaderList = Provider?.Items.FindAll(x => x.DfltRevYn == true && x.AssemblyId == entity.AssemblyId);

                if (assemblyHeaderList?.Count == 0)
                {
                    AssemblyHeaderProvider provider = new AssemblyHeaderProvider();
                    SqlFilterBuilder<AssemblyHeaderBase.Columns> builder = new SqlFilterBuilder<AssemblyHeaderBase.Columns>();
                    builder.AppendEquals(AssemblyHeaderBase.Columns.DfltRevYn, true.ToString());
                    builder.AppendEquals(AssemblyHeaderBase.Columns.AssemblyId, entity.AssemblyId);

                    provider.CompId = this.CompId;
                    assemblyHeaderList = provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));
                }

                if (assemblyHeaderList?.Count > 0)
                {
                    foreach (AssemblyHeader item in assemblyHeaderList)
                    {
                        if (entity.IsNew || (!entity.IsNew && entity.Id != item.Id))
                        {
                            item.DfltRevYn = false;
                            if (!Provider.Items.Contains(item))
                                Provider.Items.Add(item);
                        }
                    }
                }
            }
        }
        #endregion Header

        #region Routing
        protected virtual AssemblyRouting UpdateRouting(AssemblyHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Routing ID is required.");

            AssemblyRouting entity = parent.AssemblyRoutingList?.Find(x => x.Id == Convert.ToInt32(bodyItem.Id));
            if (entity == null)
                throw new InvalidValueException(string.Format("Routing ID '{0}' for Assembly ID '{1}' could not be found.",
                    bodyItem.Id, parent.Id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = RoutingUpdateComplete;
            entity.PropertyChanged += Routing_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual AssemblyRouting CreateRouting(AssemblyHeader parent, dynamic bodyItem)
        {
            AssemblyRouting entity = parent.AssemblyRoutingList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = RoutingUpdateComplete;
            entity.PropertyChanged += Routing_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void RoutingUpdateComplete(object entityObject)
        {
            var entity = entityObject as AssemblyRouting;
            entity.PropertyChanged -= Routing_PropertyChanged;
        }

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
            Action<AssemblyHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as AssemblyHeader);
        }

        private void Routing_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<AssemblyRouting> action = null;
            if (RoutingPropertyDictionary.TryGetValue(e.PropertyName, out action))
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

        protected SortedDictionary<string, Action<AssemblyHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<AssemblyHeader>>();
        
        protected SortedDictionary<string, Action<AssemblyRouting>> RoutingPropertyDictionary { get; } = new SortedDictionary<string, Action<AssemblyRouting>>();

        protected SortedDictionary<string, Action<AssemblyDetail>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<AssemblyDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties 

        #region Fields
        public const string FunctionID = "B6295F0F-6B7E-497A-80BC-7317BA50D4AF";
        #endregion Fields
    }
}
