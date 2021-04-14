#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMpRecordProdActMaterialController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{reqid:int}/detail/{id:int?}", typeof(MaterialDetail))]
        public async Task<IHttpActionResult> Get(string orderNo, int releaseNo, int reqId, int? id = null)
        {
            return Ok(await this.Load(orderNo, releaseNo, reqId, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{reqid:int}/detail/{id:int?}", typeof(MaterialDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string orderNo, int releaseNo, int reqId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, orderNo, releaseNo, reqId, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{reqid:int}/detail", typeof(MaterialDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo, int releaseNo, int reqId)
        {
            return Ok(await ProcessEditRequest(true, body, orderNo, releaseNo, reqId, null));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{reqid:int}/detail/{id:int}", typeof(MaterialDetail))]
        public async Task Delete(string orderNo, int releaseNo, int reqId, int id)
        {
            await this.MarkToDelete(orderNo, releaseNo, reqId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //Material Detail Property Changes
            PropertyDictionary.Add(MaterialDetailBase.Columns.TransDate.ToString(), TransDatePropertyChanged);
            PropertyDictionary.Add(MaterialDetailBase.Columns.GlPeriod.ToString(), FiscalPeriodPropertyChanged);
            PropertyDictionary.Add(MaterialDetailBase.Columns.FiscalYear.ToString(), FiscalPeriodPropertyChanged);
            PropertyDictionary.Add(MaterialDetailBase.Columns.SubAssemblyTranType.ToString(), (entity) =>
            {
                entity.SetCostDefaults();
            });
            PropertyDictionary.Add(MaterialDetailBase.Columns.ComponentId.ToString(), ComponentIdPropertyChanged);
            PropertyDictionary.Add(MaterialDetailBase.Columns.LocId.ToString(), (entity) =>
            {
                if (entity.ItemInfo != null && !string.IsNullOrEmpty(entity.LocId))
                    entity.SetCostDefaults();
            });
            PropertyDictionary.Add(MaterialDetailBase.Columns.UOM.ToString(), (entity) =>
            {
                if (entity.ItemInfo != null && !string.IsNullOrEmpty(entity.UOM))
                    entity.SetCostDefaults();
            });
            PropertyDictionary.Add(MaterialDetailBase.Columns.Qty.ToString(), QtyPropertyChanged);

            //Material Detail Ext Property Changes
            MaterialDetailExtPropertyDictionary.Add(MaterialDetailExtBase.Columns.LotNum.ToString(), LotNumPropertyChanged);

            //Material Ser Property Changes
            MaterialSerPropertyDictionary.Add(MaterialSerBase.Columns.SerNum.ToString(), SerNumPropertyChanged);
            MaterialSerPropertyDictionary.Add(MaterialSerBase.Columns.LotNum.ToString(), LotNumPropertyChanged);

            //Entity Property Changes
            EntityPropertyDictionary.Add(MaterialDetailBase.Columns.ComponentId.ToString(), ValidateEntityPropertyChanging);
            //Next ones work for both types, LotNum Extended and Serial
            EntityPropertyDictionary.Add(MaterialSerBase.Columns.CostUnit.ToString(), ValidateEntityPropertyChanging);
            EntityPropertyDictionary.Add(MaterialSerBase.Columns.LotNum.ToString(), ValidateEntityPropertyChanging);
        }
        #endregion Overrides

        protected virtual async Task Load(string orderNo, int releaseNo, int reqId)
        {
            var list = this.CurrentOrder?.DetailList as EntityList<OrderReleases>;

            if (this.CurrentOrder == null || !StringHelper.AreEqual(this.CurrentOrder.OrderNo, orderNo, false))
            {
                var builder = new SqlFilterBuilder<OrderBase.Columns>();
                builder.AppendEquals(OrderBase.Columns.OrderNo, orderNo);
                this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items);
                
                if (this.Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Order No '{0}' could not be found.", orderNo));

                this.CurrentOrder = Provider.Items[0];

                list = this.CurrentOrder.DetailList as EntityList<OrderReleases>;
                await this.FilterEntityListAsync(list);
            }

            this.CurrentRelease = list?.Find(OrderReleasesBase.Columns.ReleaseNo, releaseNo);

            if (this.CurrentRelease == null)
                throw new InvalidValueException(string.Format("Release No '{0}' could not be found on Order No '{1}'.", releaseNo, orderNo));

            this.CurrentRequirement = this.CurrentRelease?.RequirementList?.Find(RequirementsBase.Columns.ReqId, reqId);

            if (this.CurrentRequirement == null)
                throw new InvalidValueException(string.Format("Requirement ID '{0}' with Release No '{1}' could not be found on Order No '{2}'.", reqId, releaseNo, orderNo));
        }

        protected virtual async Task<EntityList<MaterialDetail>> Load(string orderNo, int releaseNo, int reqId, int? id)
        {
            var list = this.CurrentMaterial?.MaterialDetailList as EntityList<MaterialDetail>;

            if (this.CurrentMaterial == null || this.CurrentMaterial.Parent.ReqId != reqId)
            {
                await Load(orderNo, releaseNo, reqId);

                this.CurrentMaterial = this.CurrentRequirement?.MaterialSummary;
                list = this.CurrentMaterial?.MaterialDetailList;
            }

            if (list != null)
                await this.FilterEntityListAsync(list, FunctionID);

            if (id.HasValue)
                list = list?.FindAll(MaterialDetailBase.Columns.SeqNo, id.Value);

            return list;
        }

        protected virtual async Task<MaterialDetail> Find(string orderNo, int releaseNo, int reqId, int id)
        {
            var list = await Load(orderNo, releaseNo, reqId, id);
            return list?.Find(x => x.SeqNo== id);
        }

        protected virtual async Task<List<MaterialDetail>> ProcessEditRequest(bool isCreate, dynamic body, string orderNo, int releaseNo, int reqId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new List<MaterialDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, orderNo, releaseNo, reqId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<MaterialDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string orderNo, int releaseNo, int reqId, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNo) || bodyItem.SeqNo == null)
                bodyItem.SeqNo = code;
            else
                code = Convert.ToInt32(bodyItem.SeqNo);

            var entity = await this.Find(orderNo, releaseNo, reqId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentMaterial?.MaterialDetailList?.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Sequence No '{0}' could not be found on Requirement ID '{1}' with Release No '{2}' on Order No '{3}'.", 
                    code, reqId, releaseNo, orderNo));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string orderNo, int releaseNo, int reqId, int id)
        {
            var entity = await this.Find(orderNo, releaseNo, reqId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Sequence No '{0}' could not be found on Requirement ID '{1}' with Release No '{2}' on Order No '{3}'.",
                    id, reqId, releaseNo, orderNo));

            this.CurrentMaterial?.MaterialDetailList?.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            var entity = (MaterialDetail)args.ParentObject;

            if (args.PropertyName == "MaterialDetailExtList")
            {
                if (entity.ItemInfo == null)
                {
                    args.Ignore = true;
                    return null;
                }

                if (!entity.ItemInfo.IsLotted || entity.ItemInfo.InventoryType == InventoryType.Serial)
                    throw new InvalidValueException("Component ID is not an extended item and cannot be processed as one.");

                if (entity.IsNew)
                    return this.CreateMaterialDetailExt(entity, args.ItemModel);
                else
                    return this.UpdateMaterialDetailExt(entity, args.ItemModel);
            }
            else if (args.PropertyName == "MaterialSerList")
            {
                if (entity.ItemInfo == null)
                {
                    args.Ignore = true;
                    return null;
                }

                if (entity.ItemInfo.InventoryType != InventoryType.Serial)
                    throw new InvalidValueException("Component ID is not a serialized item and cannot be processed as one.");

                if (entity.IsNew)
                    return this.CreateMaterialSer(entity, args.ItemModel);
                else
                    return this.UpdateMaterialSer(entity, args.ItemModel);
            }

            return null;
        }

        #region Material Detail
        protected virtual void TransDatePropertyChanged(MaterialDetail entity)
        {
            if (entity.TransDate != null)
            {
                DateTime date = entity.TransDate.GetValueOrDefault();

                short period = (short)date.Month;
                short year = (short)date.Year;

                PeriodConversion fiscalPeriod = PeriodConversion.GetFiscalPeriod(period, year);
                if (fiscalPeriod != null)
                {
                    if (fiscalPeriod.ClosedGL.Value)
                        AddWarnings(string.Format("Period {0} is closed for fiscal year {1}.", period, year));
                }
                entity.SetFiscalPeriodYearFromDate(entity.TransDate.Value);
            }
        }

        protected virtual void FiscalPeriodPropertyChanged(MaterialDetail entity)
        {
            PeriodConversion fiscalPeriod = PeriodConversion.GetFiscalPeriod(entity.GlPeriod, entity.FiscalYear);
            if (fiscalPeriod != null)
            {
                if (fiscalPeriod.ClosedGL.Value)
                    AddWarnings(string.Format("Period {0} is closed for fiscal year {1}.", entity.GlPeriod, entity.FiscalYear));
            }
        }

        protected virtual void ComponentIdPropertyChanged(MaterialDetail entity)
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
            entity.SetItemDefaults();
        }

        protected virtual void QtyPropertyChanged(MaterialDetail entity)
        {
            ProductionComponentType productionComponentType = entity.Parent.ProductionComponentType;
            if (productionComponentType == ProductionComponentType.Material && entity.ItemInfo != null && entity.ItemInfo.InventoryType != InventoryType.Service)
            {
                decimal num = entity.ItemInfo.GetQuantityOnHand();
                decimal conversionFactor = entity.ItemInfo.Units.Find(ItemUnitBase.Columns.Uom, entity.UOM).ConversionFactor;
                num = Rounding.Round(new decimal?(num / conversionFactor), Utility.PrecQty);
                if (num < entity.Qty)
                {
                    this.AddWarnings(string.Format("Only {0} available. Adjustment will cause quantity to go negative.", 
                        Rounding.Round(new decimal?(num), ConfigurationValueProvider.GetRule<int>("SM", "PrecQty", this.CompId))));
                }
            }
            if (entity.ItemInfo != null && entity.ItemInfo.IsQuantityTracked && entity.ItemInfo.InventoryType != InventoryType.Serial 
                && !entity.ItemInfo.IsLotted && entity.MaterialDetailExtList != null && entity.MaterialDetailExtList.Count == 1)
            {
                entity.MaterialDetailExtList[0].QtyFilled = entity.Qty;
            }
        }
        #endregion Material Detail

        #region Material Detail Ext
        protected virtual MaterialDetailExt UpdateMaterialDetailExt(MaterialDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Extended Sequence No is required.");

            this.FilterEntityList(parent.MaterialDetailExtList, ApiMpRecordProdActMaterialExtController.FunctionID);

            MaterialDetailExt entity = parent.MaterialDetailExtList?.Find(x => x.SeqNum == Convert.ToInt32(bodyItem.SeqNum));
            if (entity == null)
                throw new InvalidValueException(string.Format("Extended Sequence No '{0}' for Requirement ID '{1}' could not be found.",
                    bodyItem.SeqNum, parent.Parent?.Parent?.ReqId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = MaterialDetailExtUpdateComplete;
            entity.PropertyChanged += MaterialDetailExt_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual MaterialDetailExt CreateMaterialDetailExt(MaterialDetail parent, dynamic bodyItem)
        {
            MaterialDetailExt entity = parent.MaterialDetailExtList.AddNew();
            entity.SetDefaultBin();
            entity.SetDefaultCost();
                
            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = MaterialDetailExtUpdateComplete;
            entity.PropertyChanged += MaterialDetailExt_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual Lot CreateBrandNewLot(string itemId, string locId, string lotNum)
        {
            Lot newLot = new Lot(this.CompId)
            {
                LotNum = lotNum,
                LocId = locId,
                ItemId = itemId,
                InitialDate = DateTime.Today
            };
            return newLot;
        }

        protected virtual void LotNumPropertyChanged(MaterialDetailExt entity)
        {
            if (entity.ItemInfo != null)
            {
                if (string.IsNullOrEmpty(entity.LotNum)
                || entity.ItemInfo == null
                || !entity.ItemInfo.IsLotted)
                    return;

                Lot lot = entity.ItemInfo?.AllLocations?.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                    .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNum, true);

                if (entity.IsNew && lot == null)
                {
                    //OUTWARDS
                    if (!string.IsNullOrEmpty(entity.LotNum)
                        && (entity.Parent.Parent.ProductionComponentType == ProductionComponentType.Material
                            || (entity.Parent.Parent.ProductionComponentType == ProductionComponentType.Subassembly
                                && entity.Parent.SubAssemblyTransactionType == SubAssemblyTransactionType.PulledFromStock)))
                    {
                        throw new InvalidValueException(string.Format("'{0}' is not on file.", entity.LotNum));
                    }
                    else
                    {
                        entity.NewLot = this.CreateBrandNewLot(entity.ItemInfo.ItemId, entity.LocId, entity.LotNum);
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(entity.LotNum))
                    entity.SetDefaultCost();

                if (entity.NewLot == null && entity.LotNum != null)
                {
                    if (lot != null && lot.ExpDate < entity.Parent.TransDate)
                        this.AddWarnings("Lot is expired.");
                }
            }
        }

        protected virtual void MaterialDetailExtUpdateComplete(object entityObject)
        {
            var entity = entityObject as MaterialDetailExt;
            entity.PropertyChanged -= MaterialDetailExt_PropertyChanged;
        }
        #endregion Material Detail Ext

        #region Material Serial
        protected virtual MaterialSer UpdateMaterialSer(MaterialDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Serial Sequence No is required.");

            this.FilterEntityList(parent.MaterialSerList, ApiMpRecordProdActMaterialSerController.FunctionID);

            MaterialSer entity = parent.MaterialSerList?.Find(x => x.SeqNum == Convert.ToInt32(bodyItem.SeqNum));
            if (entity == null)
                throw new InvalidValueException(string.Format("Serial Sequence No '{0}' for Requirement ID '{1}' could not be found.",
                    bodyItem.SeqNum, parent.Parent?.Parent?.ReqId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = MaterialSerUpdateComplete;
            entity.PropertyChanged += MaterialSer_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual MaterialSer CreateMaterialSer(MaterialDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            MaterialSer entity = parent.MaterialSerList?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' for Requierement ID '{1}' already exists.",
                    bodyItem.SerNum, parent.Parent?.Parent?.ReqId));

            entity = parent.MaterialSerList?.AddNew();
            entity.SetDefaultBin();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = MaterialSerUpdateComplete;
            entity.PropertyChanged += MaterialSer_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void SerNumPropertyChanged(MaterialSer entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum)
                || entity.ItemInfo == null
                || entity.ItemInfo.InventoryType != InventoryType.Serial)
                return;

            var serial = entity.ItemInfo.AllLocations?.Find(ItemLocationBase.Columns.LocId, entity.Parent.LocId, true)?
                .SerialItems?.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            if (string.IsNullOrEmpty(entity.LotNum))
                entity.LotNum = serial?.LotNum;

            if (entity.Parent != null)
            {
                if (!string.IsNullOrEmpty(entity.SerNum))
                {
                    //INWARDS
                    if (entity.Parent?.Parent?.ProductionComponentType == ProductionComponentType.ByProduct
                        || (entity.Parent?.Parent?.ProductionComponentType == ProductionComponentType.Subassembly
                            && entity.Parent?.SubAssemblyTransactionType == SubAssemblyTransactionType.MovedToStock))
                    {
                        if (serial != null && !string.IsNullOrEmpty(entity.SerNum))
                        {
                            entity.SerNum = null;
                            throw new InvalidValueException(string.Format("Serial Number already exists for this item."));
                        }
                        else if (serial == null && !string.IsNullOrEmpty(entity.SerNum))
                        {
                            entity.SetDefaults();
                        }
                    }
                    //OUTWARDS
                    else if (entity.Parent?.Parent?.ProductionComponentType == ProductionComponentType.Material
                             || (entity.Parent?.Parent?.ProductionComponentType == ProductionComponentType.Subassembly
                                 && entity.Parent?.SubAssemblyTransactionType == SubAssemblyTransactionType.PulledFromStock))
                    {
                        if (serial != null)
                        {
                            if (serial.SerialItemStatus == SerialItemStatus.Available)
                            {
                                serial.SerialItemStatus = SerialItemStatus.Sold;
                                entity.SetDefaults();
                            }
                            else
                            {
                                entity.SerNum = null;
                                throw new InvalidValueException(string.Format("Serial Number must have a status of Available to be sold."));
                            }
                        }
                        else
                        {
                            entity.SerNum = null;
                            throw new InvalidValueException("Serial number is not on file.");
                        }
                    }
                }
            }
            else if (serial == null)
                entity.SetDefaults();
            
            if (!string.IsNullOrEmpty(entity.SerNum))
                entity.Parent.Calculate();

            return;
        }

        protected virtual void LotNumPropertyChanged(MaterialSer entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || entity.ItemInfo == null
                || !entity.ItemInfo.IsLotted)
                return;

            Lot lot = entity.ItemInfo?.AllLocations?.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                 .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && lot == null)
            {
                //OUTWARDS
                if (!string.IsNullOrEmpty(entity.LotNum)
                    && (entity.Parent.Parent.ProductionComponentType == ProductionComponentType.Material
                        || (entity.Parent.Parent.ProductionComponentType == ProductionComponentType.Subassembly
                        && entity.Parent.SubAssemblyTransactionType == SubAssemblyTransactionType.PulledFromStock)))
                {
                    throw new InvalidValueException(string.Format("'{0}' is not on file.", entity.LotNum));
                }
                else
                {
                    entity.NewLot = this.CreateBrandNewLot(entity.ItemInfo.ItemId, entity.LocId, entity.LotNum);
                    return;
                }
            }
            if (!string.IsNullOrEmpty(entity.LotNum))
                entity.SetDefaultCost();

            if (entity.NewLot == null && entity.LotNum != null)
            {
                if (lot != null && lot.ExpDate < entity.Parent.TransDate)
                    this.AddWarnings("Lot is expired.");
            }
        }

        protected virtual void ValidateEntityPropertyChanging(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is MaterialSer serialInfo)
                {
                    //OUTWARDS --Handles Cost/Lot
                    if (serialInfo.Parent?.Parent?.ProductionComponentType == ProductionComponentType.Material
                             || (serialInfo.Parent?.Parent?.ProductionComponentType == ProductionComponentType.Subassembly
                                 && serialInfo.Parent?.SubAssemblyTransactionType == SubAssemblyTransactionType.PulledFromStock))
                    {
                        e.Handled = true;
                        return;
                    }
                }
                else if (e.Entity is MaterialDetailExt lotInfo)
                {
                    if (!lotInfo.ItemInfo.IsLotted)
                    {
                        e.Handled = true;
                        return;
                    }

                    //OUTWARDS --Handles cost
                    if (StringHelper.AreEqual(e.FieldName?.ToString(), MaterialDetailExtBase.Columns.CostUnit.ToString(), false))
                    {
                        if (lotInfo.Parent?.Parent?.ProductionComponentType == ProductionComponentType.Material
                              || (lotInfo.Parent?.Parent?.ProductionComponentType == ProductionComponentType.Subassembly
                                  && lotInfo.Parent?.SubAssemblyTransactionType == SubAssemblyTransactionType.PulledFromStock))
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
                else if (e.Entity is MaterialDetail detailInfo)
                {
                    //Assembly --Handles ComponentId
                    if (detailInfo.Parent?.Parent?.IndLevel == 0)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        protected virtual void MaterialSerUpdateComplete(object entityObject)
        {
            var entity = entityObject as MaterialSer;
            entity.PropertyChanged -= MaterialSer_PropertyChanged;
        }
        #endregion Material Serial
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
            Action<MaterialDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as MaterialDetail);
        }

        private void MaterialDetailExt_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<MaterialDetailExt> action = null;
            if (MaterialDetailExtPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as MaterialDetailExt);
        }

        private void MaterialSer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<MaterialSer> action = null;
            if (MaterialSerPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as MaterialSer);
        }
        #endregion Event Handlers

        #region Properties
        private OrderProvider Provider { get; } = new OrderProvider();

        protected Order CurrentOrder { get; set; }

        protected OrderReleases CurrentRelease { get; set; }

        protected Requirements CurrentRequirement { get; set; }

        protected MaterialSum CurrentMaterial { get; set; }

        protected SortedDictionary<string, Action<MaterialDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<MaterialDetail>>();

        protected SortedDictionary<string, Action<MaterialDetailExt>> MaterialDetailExtPropertyDictionary { get; } = new SortedDictionary<string, Action<MaterialDetailExt>>();

        protected SortedDictionary<string, Action<MaterialSer>> MaterialSerPropertyDictionary { get; } = new SortedDictionary<string, Action<MaterialSer>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties 

        #region Fields
        public const string FunctionID = "2ea6d076-b0ad-468e-82c1-1dd683a0526a";
        #endregion Fields
    }
}
