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
    public class ApiMpEditReleaseMaterialController : ApiMpEditReleaseBase<MaterialSum>
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{id:int?}", typeof(MaterialSum))]
        public async Task<IHttpActionResult> Get(string orderNo, int releaseNo, int? id = null)
        {
            return Ok(await this.Load(orderNo, releaseNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{id:int?}", typeof(MaterialSum))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string orderNo, int releaseNo, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, orderNo, releaseNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material", typeof(MaterialSum))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo, int releaseNo)
        {
            return Ok(await ProcessEditRequest(true, body, orderNo, releaseNo, null));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/material/{id:int}", typeof(MaterialSum))]
        public async Task Delete(string orderNo, int releaseNo, int id)
        {
            await this.MarkToDelete(orderNo, releaseNo, id);
        }
        #endregion Web Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(MaterialSumBase.Columns.ComponentId.ToString(), ComponentIdPropertyChanged);
            PropertyDictionary.Add(MaterialSumBase.Columns.ComponentType.ToString(), ComponentTypePropertyChanged);
            PropertyDictionary.Add(MaterialSumBase.Columns.UOM.ToString(), UomPropertyChanged);
            PropertyDictionary.Add(MaterialSumBase.Columns.LocId.ToString(), LocIdPropertyChanged);
            PropertyDictionary.Add(MaterialSumBase.Columns.EstQtyRequired.ToString(), QtyPropertyChanged);
        }

        protected override async Task<EntityList<MaterialSum>> Load(string orderNo, int releaseNo, int? id)
        {
            var release = await LoadOrderRelease(orderNo, releaseNo);
            if (release == null || release.RequirementList == null)
                throw new InvalidValueException(string.Format("Release No '{0}' could not be found on Order No '{1}'.", releaseNo, orderNo));

            var materialList = new EntityList<MaterialSum>();
            release.RequirementList.ForEach(r =>
            {
                if (id.HasValue)
                {
                    if (id.Value == r.ReqId && r.MaterialSummary != null)
                        materialList.Add(r.MaterialSummary);
                }
                else if (r.MaterialSummary != null)
                    materialList.Add(r.MaterialSummary);
            });

            await FilterEntityListAsync(materialList);
            return materialList;
        }

        protected override async Task<MaterialSum> Find(string orderNo, int releaseNo, int id)
        {
            var list = await Load(orderNo, releaseNo, id);
            return list.Count > 0 ? list[0] : null;
        }

        protected override MaterialSum CreateRequirement()
        {
            Requirements reqs = new Requirements();
            reqs.ComponentType = ProductionComponentType.Material;
            var entity = reqs.MaterialSummary;
            entity.OrderReleaseStatus = OrderReleaseStatus.InProcess;
            entity.SetDefaults();

            return entity;
        }

        protected override void InsertRequirement(MaterialSum entity, int parentId)
        {
            Requirements parent = this.CurrentRelease.RequirementList.Find(x => x.ReqId == parentId);
            if (parent != null)
            {
                Requirements requirement = null;
                if (parent.Type == 0 || parent.Type == 1 || parent.Type == 2 || parent.Type == 6)
                    requirement = parent;
                else
                    requirement = this.CurrentRelease.RequirementList.Find(RequirementsBase.Columns.TransId, parent.ParentId);

                this.CurrentRelease.InsertRequirements(requirement, new EntityList<Requirements>(new[] { entity.Parent }));

                if (!entity.RequiredDate.HasValue)
                    entity.RequiredDate = DateTime.Today;
            }
            else
                throw new InvalidValueException(string.Format("Parent Requirement ID '{0}' with Release No '{1}' on Order No '{2}' could not be found.",
                    parentId, this.CurrentRelease.ReleaseNo, this.CurrentOrder.OrderNo));
        }
        #endregion Overrides

        #region Helper Methods
        protected virtual void ComponentIdPropertyChanged(MaterialSum entity)
        {
            if (entity == null)
                return;

            if (entity.ComponentId != null && entity.ItemInfo != null)
            {
                entity.Parent.Description = entity.ItemInfo.Description;
                if (entity.ItemInfo.InventoryStatus == InventoryStatus.Superseded)
                {
                    entity.ComponentId = entity.ItemInfo.SuperId;
                    base.AddWarnings("The selected item is superseded and has been replaced");
                }
                else if (entity.ItemInfo.InventoryStatus == InventoryStatus.Obsolete)
                {
                    base.AddWarnings("The selected item has a status of Obsolete");
                }
                else if (entity.ItemInfo.InventoryStatus == InventoryStatus.Discontinued)
                {
                    base.AddWarnings("The selected item has a status of Discontinued");
                }
                entity.SetItemDefaults();
            }
            entity.Validate(MaterialSumBase.Columns.ComponentType.ToString());
        }

        protected virtual void ComponentTypePropertyChanged(MaterialSum entity)
        {
            entity.CostGroupId = Utility.DefaultCostGroupID;
            entity.SetCostDefaults();
        }

        protected virtual void UomPropertyChanged(MaterialSum entity)
        {
            if (entity != null && entity.ItemInfo != null && !string.IsNullOrEmpty(entity.UOM))
            {
                entity.SetCostDefaults();
            }
            entity.CostGroupId = Utility.DefaultCostGroupID;
        }

        protected virtual void LocIdPropertyChanged(MaterialSum entity)
        {
            if (entity != null && entity.ItemInfo != null && !string.IsNullOrEmpty(entity.LocId))
            {
                entity.SetCostDefaults();
            }
            entity.CostGroupId = Utility.DefaultCostGroupID;
        }

        protected virtual void QtyPropertyChanged(MaterialSum entity)
        {
            entity.Parent.QTY = entity.EstQtyRequired;
            if (entity.ItemInfo != null && entity.ItemInfo.IsQuantityTracked && entity.MaterialSumExtList != null && entity.MaterialSumExtList.Count == 1)
            {
                entity.MaterialSumExtList[0].QtyRequired = entity.EstQtyRequired;
            }
        }
        #endregion Helper Methods

        #region Fields
        public const string FunctionID = "840f79bf-3780-4dc4-ae08-992e72f80f1a";
        #endregion Fields
    }
}
