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
    public class ApiMpRecordProdActMaterialExtController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{reqid:int}/detail/{seqno:int}/extended/{id:int?}", typeof(MaterialDetailExt))]
        public async Task<IHttpActionResult> Get(string orderNo, int releaseNo, int reqId, int seqNo, int? id = null)
        {
            return Ok(await this.Load(orderNo, releaseNo, reqId, seqNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{reqid:int}/detail/{seqno:int}/extended/{id:int?}", typeof(MaterialDetailExt))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string orderNo, int releaseNo, int reqId, int seqNo, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, orderNo, releaseNo, reqId, seqNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{reqid:int}/detail/{seqno:int}/extended", typeof(MaterialDetailExt))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo, int releaseNo, int reqId, int seqNo)
        {
            return Ok(await ProcessEditRequest(true, body, orderNo, releaseNo, reqId, seqNo, null));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{reqid:int}/detail/{seqno:int}/extended/{id:int}", typeof(MaterialDetailExt))]
        public async Task Delete(string orderNo, int releaseNo, int reqId, int seqNo, int id)
        {
            await this.MarkToDelete(orderNo, releaseNo, reqId, seqNo, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //Material Detail Ext Property Changes
            PropertyDictionary.Add(MaterialDetailExtBase.Columns.LotNum.ToString(), LotNumPropertyChanged);

            //Entity Property Changes
            EntityPropertyDictionary.Add(MaterialDetailExtBase.Columns.LotNum.ToString(), ValidateEntityPropertyChanging);
            EntityPropertyDictionary.Add(MaterialDetailExtBase.Columns.CostUnit.ToString(), ValidateEntityPropertyChanging);
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

        protected virtual async Task<EntityList<MaterialDetailExt>> Load(string orderNo, int releaseNo, int reqId, int seqNo, int? id)
        {
            var list = this.CurrentMaterialDetail?.MaterialDetailExtList;

            if (list == null || this.CurrentMaterial == null || this.CurrentMaterial.Parent.ReqId != reqId)
            {
                await Load(orderNo, releaseNo, reqId);

                this.CurrentMaterial = this.CurrentRequirement?.MaterialSummary;
                this.CurrentMaterialDetail = this.CurrentMaterial?.MaterialDetailList?.Find(MaterialDetailBase.Columns.SeqNo, seqNo);

                if (this.CurrentMaterialDetail == null)
                    throw new InvalidValueException(string.Format("Material Detail {0} with Requirement ID '{1}' and Release No '{2}' could not be found on Order No '{3}'.", 
                        seqNo, reqId, releaseNo, orderNo));

                await this.FilterEntityListAsync(this.CurrentMaterialDetail.MaterialDetailExtList, FunctionID);

                list = this.CurrentMaterialDetail.MaterialDetailExtList;
            }

            if (id.HasValue)
                list = list?.FindAll(MaterialDetailExtBase.Columns.SeqNum, id.Value);

            return list;
        }

        protected virtual async Task<MaterialDetailExt> Find(string orderNo, int releaseNo, int reqId, int seqNo, int id)
        {
            var list = await Load(orderNo, releaseNo, reqId, seqNo, id);
            return list?.Find(x => x.SeqNum == id);
        }

        protected virtual async Task<List<MaterialDetailExt>> ProcessEditRequest(bool isCreate, dynamic body, string orderNo, int releaseNo, int reqId, int seqNo, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new List<MaterialDetailExt>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, orderNo, releaseNo, reqId, seqNo, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<MaterialDetailExt> ProcessBodyItem(bool isCreate, dynamic bodyItem, string orderNo, int releaseNo, int reqId, int seqNo, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum) || bodyItem.SeqNum == null)
                bodyItem.SeqNum = code;
            else
                code = Convert.ToInt32(bodyItem.SeqNum);

            var entity = await this.Find(orderNo, releaseNo, reqId, seqNo, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = this.CurrentMaterialDetail.MaterialDetailExtList.AddNew();
                entity.SetDefaultBin();
                entity.SetDefaultCost();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Extended Record {0} in Material Detail {1} with Requirement ID '{2}' and Release No '{3}' could not be found on Order No '{4}'.",
                        code, seqNo, reqId, releaseNo, orderNo));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string orderNo, int releaseNo, int reqId, int seqNo, int id)
        {
            var entity = await this.Find(orderNo, releaseNo, reqId, seqNo, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Extended Record {0} in Material Detail {1} with Requirement ID '{2}' and Release No '{3}' could not be found on Order No '{4}'.",
                        id, seqNo, reqId, releaseNo, orderNo));

            this.CurrentMaterialDetail.MaterialDetailExtList.Remove(entity);
            this.Provider.Update(this.CompId);
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

        protected virtual void ValidateEntityPropertyChanging(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is MaterialDetailExt lotInfo)
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
            Action<MaterialDetailExt> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as MaterialDetailExt);
        }
        #endregion Event Handlers

        #region Properties
        private OrderProvider Provider { get; } = new OrderProvider();

        protected Order CurrentOrder { get; set; }

        protected OrderReleases CurrentRelease { get; set; }

        protected Requirements CurrentRequirement { get; set; }

        protected MaterialSum CurrentMaterial { get; set; }

        protected MaterialDetail CurrentMaterialDetail { get; set; }

        protected SortedDictionary<string, Action<MaterialDetailExt>> PropertyDictionary { get; } = new SortedDictionary<string, Action<MaterialDetailExt>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties 

        #region Fields
        public const string FunctionID = "6e759195-5a89-421f-a653-3984a8c4e697";
        #endregion Fields
    }
}
