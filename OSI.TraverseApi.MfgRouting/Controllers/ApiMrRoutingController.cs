#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMrRoutingController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "routing/{id?}", typeof(RoutingHeader))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "routing/{id?}", typeof(RoutingHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "routing/{id?}", typeof(RoutingHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "routing/{id}", typeof(RoutingHeader))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            DetailPropertyDictionary.Add(RoutingDetailBase.Columns.OperationId.ToString(), OperationIdChange);
        }

        protected virtual async Task<EntityList<RoutingHeader>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.RoutingId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<RoutingHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<RoutingHeaderBase.Columns>();
                    builder.AppendEquals(RoutingHeaderBase.Columns.RoutingId, id);
                    var list = new RoutingHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                if(Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Routing ID '{0}' could not be found.",id));

                await this.FilterEntityListAsync(Provider.Items);
            }
            return Provider.Items;
        }

        protected virtual async Task<RoutingHeader> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.RoutingId, id, false));
        }

        protected virtual async Task<List<RoutingHeader>> ProcessEditRequest(bool isCreate, dynamic body, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Routing ID is provided along with more than one record.");

            var entityList = new List<RoutingHeader>();
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

        protected virtual async Task<RoutingHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.RoutingId) || string.IsNullOrWhiteSpace(bodyItem.RoutingId))
                bodyItem.RoutingId = code;
            else
                code = bodyItem.RoutingId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new RoutingHeader(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Routing ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Routing ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailList")
            {
                if (((RoutingHeader)args.ParentObject).IsNew)
                    return this.CreateDetail((RoutingHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDetail((RoutingHeader)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Detail Methods
        protected virtual RoutingDetail CreateDetail(RoutingHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.OperationId))
                throw new InvalidValueException("Operation ID is required.");

            RoutingDetail entity = parent.DetailList.AddNew();
            
            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;
            entity.SetOperationDefaults();

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual RoutingDetail UpdateDetail(RoutingHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Routing Detail ID is required.");

            this.FilterEntityList(parent.DetailList, ApiMrRoutingDetailController.FunctionID);
            RoutingDetail entity = (parent.DetailList as EntityList<RoutingDetail>).Find(RoutingDetailBase.Columns.Id, (int)bodyItem.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Detail ID '{0}' with Routing ID '{1}' could not be found.", bodyItem.Id, parent.RoutingId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as RoutingDetail;
            entity.PropertyChanged -= Detail_PropertyChanged;
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
        #endregion Detail Methods
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
            var entity = sender as RoutingHeader;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<RoutingHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<RoutingDetail> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as RoutingDetail);
        }
        #endregion Event Handlers

        #region Properties
        private RoutingHeaderProvider Provider { get; } = new RoutingHeaderProvider();
        protected SortedDictionary<string, Action<RoutingHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<RoutingHeader>>();

        protected SortedDictionary<string, Action<RoutingDetail>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<RoutingDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "68355646-1F88-440B-9362-A9D07271B3F7";
        #endregion Fields
    }
}
