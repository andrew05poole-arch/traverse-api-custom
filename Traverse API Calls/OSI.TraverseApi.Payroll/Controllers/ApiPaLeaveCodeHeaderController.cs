#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaLeaveCodeHeaderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "leavecode/{id?}", typeof(LeaveCodeHeader))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "leavecode/{id?}", typeof(LeaveCodeHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "leavecode/{id?}", typeof(LeaveCodeHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "leavecode/{id}", typeof(LeaveCodeHeader))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates(){}
        #endregion Override

        protected virtual async Task<EntityList<LeaveCodeHeader>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.Id, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<LeaveCodeHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<LeaveCodeHeaderBase.Columns>();
                    builder.AppendEquals(LeaveCodeHeaderBase.Columns.Id, id);
                    var list = new LeaveCodeHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<LeaveCodeHeader> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.Id, id, false));
        }

        protected virtual async Task<List<LeaveCodeHeader>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Leave Code ID is provided along with more than one record.");

            var entityList = new List<LeaveCodeHeader>();
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

        protected virtual async Task<LeaveCodeHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || string.IsNullOrWhiteSpace(bodyItem.Id))
                bodyItem.Id = code;
            else
                code = bodyItem.Id;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new LeaveCodeHeader(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Leave Code ID '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Leave Code ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailList")
            {
                if (((LeaveCodeHeader)args.ParentObject).IsNew)
                    return this.CreateDetail((LeaveCodeHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDetail((LeaveCodeHeader)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Detail Method
        protected virtual LeaveCodeDetail UpdateDetail(LeaveCodeHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.UpToYear))
                throw new InvalidValueException("Up To Year is required.");

            this.FilterEntityList(parent.DetailList, ApiPaLeaveCodeDetailController.FunctionID);
            LeaveCodeDetail entity = parent?.DetailList?.Find(LeaveCodeDetailBase.Columns.UpToYear, (short)bodyItem.UpToYear);
            if (entity == null)
                throw new InvalidValueException(string.Format("Up To Year {0} could not be found on Leave Code ID'{1}'", bodyItem.UpToYear, parent.Id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = RoutingUpdateComplete;
            entity.PropertyChanged += DetailEntity_PropertyChanged;


            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual LeaveCodeDetail CreateDetail(LeaveCodeHeader parent, dynamic bodyItem)
        {
            LeaveCodeDetail entity = parent.DetailList.AddNew();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = RoutingUpdateComplete;
            entity.PropertyChanged += DetailEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void RoutingUpdateComplete(object entityObject)
        {
            var entity = entityObject as LeaveCodeDetail;
            entity.PropertyChanged -= DetailEntity_PropertyChanged;
        }
        #endregion Detail Method
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
            Action<LeaveCodeHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as LeaveCodeHeader);
        }

        private void DetailEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<LeaveCodeDetail> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as LeaveCodeDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected LeaveCodeHeaderProvider Provider { get; } = new LeaveCodeHeaderProvider();

        protected SortedDictionary<string, Action<LeaveCodeHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<LeaveCodeHeader>>();
        protected SortedDictionary<string, Action<LeaveCodeDetail>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<LeaveCodeDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "3EAB000D-86D5-4BCC-BFF7-1E50623256B3";
        #endregion Fields
    }
}
