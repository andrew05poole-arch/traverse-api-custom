#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives


namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMpEditReleaseSubcontractController : ApiMpEditReleaseBase<SubContractSum>
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/subcontract/{id:int?}", typeof(SubContractSum))]
        public async Task<IHttpActionResult> Get(string orderNo, int releaseNo, int? id = null)
        {
            return Ok(await this.Load(orderNo, releaseNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/subcontract/{id:int?}", typeof(SubContractSum))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string orderNo, int releaseNo, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, orderNo, releaseNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/subcontract", typeof(SubContractSum))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo, int releaseNo)
        {
            return Ok(await ProcessEditRequest(true, body, orderNo, releaseNo, null));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/subcontract/{id:int}", typeof(SubContractSum))]
        public async Task Delete(string orderNo, int releaseNo, int id)
        {
            await this.MarkToDelete(orderNo, releaseNo, id);
        }
        #endregion Web Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(SubContractSumBase.Columns.OperationId.ToString(), OperationIdPropertyChanged);
            PropertyDictionary.Add(SubContractSumBase.Columns.EstQtyRequired.ToString(), (t) => t.Parent.QTY = t.EstQtyRequired);
        }

        protected override async Task<EntityList<SubContractSum>> Load(string orderNo, int releaseNo, int? id)
        {
            var release = await LoadOrderRelease(orderNo, releaseNo);
            if (release == null || release.RequirementList == null)
                throw new InvalidValueException(string.Format("Release No '{0}' could not be found on Order No '{1}'.", releaseNo, orderNo));

            var contractList = new EntityList<SubContractSum>();
            release.RequirementList.ForEach(r =>
            {
                if (id.HasValue)
                {
                    if (id.Value == r.ReqId && r.SubContractSummary != null)
                        contractList.Add(r.SubContractSummary);
                }
                else if (r.SubContractSummary != null)
                    contractList.Add(r.SubContractSummary);
            });

            await FilterEntityListAsync(contractList);
            return contractList;
        }

        protected override async Task<SubContractSum> Find(string orderNo, int releaseNo, int id)
        {
            var list = await Load(orderNo, releaseNo, id);
            return list.Count > 0 ? list[0] : null;
        }

        protected override SubContractSum CreateRequirement()
        {
            Requirements reqs = new Requirements();
            reqs.ComponentType = ProductionComponentType.Subcontract;
            var entity = reqs.SubContractSummary;
            entity.OrderReleaseStatus = OrderReleaseStatus.InProcess;
            entity.SetDefaults();

            return entity;
        }

        protected override void InsertRequirement(SubContractSum entity, int parentId)
        {
            Requirements parent = this.CurrentRelease.RequirementList.Find(x => x.ReqId == parentId);
            if (parent != null)
            {
                if (parent.Type != 0 && parent.Type != 2)
                    throw new InvalidValueException("Cannot add a subcontract entry to the selected parent");

                this.CurrentRelease.InsertRequirements(parent, new EntityList<Requirements>(new[] { entity.Parent }));

                if (!entity.RequiredDate.HasValue)
                    entity.RequiredDate = DateTime.Today;
            }
            else
                throw new InvalidValueException(string.Format("Parent Requirement ID '{0}' with Release No '{1}' on Order No '{2}' could not be found.",
                    parentId, this.CurrentRelease.ReleaseNo, this.CurrentOrder.OrderNo));
        }
        #endregion Overrides

        #region Helper Methods
        protected virtual void OperationIdPropertyChanged(SubContractSum entity)
        {
            if (entity == null || entity.OperationInfo == null)
                return;

            entity.Parent.Description = entity.OperationInfo.Description;
            if (!entity.IsNew)
                return;

            entity.SetDefaults();
        }
        #endregion Helper Methods

        #region Fields
        public const string FunctionID = "7a08fe55-8de2-4280-9b08-a447e5ebe3d8";
        #endregion Fields
    }
}
