#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMpRecordProdActTimeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/time/{reqid:int}/detail/{id:int?}", typeof(TimeDetail))]
        public async Task<IHttpActionResult> Get(string orderNo, int releaseNo, int reqId, int? id = null)
        {
            return Ok(await this.Load(orderNo, releaseNo, reqId, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/time/{reqid:int}/detail/{id:int?}", typeof(TimeDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string orderNo, int releaseNo, int reqId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, orderNo, releaseNo, reqId, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/time/{reqid:int}/detail", typeof(TimeDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo, int releaseNo, int reqId)
        {
            return Ok(await ProcessEditRequest(true, body, orderNo, releaseNo, reqId, null));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}/time/{reqid:int}/detail/{id:int}", typeof(TimeDetail))]
        public async Task Delete(string orderNo, int releaseNo, int reqId, int id)
        {
            await this.MarkToDelete(orderNo, releaseNo, reqId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //Time Detail Property Changes
            PropertyDictionary.Add(TimeDetailBase.Columns.TransDate.ToString(), TransDatePropertyChanged);
            PropertyDictionary.Add(TimeDetailBase.Columns.GlPeriod.ToString(), FiscalPeriodPropertyChanged);
            PropertyDictionary.Add(TimeDetailBase.Columns.FiscalYear.ToString(), FiscalPeriodPropertyChanged);
            PropertyDictionary.Add(TimeDetailBase.Columns.EmployeeId.ToString(), (entity) =>
            {
                entity.SetEmployeeDefaults();
            });
            PropertyDictionary.Add(TimeDetailBase.Columns.Labor.ToString(), (entity) =>
            {
                entity.SetLabourTime();
            });
            PropertyDictionary.Add(TimeDetailBase.Columns.LaborIn.ToString(), (entity) =>
            {
                entity.SetLabourTime();
            });
            PropertyDictionary.Add(TimeDetailBase.Columns.LaborSetup.ToString(), (entity) =>
            {
                entity.SetLabourTime();
            });
            PropertyDictionary.Add(TimeDetailBase.Columns.LaborSetupIn.ToString(), (entity) =>
            {
                entity.SetLabourTime();
            });
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

        protected virtual async Task<EntityList<TimeDetail>> Load(string orderNo, int releaseNo, int reqId, int? id)
        {
            var list = this.CurrentTime?.TimeDetailList as EntityList<TimeDetail>;

            if (this.CurrentTime == null || this.CurrentTime.Parent.ReqId != reqId)
            {
                await Load(orderNo, releaseNo, reqId);

                this.CurrentTime = this.CurrentRequirement?.TimeSummary;
                list = this.CurrentTime?.TimeDetailList;
            }

            if (list != null)
                await this.FilterEntityListAsync(list, FunctionID);

            if (id.HasValue)
                list = list?.FindAll(MaterialDetailBase.Columns.SeqNo, id.Value);

            return list;
        }

        protected virtual async Task<TimeDetail> Find(string orderNo, int releaseNo, int reqId, int id)
        {
            var list = await Load(orderNo, releaseNo, reqId, id);
            return list?.Find(x => x.SeqNo == id);
        }

        protected virtual async Task<List<TimeDetail>> ProcessEditRequest(bool isCreate, dynamic body, string orderNo, int releaseNo, int reqId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new List<TimeDetail>();
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

        protected virtual async Task<TimeDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string orderNo, int releaseNo, int reqId, int? id)
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

                if (this.CurrentTime?.OrderReleaseStatus == OrderReleaseStatus.InProcess)
                {
                    entity = CurrentTime?.TimeDetailList?.AddNew();
                    entity.SetDefaults();
                }
                else
                    throw new InvalidValueException("Time Release Status is invalid.");
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Sequence No '{0}' could not be found on Requirement ID '{1}' with Release No '{2}' on Order No '{3}'.",
                    code, reqId, releaseNo, orderNo));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
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

            this.CurrentTime?.TimeDetailList?.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void TransDatePropertyChanged(TimeDetail entity)
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

        protected virtual void FiscalPeriodPropertyChanged(TimeDetail entity)
        {
            PeriodConversion fiscalPeriod = PeriodConversion.GetFiscalPeriod(entity.GlPeriod, entity.FiscalYear);
            if (fiscalPeriod != null)
            {
                if (fiscalPeriod.ClosedGL.Value)
                    AddWarnings(string.Format("Period {0} is closed for fiscal year {1}.", entity.GlPeriod, entity.FiscalYear));
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
            Action<TimeDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TimeDetail);
        }
        #endregion Event Handlers

        #region Properties
        private OrderProvider Provider { get; } = new OrderProvider();

        protected Order CurrentOrder { get; set; }

        protected OrderReleases CurrentRelease { get; set; }

        protected Requirements CurrentRequirement { get; set; }

        protected TimeSum CurrentTime { get; set; }

        protected SortedDictionary<string, Action<TimeDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TimeDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties 

        #region Fields
        public const string FunctionID = "94b3c7e8-0c49-4f2a-a3bd-a5fd98791447";
        #endregion Fields
    }
}
