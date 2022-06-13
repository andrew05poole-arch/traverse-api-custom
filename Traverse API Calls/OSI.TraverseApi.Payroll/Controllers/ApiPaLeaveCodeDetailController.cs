#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaLeaveCodeDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "leavecode/{leavecode}/detail/{uptoyear?}", typeof(LeaveCodeDetail))]
        public async Task<IHttpActionResult> Get(string leaveCode = null, short? upToYear = null)
        {
            return Ok(await this.Load(leaveCode, upToYear));
        }

        [ApiRoute(FunctionID, 2f, "leavecode/{leavecode}/detail/{uptoyear?}", typeof(LeaveCodeDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string leaveCode = null, short? upToYear = null)
        {
            return Ok(await ProcessEditRequest(false, body, leaveCode, upToYear));
        }

        [ApiRoute(FunctionID, 2f, "leavecode/{leavecode}/detail/{uptoyear?}", typeof(LeaveCodeDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string leaveCode = null, short? upToYear = null)
        {
            return Ok(await ProcessEditRequest(true, body, leaveCode, upToYear));
        }

        [ApiRoute(FunctionID, 2f, "leavecode/{leavecode}/detail/{uptoyear}", typeof(LeaveCodeDetail))]
        public async Task Delete(string leaveCode, short upToYear)
        {
            await this.MarkToDelete(leaveCode, upToYear);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates(){}
        #endregion Override

        protected virtual async Task<EntityList<LeaveCodeDetail>> Load(string leaveCodeId, short? id)
        {
            if (CurrentLeaveCodeDetailList == null || CurrentLeaveCodeDetailList.Exists(i => i.UpToYear != id))
            {
                var builder = new SqlFilterBuilder<LeaveCodeHeaderBase.Columns>();
                builder.AppendEquals(LeaveCodeHeaderBase.Columns.Id, leaveCodeId);
                var headerList = new LeaveCodeHeaderProvider().Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPaLeaveCodeHeaderController.FunctionID);

                if (headerList.Count <= 0)
                    throw new InvalidValueException(string.Format("Leave Code ID '{0}' could not be found.", leaveCodeId));

                CurrentLeaveCodeDetailList = headerList[0].DetailList;
                await this.FilterEntityListAsync(CurrentLeaveCodeDetailList, FunctionID);
                Provider.Items.AddRange(CurrentLeaveCodeDetailList);                
            }

            if (id.HasValue)
                return CurrentLeaveCodeDetailList.FindAll(LeaveCodeDetailBase.Columns.UpToYear, id.Value);

            return CurrentLeaveCodeDetailList;
        }

        protected virtual async Task<LeaveCodeDetail> Find(string leaveCode, short id)
        {
            var list = await Load(leaveCode, id);
            return list?.Find(x => x.UpToYear == id);
        }

        protected virtual async Task<List<LeaveCodeDetail>> ProcessEditRequest(bool isCreate, dynamic body, string leaveCode, short? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Up To Year is provided along with more than one record.");

            var entityList = new List<LeaveCodeDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, leaveCode, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<LeaveCodeDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string leaveCode, short? id)
        {
            short code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.UpToYear) || bodyItem.UpToYear == null)
                bodyItem.UpToYear = code;
            else
                code = Convert.ToInt16(bodyItem.UpToYear);

            var entity = await this.Find(leaveCode, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentLeaveCodeDetailList.AddNew() as LeaveCodeDetail;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Up To Year {0} not be found on Leave Code ID'{1}'.", code, leaveCode));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string leaveCode, short id)
        {
            var entity = await this.Find(leaveCode, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Up To Year {0} could not be found on Leave Code ID'{1}'", id, leaveCode));

            CurrentLeaveCodeDetailList.Remove(entity);
            this.Provider.Update(this.CompId);
        }
        #endregion Helper Methods

        #region Event Handler
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<LeaveCodeDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as LeaveCodeDetail);
        }
        #endregion Event Handler

        #region Properties
        protected EntityList<LeaveCodeDetail> CurrentLeaveCodeDetailList { get; set; }
        protected LeaveCodeDetailProvider Provider { get; } = new LeaveCodeDetailProvider();

        protected SortedDictionary<string, Action<LeaveCodeDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<LeaveCodeDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "c03fe183-85da-4432-bd5d-8a33a6e7e6ad";
    #endregion Fields
}
}
