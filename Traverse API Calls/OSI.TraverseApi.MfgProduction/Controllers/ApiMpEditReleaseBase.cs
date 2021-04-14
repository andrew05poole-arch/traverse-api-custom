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
    public abstract class ApiMpEditReleaseBase<E> : ApiControllerBase where E : EntityBase
    {
        #region Overrides
        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (StringHelper.AreEqual(args.FieldName, "ReqId", false))
                args.ActualValue = (((EntityBase)args.Entity).ParentEntity as Requirements)?.ReqId;
        }
        #endregion Overrides

        #region Helper Methods
        protected virtual async Task LoadOrder(string orderNo)
        {
            if (this.CurrentOrder != null && StringHelper.AreEqual(this.CurrentOrder.OrderNo, orderNo, false))
                return;

            var order = this.Provider.Items.Find(OrderBase.Columns.OrderNo, orderNo, true);
            if (order == null)
            {
                SqlFilterBuilder<OrderBase.Columns> builder = new SqlFilterBuilder<OrderBase.Columns>();
                builder.AppendEquals(OrderBase.Columns.OrderNo, orderNo);
                var list = (new OrderProvider()).Load(CompId, new FilterCriteria(builder.ToString(), ""));
                await this.FilterEntityListAsync(list, ApiMpOrderController.FunctionID);

                if (list.Count > 0)
                {
                    order = list[0];
                    this.Provider.Items.Add(order);
                }
            }
            this.CurrentOrder = order;
        }

        protected virtual async Task<OrderReleases> LoadOrderRelease(string orderNo, int releaseNo)
        {
            await LoadOrder(orderNo);

            if (this.CurrentOrder == null)
                throw new InvalidValueException(string.Format("Order No '{0}' could not be found.", orderNo));

            await FilterEntityListAsync(this.CurrentOrder.DetailList, ApiMpOrderReleaseController.FunctionID);

            return this.CurrentRelease = this.CurrentOrder.DetailList.Find(OrderReleasesBase.Columns.ReleaseNo, releaseNo);
        }

        protected abstract Task<EntityList<E>> Load(string orderNo, int releaseNo, int? id);

        protected abstract Task<E> Find(string orderNo, int releaseNo, int id);

        protected virtual async Task<List<E>> ProcessEditRequest(bool isCreate, dynamic body, string orderNo, int releaseNo, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new List<E>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, orderNo, releaseNo, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);

            //Recalculate
            this.CurrentRelease.CalculateRequirementLeadTime(this.CurrentRelease.RequirementList[0]);
            this.CurrentRelease.CalculateRequirementDate(this.CurrentRelease.RequirementList[0]);

            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<E> ProcessBodyItem(bool isCreate, dynamic bodyItem, string orderNo, int releaseNo, int? id)
        {
            int code = id.GetValueOrDefault(-1);
            int? parentReqId = Convert.ToInt32(!ApiUserSkipped.IsApiUserSkipped(bodyItem.ParentId) && bodyItem.ParentId != null ? bodyItem.ParentId : null);

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ReqId) || bodyItem.ReqId == null)
                bodyItem.ReqId = code;
            else
                code = Convert.ToInt32(bodyItem.ReqId);

            var entity = await this.Find(orderNo, releaseNo, code);

            Requirements reqs = null;
            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CreateRequirement();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Requirement ID '{0}' with Release No '{1}' on Order No '{2}' could not be found.",
                    code, releaseNo, orderNo));

            reqs = entity.ParentEntity as Requirements;
            
            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (entity.IsNew && parentReqId.HasValue)
            {
                reqs.EstStartDate = DateTime.Today;
                reqs.EstCompletionDate = DateTime.Today;
                InsertRequirement(entity, parentReqId.Value);

                if (reqs.ComponentType == ProductionComponentType.Subassembly)
                {
                    string revision = !ApiUserSkipped.IsApiUserSkipped(bodyItem.RevisionNo) && !string.IsNullOrWhiteSpace(bodyItem.RevisionNo as string) ? (string)bodyItem.RevisionNo : null;
                    
                    if (string.IsNullOrWhiteSpace(revision))
                        throw new InvalidValueException("Revision number is invalid.");

                    reqs.ExplodeSubAssembly(revision);
                }
            }

            if (reqs.MaterialSummary != null)
                reqs.MaterialSummary.Calculate();

            return entity;
        }

        protected virtual async Task MarkToDelete(string orderNo, int releaseNo, int id)
        {
            var entity = await this.Find(orderNo, releaseNo, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Requirement ID '{0}' with Release No '{1}' on Order No '{2}' could not be found.",
                    id, releaseNo, orderNo));

            this.CurrentRelease.RequirementList.Remove(entity.ParentEntity as Requirements);
            this.Provider.Update(this.CompId);
        }

        protected abstract E CreateRequirement();

        protected abstract void InsertRequirement(E entity, int parentId);

        protected virtual Operations LookupOperation(string operationId)
        {
            OperationsProvider operationsProvider = new OperationsProvider();
            (new SqlFilterBuilder<OperationsBase.Columns>()).AppendEquals(OperationsBase.Columns.OperationId, operationId);
            return operationsProvider.Load(this.CompId).Find(OperationsBase.Columns.OperationId, operationId);
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
            Action<E> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
            {
                var entity = sender as E;
                entity.PropertyChanged -= Entity_PropertyChanged;
                action.Invoke(sender as E);
                entity.PropertyChanged += Entity_PropertyChanged;
            }
        }
        #endregion Event Handlers

        #region Properties
        protected OrderProvider Provider { get; } = new OrderProvider();

        protected Order CurrentOrder { get; set; }

        protected OrderReleases CurrentRelease { get; set; }

        protected SortedDictionary<string, Action<E>> PropertyDictionary { get; } = new SortedDictionary<string, Action<E>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties
    }
}
