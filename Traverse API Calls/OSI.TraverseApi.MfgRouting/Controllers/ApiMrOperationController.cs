#region Using Directives
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
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMrOperationController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "operation/{id?}", typeof(Operations))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "operation/{id?}", typeof(Operations))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "operation/{id?}", typeof(Operations))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "operation/{id}", typeof(Operations))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            //'Batch Operation type' only - property
            EntityPropertyDictionary.Add(OperationsBase.Columns.MaxQuantity.ToString(), ValidateMinQtyProperty);

            //Non valid 'Subcontract Operation type' properties
            EntityPropertyDictionary.Add(OperationsBase.Columns.WorkCenterId.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.SetupLaborTypeId.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.LaborTypeId.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.ReqEmployees.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.MachineGroupId.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.MGId.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.YieldPct.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.Notes.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.QueueTime.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.QueueTimeIn.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.MachSetup.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.MachSetupIn.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.MachRunTime.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.MachRunTimeIn.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.LaborSetup.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.LaborSetupIn.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.LaborRunTime.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.LaborRunTimeIn.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.WaitTime.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.WaitTimeIn.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.MoveTime.ToString(), ValidateNonSubcontractProperty);
            EntityPropertyDictionary.Add(OperationsBase.Columns.MoveTimeIn.ToString(), ValidateNonSubcontractProperty);

            //Subcontracted
            SubcontractedPropertyDictionary.Add(SubContractedBase.Columns.VendorId.ToString(), (entity) =>
            {
                entity.SetVendorDefaults();
            });
        }

        protected virtual async Task<EntityList<Operations>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.OperationId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Operations>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<OperationsBase.Columns>();
                    builder.AppendEquals(OperationsBase.Columns.OperationId, id);
                    var list = new OperationsProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Operations> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.OperationId, id, false));
        }

        protected virtual async Task<List<Operations>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Operation ID is provided along with more than one record.");

            var entityList = new List<Operations>();
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

        protected virtual async Task<Operations> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.OperationId) || string.IsNullOrWhiteSpace(bodyItem.OperationId))
                bodyItem.OperationId = code;
            else
                code = bodyItem.OperationId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Operations(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Operation ID '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Operation ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "ToolingList")
            {
                var operation = (Operations)args.ParentObject;

                if (operation.OperationType == RoutingOperationType.Subcontract)
                    throw new InvalidValueException(string.Format("Current Operation Type: '{0}' does not support adding tooling information.", operation.OperationType));

                if (operation.IsNew)
                    return this.CreateDetail((Operations)args.ParentObject, args.ItemModel);
            }
            else if (args.PropertyName == "SubContractedList")
            {
                var operation = (Operations)args.ParentObject;

                if (operation.OperationType != RoutingOperationType.Subcontract)
                    throw new InvalidValueException(string.Format("Current Operation Type: '{0}' does not support adding subcontracted information.", operation.OperationType));

                if (operation.IsNew)
                    return this.CreateSubcontract(operation, args.ItemModel);
                else
                    return this.UpdateSubcontract(operation, args.ItemModel);
            }
            return null;
        }

        #region Header Update Methods
        protected virtual void ValidateMinQtyProperty(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is Operations operation)
                {
                    if (operation.OperationType != RoutingOperationType.Batch)
                        e.Handled = true;
                }
            }
        }

        protected virtual void ValidateNonSubcontractProperty(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is Operations operation)
                {
                    if (operation.OperationType == RoutingOperationType.Subcontract)
                        e.Handled = true;
                }
            }
        }
        #endregion Header Update Methods

        #region Detail Update Methods
        protected virtual OperationsTooling CreateDetail(Operations parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ToolingId))
                throw new InvalidValueException("Tooling ID is required.");

            OperationsTooling entity = parent.ToolingList?.Find(OperationsToolingBase.Columns.ToolingId, bodyItem.ToolingId);
            if (entity != null)
                throw new InvalidValueException(string.Format("Tooling ID '{0}' with Operation ID '{1}' already exists.", bodyItem.ToolingId, parent.OperationId));

            entity = parent.ToolingList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as OperationsTooling;
            entity.PropertyChanged -= Detail_PropertyChanged;
        }
        #endregion Detail Update Methods

        #region Subcontract Update Methods
         protected virtual SubContracted UpdateSubcontract(Operations parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.VendorId))
                throw new InvalidValueException("Vendor ID is required.");

            this.FilterEntityList(parent.SubContractedList, SubcontractedFunctionID);

            SubContracted entity = parent.SubContractedList?.Find(x => StringHelper.AreEqual(x.VendorId, bodyItem.VendorId, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Vendor ID '{0}' for Operation ID '{1}' could not be found.", bodyItem.VendorId, parent.OperationId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SubcontractedUpdateComplete;
            entity.PropertyChanged += SubContractedEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual SubContracted CreateSubcontract(Operations parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.VendorId))
                throw new InvalidValueException("Vendor ID is required.");

            SubContracted entity = parent.SubContractedList?.Find(x => StringHelper.AreEqual(x.VendorId, bodyItem.VendorId, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Vendor ID '{0}' for Operation ID '{1}' already exists.",
                    bodyItem.VendorId, parent.OperationId));

            entity = parent.SubContractedList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SubcontractedUpdateComplete;
            entity.PropertyChanged += SubContractedEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void SubcontractedUpdateComplete(object entityObject)
        {
            var entity = entityObject as SubContracted;
            entity.PropertyChanged -= SubContractedEntity_PropertyChanged;
        }
        #endregion Subcontract Update Methods
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
            var entity = sender as Operations;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<Operations> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<OperationsTooling> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as OperationsTooling);
        }

        private void SubContractedEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<SubContracted> action = null;
            if (SubcontractedPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as SubContracted);
        }
        #endregion Event Handlers

        #region Properties
        protected OperationsProvider Provider { get; } = new OperationsProvider();

        protected SortedDictionary<string, Action<Operations>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Operations>>();

        protected SortedDictionary<string, Action<OperationsTooling>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<OperationsTooling>>();

        protected SortedDictionary<string, Action<SubContracted>> SubcontractedPropertyDictionary { get; } = new SortedDictionary<string, Action<SubContracted>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "38D6128B-6FC7-4AF7-879C-A850FD7A6A80";

        public const string SubcontractedFunctionID = "2138815d-b74a-4ff9-b0db-c27dcd80a64b";
        #endregion Fields
    }
}
