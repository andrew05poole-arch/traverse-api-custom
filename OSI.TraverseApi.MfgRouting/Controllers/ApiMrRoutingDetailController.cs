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
    public class ApiMrRoutingDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "routing/{routingid}/detail/{id:int?}", typeof(RoutingDetail))]
        public async Task<IHttpActionResult> Get(string routingId = null, int? id = null)
        {
            return Ok(await Load(routingId, id));
        }

        [ApiRoute(FunctionID, 2f, "routing/{routingid}/detail/{id:int?}", typeof(RoutingDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string routingId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, routingId, id));
        }

        [ApiRoute(FunctionID, 2f, "routing/{routingid}/detail/{id:int?}", typeof(RoutingDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string routingId, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, routingId, id));
        }

        [ApiRoute(FunctionID, 2f, "routing/{routingid}/detail/{id:int}", typeof(RoutingDetail))]
        public async Task Delete(string routingId, int id)
        {
            await this.MarkToDelete(routingId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(RoutingDetailBase.Columns.OperationId.ToString(), OperationIdChange);
        }

        protected virtual async Task<EntityList<RoutingDetail>> Load(string routingId, int? id)
        {
            var list = CurrentRoutingHeader?.DetailList as EntityList<RoutingDetail>;

            if (CurrentRoutingHeader == null || !StringHelper.AreEqual(CurrentRoutingHeader.RoutingId, routingId, false))
            {
                var builder = new SqlFilterBuilder<RoutingHeaderBase.Columns>();
                builder.AppendEquals(RoutingHeaderBase.Columns.RoutingId, routingId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiMrRoutingController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Routing Detail '{0}' could not be found.", id));

                CurrentRoutingHeader = Provider.Items[0];

                list = CurrentRoutingHeader.DetailList as EntityList<RoutingDetail>;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(RoutingDetailBase.Columns.Id, id.Value);

            return list;
        }

        protected virtual async Task<RoutingDetail> Find(string RoutingId, int id)
        {
            var list = await Load(RoutingId, id);
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<RoutingDetail>> ProcessEditRequest(bool isCreate, dynamic body, string routingId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Routing Detail ID is provided along with more than one record.");

            var entityList = new List<RoutingDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, routingId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<RoutingDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string routingId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(routingId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentRoutingHeader.DetailList.AddNew() as RoutingDetail;               
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Routing Detail ID {0} not be found on Routing '{1}'.", code, routingId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string routingId, int id)
        {
            var entity = await this.Find(routingId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Routing Detail ID {0} could not be found on Routing '{1}'.", id, routingId));

            CurrentRoutingHeader.DetailList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void OperationIdChange(RoutingDetail entity)
        {
            var operationInfo = entity.OperationInfo;
            if (operationInfo != null)
            {
                entity.Notes = operationInfo.Notes;
                entity.Description = operationInfo.Description;
                entity.MachineGroupId = operationInfo.MachineGroupId;
                entity.WorkCenterId = operationInfo.WorkCenterId;
                entity.LaborTypeId = operationInfo.LaborTypeId;
                entity.SetupLaborTypeId = operationInfo.SetupLaborTypeId;
                entity.MGId = operationInfo.MGId;
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
            Action<RoutingDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as RoutingDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected RoutingHeaderProvider Provider { get; } = new RoutingHeaderProvider();
        protected RoutingHeader CurrentRoutingHeader { get; set; }
        protected SortedDictionary<string, Action<RoutingDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<RoutingDetail>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "306b8ce5-45ea-479a-8cd1-7845efdbf008";
        #endregion Fields
    }
}
