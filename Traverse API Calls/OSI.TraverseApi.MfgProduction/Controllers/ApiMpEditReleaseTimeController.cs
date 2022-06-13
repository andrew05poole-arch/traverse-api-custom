#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Manufacturing.Controllers
{
    public class ApiMpEditReleaseTimeController : ApiMpEditReleaseBase<TimeSum>
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/time/{id:int?}", typeof(TimeSum))]
        public async Task<IHttpActionResult> Get(string orderNo, int releaseNo, int? id = null)
        {
            return Ok(await this.Load(orderNo, releaseNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/time/{id:int?}", typeof(TimeSum))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string orderNo, int releaseNo, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, orderNo, releaseNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/time", typeof(TimeSum))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo, int releaseNo)
        {
            return Ok(await ProcessEditRequest(true, body, orderNo, releaseNo, null));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/time/{id:int}", typeof(TimeSum))]
        public async Task Delete(string orderNo, int releaseNo, int id)
        {
            await this.MarkToDelete(orderNo, releaseNo, id);
        }
        #endregion Web Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TimeSumBase.Columns.OperationId.ToString(), OperationIdPropertyChanged);
            PropertyDictionary.Add(TimeSumBase.Columns.QtyProducedEst.ToString(), (t) => t.Parent.QTY = t.QtyProducedEst);
            PropertyDictionary.Add(TimeSumBase.Columns.WorkCenterId.ToString(), (t) => t.SetWorkCenterDefaults());
            PropertyDictionary.Add(TimeSumBase.Columns.LaborSetupTypeId.ToString(), (t) => t.SetLaborSetupDefaults());
            PropertyDictionary.Add(TimeSumBase.Columns.LaborTypeId.ToString(), (t) => t.SetLabourDefaults());
            PropertyDictionary.Add(TimeSumBase.Columns.MachineGroupId.ToString(), (t) => t.SetMachineDefaults());

            //PropertyDictionary.Add(TimeSumBase.Columns.QueueTimeEst.ToString(), QueueTimePropertyChanged);
            //PropertyDictionary.Add(TimeSumBase.Columns.MachineSetupEst.ToString(), MachineSetupTimePropertyChanged);
            //PropertyDictionary.Add(TimeSumBase.Columns.MachineRunEst.ToString(), MachineRunTimePropertyChanged);
            //PropertyDictionary.Add(TimeSumBase.Columns.LaborSetupEst.ToString(), LaborSetupTimePropertyChanged);
            //PropertyDictionary.Add(TimeSumBase.Columns.LaborEst.ToString(), LaborRunTimePropertyChanged);
            //PropertyDictionary.Add(TimeSumBase.Columns.WaitTimeEst.ToString(), WaitTimePropertyChanged);
            //PropertyDictionary.Add(TimeSumBase.Columns.MoveTimeEst.ToString(), MoveTimePropertyChanged);

        }

        protected override async Task<EntityList<TimeSum>> Load(string orderNo, int releaseNo, int? id)
        {
            var release = await LoadOrderRelease(orderNo, releaseNo);
            if (release == null || release.RequirementList == null)
                throw new InvalidValueException(string.Format("Release No '{0}' could not be found on Order No '{1}'.", releaseNo, orderNo));

            var timeList = new EntityList<TimeSum>();
            release.RequirementList.ForEach(r =>
            {
                if (id.HasValue)
                {
                    if (id.Value == r.ReqId && r.TimeSummary != null)
                        timeList.Add(r.TimeSummary);
                }
                else if (r.TimeSummary != null)
                    timeList.Add(r.TimeSummary);
            });

            await FilterEntityListAsync(timeList);
            return timeList;
        }

        protected override async Task<TimeSum> Find(string orderNo, int releaseNo, int id)
        {
            var list = await Load(orderNo, releaseNo, id);
            return list.Count > 0 ? list[0] : null;
        }

        protected override TimeSum CreateRequirement()
        {
            Requirements reqs = new Requirements();
            reqs.ComponentType = ProductionComponentType.Routing;
            var entity = reqs.TimeSummary;
            entity.OrderReleaseStatus = OrderReleaseStatus.InProcess;
            entity.SetDefaults();

            return entity;
        }

        protected override void InsertRequirement(TimeSum entity, int parentId)
        {
            Requirements parent = this.CurrentRelease.RequirementList.Find(x => x.ReqId == parentId);
            if (parent != null)
            {
                if (parent.Type != 0 && parent.Type != 2)
                    throw new InvalidValueException("Cannot add a time entry to the selected parent");

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
        protected virtual void SetTimeUnit(TimeSum entity)
        {
            TimeUnit dfltUnit = Utility.DefaultUnitofTime;
            entity.QueueTimeEstUnit = dfltUnit;
            entity.MachineSetupEstUnit = dfltUnit;
            entity.MachineRunEstUnit = dfltUnit;
            entity.LaborSetupEstUnit = dfltUnit;
            entity.LaborEstUnit = dfltUnit;
            entity.WaitTimeEstUnit = dfltUnit;
            entity.MoveTimeEstUnit = dfltUnit;
        }

        protected virtual void OperationIdPropertyChanged(TimeSum entity)
        {
            if (entity == null || entity.OperationInfo == null)
                return;

            entity.Parent.Description = entity.OperationInfo.Description;
            if (!entity.IsNew)
                return;

            entity.OperatorCount = entity.OperationInfo.ReqEmployees;
            entity.SetDefaults();
            SetTimeUnit(entity);
        }

        //protected virtual void QueueTimePropertyChanged(TimeSum entity)
        //{
        //    if (entity.QueueTimeEst != 0)
        //    {
        //        entity.QueueTimeEst = (int)this.CurrentTime.TimeUnitQueueTimeEst;
        //        return;
        //    }
        //}

        //protected virtual void MachineSetupTimePropertyChanged(TimeSum entity)
        //{
        //    if (entity.MachineSetupEst != 0)
        //    {
        //        entity.MachineSetupEst = (int)this.CurrentTime.TimeUnitMachineSetupEst;
        //        return;
        //    }
        //}

        //protected virtual void MachineRunTimePropertyChanged(TimeSum entity)
        //{
        //    if (entity.MachineRunEst != 0)
        //    {
        //        entity.MachineRunEst = (int)this.CurrentTime.TimeUnitMachineRunEst;
        //        return;
        //    }
        //}

        //protected virtual void LaborSetupTimePropertyChanged(TimeSum entity)
        //{
        //    if (entity.LaborSetupEst != 0)
        //    {
        //        entity.MachineRunEst = (int)this.CurrentTime.TimeUnitMachineRunEst;
        //        return;
        //    }
        //}

        //protected virtual void LaborRunTimePropertyChanged(TimeSum entity)
        //{
        //    if (entity.LaborEst != 0)
        //    {
        //        entity.LaborEst = (int)this.CurrentTime.TimeUnitLaborEst;
        //        return;
        //    }
        //}

        //protected virtual void WaitTimePropertyChanged(TimeSum entity)
        //{
        //    if (entity.WaitTimeEst != 0)
        //    {
        //        entity.WaitTimeEst = (int)this.CurrentTime.TimeUnitWaitTimeEst;
        //        return;
        //    }
        //}

        //protected virtual void MoveTimePropertyChanged(TimeSum entity)
        //{
        //    if (entity.MachineRunEst != 0)
        //    {
        //        entity.MachineRunEst = (int)this.CurrentTime.TimeUnitMachineRunEst;
        //        return;
        //    }
        //}
        #endregion Helper Methods

        #region Fields
        public const string FunctionID = "8ae5956c-2346-481d-bab0-af960c4f1ed1";
        #endregion Fields
    }
}
