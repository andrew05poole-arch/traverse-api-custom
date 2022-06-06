#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WM;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMReleaseItemsPickController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "release/released/{locationid}/{pickid?}", typeof(PickGenerated))]
        public async Task<IHttpActionResult> Get(string locationid = null, string pickid = null)
        {
            return Ok(await this.Load(locationid, pickid));
        }

        [ApiRoute(FunctionID, 2f, "release/assign/{pickid}", typeof(PickGenerated))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string pickId = null)
        {
            return Ok(await ProcessEditRequest(false, body, null, pickId));
        }

        [ApiRoute(FunctionID, 2f, "release/pick/{locationid}", typeof(PickGenerated))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string locationid = null)
        {
            return Ok(await ProcessEditRequest(true, body, locationid, null));
        }

        [ApiRoute(FunctionID, 2f, "release/pick/{pickId}", typeof(PickGenerated))]
        public async Task Delete(string pickid)
        {
            await this.MarkToDelete(pickid);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.Entity is PickGenerated pick)
            {
                if (StringHelper.AreEqual(args.FieldName, "QtyRemaining", false))
                {
                    args.ActualValue = pick.QtyReq - pick.QtyPacked;
                }
                if (StringHelper.AreEqual(args.FieldName, "AssignedTo", false))
                {
                    args.ActualValue = pick.PickInfo?.AssignedTo;
                }
                if (StringHelper.AreEqual(args.FieldName, "PickDate", false))
                {
                    args.ActualValue = pick.PickInfo?.PickDate;
                }
                if (StringHelper.AreEqual(args.FieldName, "ExtLocBZoneId", false))
                {
                    ExtLocationBin extLocationBin = ListHelper.GetExtLocationBin(pick.ExtLocA, pick.LocId, this.CompId);
                    if (extLocationBin != null)
                        args.ActualValue = extLocationBin.ZoneId;
                }
            }
        }
        #endregion

        protected virtual async Task<EntityList<PickGenerated>> Load(string location, string id)
        {
            if (this.Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !this.Provider.Items.Exists(i => StringHelper.AreEqual(id, i.PickId, false))))
            {
                var builder = new SqlFilterBuilder<PickGenerated.Columns>();

                if (!string.IsNullOrEmpty(location))
                    builder.AppendEquals(PickGenerated.Columns.LocId, location);

                if (string.IsNullOrEmpty(id))
                    Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                else
                {
                    builder.AppendEquals(PickGenerated.Columns.PickId, id);
                    var list = new ReleaseItems(this.CompId).LoadExistingOrders(builder.ToString());
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(this.Provider.Items);
            }
            return this.Provider.Items;
        }

        protected virtual async Task<PickGenerated> Find(string location, string id)
        {
            var list = await Load(location, id);
            return list.Find(x => StringHelper.AreEqual(x.PickId, id, false) && StringHelper.AreEqual(x.LocId, location, false));
        }

        protected virtual async Task<List<PickGenerated>> ProcessEditRequest(bool isCreate, dynamic bodyItem, string location, string id)
        {
            object[] list;
            if (bodyItem is object[])
                list = bodyItem;
            else
                list = new object[1] { bodyItem };

            var entityList = new List<PickGenerated>();
            var auxList = new List<PickGenerated>();

            foreach (dynamic item in list)
            {
                var processList = new List<PickGenerated>();
                var entity = await this.ProcessBodyItem(isCreate, item, location, id);
                processList.Add(entity);
                auxList = await ProcessPickGenerated(isCreate, processList, location, id);
                entityList.AddRange(auxList);
            }
           
            return entityList;
        }

        protected virtual async Task<List<PickGenerated>> ProcessPickGenerated(bool isCreate, List<PickGenerated> entityList, string location, string id)
        {
            if (entityList.Count > 0)
            {
                if (isCreate)
                {
                    EntityList<NewPickGenerate> newPicks = this.LoadPicks(entityList, location);
                    if (newPicks.Count > 0)
                    {
                        string pickId = PickInfoProvider.GetNextTransId(this.CompId, null);
                        await ValidateEntityListAsync(newPicks);
                        this.ReleaseSelectedItems(pickId, null, DateTime.Today, string.Empty, newPicks, location);
                        return this.LoadPick(pickId);
                    }
                    else
                    {
                        throw new InvalidValueException("Invalid Data. Please verify that Order Id and Entry Num exist on the Provided Source.");
                    }
                }
                else
                {
                    return this.LoadPick(id);
                }
            }
            return entityList;
        }

        protected virtual async Task<PickGenerated> ProcessBodyItem(bool isCreate, dynamic bodyItem, string location, string id)
        {
            string code = id;
            string loc = location;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PickId) || string.IsNullOrEmpty(bodyItem.PikcId))
                bodyItem.Id = code;
            else
                code = bodyItem.PickId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId) || string.IsNullOrEmpty(bodyItem.LocId))
                bodyItem.LocId = loc;
            else
                loc = bodyItem.LocId;

            var entity = await this.Find(loc, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new PickGenerated(this.CompId);
                entity.LocId = loc;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Pick Id '{0}' could not be found.", code));
            else
            {
                this.AssignPick(bodyItem.AssignedTo, code);
            }
            if (isCreate)
            {
                ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
                entity.PropertyChanged += Entity_PropertyChanged;
                await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
                entity.PropertyChanged -= Entity_PropertyChanged;
                ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;
            }
            return entity;
        }
                
        protected virtual async Task MarkToDelete(string id)
        {
            var pickInfo = await this.LoadPickInfo(id);

            if (pickInfo == null)
                throw new NothingToProcessException(string.Format("Píck Id '{0}' could not be found.", id));
            else
            {                
                this.UnReleasePick(pickInfo);
            }
        }

        protected virtual List<PickGenerated> LoadPick(string pickId)
        {
            PickGeneratedProvider pProvider = new PickGeneratedProvider();
            SqlFilterBuilder<PickGeneratedBase.Columns> pBuilder = new SqlFilterBuilder<PickGeneratedBase.Columns>();
            pBuilder.AppendEquals(PickGeneratedBase.Columns.PickId, pickId);

            return pProvider.Load(this.CompId, new FilterCriteria(pBuilder.ToString(), string.Empty))?.ToList(); ;
        }

        protected virtual EntityList<NewPickGenerate> LoadPicks(List<PickGenerated> entityList, string locId)
        {
            var engine = this.CreateNewProcessEngine();
            EntityList<NewPickGenerate> toGenerate = new EntityList<NewPickGenerate>();

            SQLStringBuilder sQLStringBuilder = new SQLStringBuilder();
            sQLStringBuilder.AppendEquals("LocId", locId);
            engine.LocId = locId;
            EntityList<NewPickGenerate> newOrdersList = engine.LoadNewOrders(sQLStringBuilder.ToString());

            if (newOrdersList != null && newOrdersList.Count > 0)
            {
                entityList.ForEach(order => {
                    var newPick = newOrdersList.Find(o => o.Ref1 == order.Ref1 && o.SourceId == order.SourceId && o.EntryNum == order.EntryNum);
                    if (newPick != null)
                        toGenerate.Add(newPick);
                });
            }
            return toGenerate;
        }

        protected virtual void AssignPick(dynamic assignedTo, string pickId)
        {
            UserProvider uProvider = new UserProvider();
            PickInfoProvider pProvider = new PickInfoProvider();

            SqlFilterBuilder<User> uBuilder = new SqlFilterBuilder<User>();
            uBuilder.AppendEquals(UserBase.Columns.EmployeeId.ToString(), assignedTo);

            SqlFilterBuilder<PickInfo> pBuilder = new SqlFilterBuilder<PickInfo>();
            pBuilder.AppendEquals(PickInfo.Columns.Id.ToString(), pickId);

            uProvider.Load(this.CompId, new FilterCriteria(uBuilder.ToString(), string.Empty));
            pProvider.Load(this.CompId, new FilterCriteria(pBuilder.ToString(), string.Empty));

            if (uProvider.Items != null && uProvider.Items.Count > 0 && pProvider.Items != null && pProvider.Items.Count > 0)
            {
                pProvider.Items[0].AssignedTo = assignedTo;
                pProvider.Items[0].MarkAsDirty();
                pProvider.Update(this.CompId);
            }
            else
            {
                throw new Exception(string.Format("Employee ID '{0}' does not exist as a valid WM User", assignedTo));
            }
        }

        protected virtual async Task<PickInfo> LoadPickInfo(string id)
        {
            PickInfoProvider piProvider = new PickInfoProvider();
            SqlFilterBuilder<PickInfo.Columns> piBuilder = new SqlFilterBuilder<PickInfoBase.Columns>();
            piBuilder.AppendEquals(PickInfoBase.Columns.Id.ToString(), id);
            var entityList = piProvider.Load(this.CompId, new FilterCriteria(piBuilder.ToString(), string.Empty));
            if (entityList != null && entityList.Count > 0)
            {
                await this.FilterEntityListAsync(entityList);
                return entityList[0];
            }
            return null;
        }

        protected virtual void UnReleasePick(PickInfo pickInfo)
        {
            if (pickInfo == null)
            {
                return;
            }
            using (PickGeneratedProvider pickGeneratedProvider = this.CreatePickGenProvider(pickInfo.Id))
            {
                if (!this.IsValidToUnrelease(pickGeneratedProvider.Items))
                {
                    throw new Exception("Order must have a status of Assigned or Released ");
                }
                foreach (PickGenerated current in pickGeneratedProvider.Items)
                {
                    this.UnReleasePick(current);
                }
                pickGeneratedProvider.Update(this.CompId);
            }
        }

        protected virtual void UnReleasePick(PickGenerated entity)
        {
            if (this.IsValidToUnrelease(entity))
            {
                entity.MarkToDelete();
            }
        }

        protected virtual bool IsValidToUnrelease(EntityList<PickGenerated> items)
        {
            return ListBase<PickGenerated>.IsNullOrEmpty(items) || items.Count((PickGenerated x) => this.IsValidToUnrelease(x)) > 0;
        }

        protected virtual bool IsValidToUnrelease(PickGenerated entity)
        {
            return entity != null && (entity.OrderStatusType == OrderStatus.Assigned || entity.OrderStatusType == OrderStatus.Released) ;
        }

        protected virtual PickGeneratedProvider CreatePickGenProvider(string pickId)
        {
            SqlFilterBuilder<PickGeneratedBase.Columns> sqlFilterBuilder = new SqlFilterBuilder<PickGeneratedBase.Columns>();
            sqlFilterBuilder.AppendEquals(PickGeneratedBase.Columns.PickId, pickId);
            PickGeneratedProvider pgProvider = new PickGeneratedProvider();
            pgProvider.Load(this.CompId, new FilterCriteria(sqlFilterBuilder.ToString(), string.Empty));
            return pgProvider;
        }

        protected virtual void ReleaseSelectedItems(string generatedPickId, string assignedTo, DateTime pickDate, string description, EntityList<NewPickGenerate> entityList, string locId)
        {
            Guid.NewGuid();
            this.ProcessEngine = this.CreateNewProcessEngine();
            try
            {
                this.ProcessEngine.PickId = generatedPickId;
                this.ProcessEngine.SelectedNewOrders = entityList;
                this.ProcessEngine.LocId = locId;
                this.ProcessEngine.Picking.LocId = locId;
                this.ProcessEngine.Picking.Filter = string.Empty;
                this.ProcessEngine.Picking.AssignedTo = assignedTo;
                this.ProcessEngine.Picking.PickDate = pickDate;
                this.ProcessEngine.Picking.Description = description;
                this.ProcessEngine.Picking.Selected = this.BuildSelectedOrdersSourceValue(entityList);
                
                if (this.ProcessEngine.ValidateProperties())
                {
                    this.ProcessEngine.Execute(null);
                    this.UpdatePickInfo(this.ProcessEngine.Picking);
                }
                else
                {                    
                    this.DisplayInvalidProperties();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected virtual void UpdatePickInfo(PickInfo pickInfo)
        {
            try
            {
                new PickInfoProvider
                {
                    Items =
                    {
                        pickInfo
                    }
                }.Update(this.CompId);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected virtual void DisplayInvalidProperties()
        {
            if (this.ProcessEngine != null && this.ProcessEngine.InvalidPropertyList.Count > 0)
            {
                
                StringBuilder stringBuilder = new StringBuilder();
                foreach (EntityProperty<string> current in ProcessEngine.InvalidPropertyList)
                {
                    stringBuilder.AppendLine(string.Format("{0}", current.Value));
                }
                if (stringBuilder.Length > 0)
                {
                    throw new Exception(stringBuilder.ToString());
                }
            }
        }

        protected virtual int BuildSelectedOrdersSourceValue(EntityList<NewPickGenerate> entityList)
        {
            int retVal = 0;
            foreach (IGrouping<byte, NewPickGenerate> item in entityList.GroupBy(x => x.SourceId))
            {
                retVal = (retVal | (int)item.Key);
            }
            return retVal;
        }

        protected virtual ReleaseItems CreateNewProcessEngine()
        {
            return ProcessBase.LoadProcessEngine<ReleaseItems>(this.CompId);
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
            Action<PickGenerated> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PickGenerated);
        }
        #endregion  Event Handlers

        #region Properties
        protected PickGeneratedProvider Provider { get; } = new PickGeneratedProvider();

        protected SortedDictionary<string, Action<PickGenerated>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PickGenerated>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        
        public virtual ReleaseItems ProcessEngine
        {
            get;
            set;
        }
        #endregion Properties

        #region Fields
        public const string FunctionID = "662462eb-bad6-46d3-8292-55a8f423fedf";
        #endregion
    }
}
