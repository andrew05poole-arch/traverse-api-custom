#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.WM;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.WarehouseManagement.Controllers
{
    public class ApiWMMoveQuantityController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "movequantity/entry/{id:int?}", typeof(MoveQuantityHeader))]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "movequantity/entry/{id:int?}", typeof(MoveQuantityHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "movequantity/entry", typeof(MoveQuantityHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "movequantity/write/{id:int?}", typeof(MoveQuantityHeader))]
        public async Task<IHttpActionResult> Write(int? id = null)
        {
            return Ok(await this.ProcessMoveQuantity(id));
        }

        [ApiRoute(FunctionID, 2f, "movequantity/entry/{id:int}", typeof(MoveQuantityHeader))]
        public async Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.EntityPropertyDictionary.Add("WMBinFrom", this.BinFromPropertyChanged);
            this.EntityPropertyDictionary.Add("WMBinTo", this.BinToPropertyChanged);
            this.EntityPropertyDictionary.Add("WMContainerFrom", this.ContainerFromPropertyChanged);
            this.EntityPropertyDictionary.Add("WMContainerTo", this.ContainerToPropertyChanged);
            this.EntityPropertyDictionary.Add("MoveById", this.MoveByIdPropertyChanged);
        }
        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.Entity is MoveQuantityHeader move)
            {
                if (StringHelper.AreEqual(args.FieldName, "WMBinTo", false))
                {
                    args.ActualValue = move.ExtLocAToId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMBinFrom", false))
                {
                    args.ActualValue = move.ExtLocAFromId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMContainerTo", false))
                {
                    args.ActualValue = move.ExtLocBToId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMContainerFrom", false))
                {
                    args.ActualValue = move.ExtLocBFromId;
                }
            }
        }

        #endregion Overrides
        protected virtual async Task<EntityList<MoveQuantityHeader>> Load(int? id)
        {
            if (this.Provider.Items.Count <= 0 || (id.HasValue && !this.Provider.Items.Exists(i => i.Id == id)))
            {
                if (!id.HasValue)
                    await Provider.Load<MoveQuantityHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<MoveQuantityBase.Columns>();
                    builder.AppendEquals(MoveQuantityBase.Columns.Id, id.ToString());
                    builder.AppendEquals(MoveQuantityBase.Columns.FunctionId, MoveQuantityFunctionId);
                    var list = new MoveQuantityHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }
            return this.Provider.Items;
        }
        protected virtual async Task<MoveQuantityHeader> Find(int? id)
        {
            var list = await Load(id);
            return list.Find(x => x.Id == id);
        }
        protected virtual async Task<List<MoveQuantityHeader>> ProcessEditRequest(bool isCreate, dynamic bodyItem, int? id = null)
        {
            object[] list;

            if (bodyItem is object[])
                list = bodyItem;
            else
                list = new object[1] { bodyItem };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Move Quantities is provided along with more than one record.");

            var entityList = new List<MoveQuantityHeader>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider?.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }
            await ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }
        protected virtual async Task<MoveQuantityHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id = null)
        {
            int? code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            Workstation = ApiUserSkipped.IsApiUserSkipped(bodyItem.HostId) ? ApplicationContext.SessionId : bodyItem.HostId;
            UserPick = ApiUserSkipped.IsApiUserSkipped(bodyItem.UId) ? ApplicationContext.CurrentUser : bodyItem.UId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = new MoveQuantityHeader(this.CompId);
                entity.UId = UserPick;
                entity.HostId = Workstation;
                entity.SetDefaults();
                entity.SetMoveByIdDefaults();
                entity.FunctionId = Guid.Parse(MoveQuantityFunctionId);
                entity.MovementType = QuantityMovementType.ItemId;
                string userLocationID = Utility.GetUserLocationID(this.CompId);
                if (!string.IsNullOrEmpty(userLocationID))
                {
                    entity.LocId = userLocationID;
                }
                else
                {
                    entity.LocId = ConfigurationValueProvider.GetRule<string>("SM", "WhseID", this.CompId);
                }
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Move Quantity '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }
        protected virtual async Task MarkToDelete(int id)
        {
            var employee = await this.Find(id);

            if (employee == null)
                throw new NothingToProcessException(string.Format("Move Quantity '{0}' could not be found.", id));
            else
            {
                this.Provider.Items.Remove(employee);
                this.Provider.Update(this.CompId);
            }
        }
        protected virtual async Task<EntityList<MoveQuantityHeader>> LoadWrite(int? id)
        {
            if (this.Provider.Items.Count <= 0 || (id.HasValue && !this.Provider.Items.Exists(i => i.Id == id)))
            {
                var builder = new SqlFilterBuilder<MoveQuantityBase.Columns>();
                builder.AppendEquals(MoveQuantityBase.Columns.HostId, TRAVERSE.Core.ApplicationContext.SessionId);
                builder.AppendEquals(MoveQuantityBase.Columns.UId, TRAVERSE.Core.ApplicationContext.CurrentUser);

                if (!id.HasValue)
                {
                    var list = new MoveQuantityHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                else
                {
                    builder.AppendEquals(MoveQuantityBase.Columns.Id, id.ToString());
                    builder.AppendEquals(MoveQuantityBase.Columns.FunctionId, MoveQuantityFunctionId);
                    var list = new MoveQuantityHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }
            return this.Provider.Items;
        }
        protected virtual async Task<MoveQuantityHeader> FindWrite(int? id)
        {
            var list = await LoadWrite(id);
            return list.Find(x => x.Id == id);
        }
        protected virtual async Task<MoveQuantityHeader> ProcessMoveQuantity(int? id)
        {
            var entity = await this.FindWrite(id);

            ProcessMoveQuantityPost();

            Provider.Items.Clear();
            return entity;
        }
        protected virtual bool ProcessMoveQuantityPost()
        {
            if (this.Provider.Items.IsValid && this.PostingEngine.CheckClosedPeriod() && this.VerifyNegativeInventory())
            {
                this.PostingEngine.ProcessId = null;
                this.PostingEngine.PostRun = null;
                this.PostingEngine.ResetLists();
                this.PostingEngine.SetDefaults();
                this.PostingEngine.Comments = string.Empty;
                this.PostingEngine.TransactionList.AddRange(this.Provider.Items);
                this.PostingEngine.Execute(null);
                return true;
            }
            return false;
        }
        protected virtual bool VerifyNegativeInventory()
        {
            return ListBase<MoveQuantityHeader>.IsNullOrEmpty(this.Provider.Items.FindAll(new Predicate<MoveQuantityHeader>(this.FindNegativeMovement))) || this.VerifyNegativeInventory(Utility.GetNegativeInventory(this.CompId), this.Provider.Items.FindAll(new Predicate<MoveQuantityHeader>(this.FindNegativeMovement)));
        }
        protected virtual bool FindNegativeMovement(MoveQuantity moveQuantity)
        {
            return moveQuantity != null && moveQuantity.MovementType == QuantityMovementType.ItemId && moveQuantity.InItem != null && moveQuantity.QtyBase > decimal.Zero;
        }
        protected virtual bool VerifyNegativeInventory(byte negativeInvOpt, EntityList<MoveQuantityHeader> quantities)
        {
            if (ListBase<MoveQuantityHeader>.IsNullOrEmpty(quantities) || negativeInvOpt == 0)
            {
                return true;
            }
            using (NegativeInventoryProcess negativeInventoryProcess = this.InitNegativeInventoryProcess())
            {
                if (negativeInventoryProcess != null)
                {
                    negativeInventoryProcess.NegativeList.AddRange(quantities);
                    negativeInventoryProcess.Execute(null);
                }
            }
            return true;
        }
        protected virtual NegativeInventoryProcess InitNegativeInventoryProcess()
        {
            return ProcessBase.LoadProcessEngine<NegativeInventoryProcess>(this.CompId);
        }
        protected virtual int LoadContainerId(string container)
        {
            int containerId = 0;
            ExtLocationContainerProvider contProvider = new ExtLocationContainerProvider();
            SqlFilterBuilder<ExtLocationBin.Columns> contBuilder = new SqlFilterBuilder<ExtLocationBase.Columns>();
            contBuilder.AppendEquals(ExtLocationBase.Columns.ExtLocId.ToString(), container);
            contBuilder.AppendIsNull(ExtLocationBase.Columns.LocId.ToString());
            contBuilder.AppendEquals(ExtLocationBase.Columns.Type.ToString(), "1");
            contProvider.Load(this.CompId, new FilterCriteria(contBuilder.ToString(), string.Empty));
            if (contProvider.Items != null && contProvider.Items.Count > 0)
                containerId = contProvider.Items[0].Id;

            if (containerId == 0)
                throw new Exception(string.Format("Container {0} could not be found.", container));

            return containerId;
        }
        protected virtual int LoadBinId(string locationId, string bin)
        {
            int binId = 0;
            ExtLocationBinProvider binProvider = new ExtLocationBinProvider();
            SqlFilterBuilder<ExtLocationBin.Columns> binBuilder = new SqlFilterBuilder<ExtLocationBase.Columns>();
            binBuilder.AppendEquals(ExtLocationBase.Columns.ExtLocId.ToString(), bin);
            binBuilder.AppendEquals(ExtLocationBase.Columns.LocId.ToString(), locationId);
            binBuilder.AppendEquals(ExtLocationBase.Columns.Type.ToString(), "0");
            binProvider.Load(this.CompId, new FilterCriteria(binBuilder.ToString(), string.Empty));
            if (binProvider.Items != null && binProvider.Items.Count > 0)
                binId = binProvider.Items[0].Id;

            if (binId == 0)
                throw new Exception(string.Format("Bin {0} could not be found on Location {1}", bin, locationId));

            return binId;
        }
        public virtual void OnMoveQuantityHeaderPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (!(args.FieldName == "MoveById"))
            {
                if (!(args.FieldName == "LocId"))
                {
                    return;
                }
                this.CurrentMoveQty.ExtLocAFrom = null;
                this.CurrentMoveQty.ExtLocATo = null;
            }
            else if (this.CurrentMoveQty.MovementType == QuantityMovementType.ItemId)
            {
                if (!string.IsNullOrEmpty(this.CurrentMoveQty.MoveById))
                {
                    this.CurrentMoveQty.LocId = null;
                    this.CurrentMoveQty.UOM = null;
                    if (this.CurrentMoveQty.InItem != null && this.CurrentMoveQty.InItem.InventoryStatus == InventoryStatus.Superseded)
                    {
                        this.CurrentMoveQty.MoveById = this.CurrentMoveQty.InItem.SuperId;
                    }
                }
                if (this.CurrentMoveQty.InItem != null)
                {
                    if (this.CurrentMoveQty.InItem.AllLocations.Count > 0)
                    {
                        this.CurrentMoveQty.LocId = this.CurrentMoveQty.InItem.AllLocations[0].LocId;
                    }
                    this.CurrentMoveQty.UOM = this.CurrentMoveQty.InItem.UomDflt;
                }
                return;
            }
        }
        protected virtual void BinFromPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "WMBinFrom" && args.Entity is MoveQuantityHeader entity)
            {
                if (args.ActualValue != null)
                    entity.ExtLocAFrom = this.LoadBinId(entity.LocId, args.ActualValue?.ToString());
            }
        }
        protected virtual void BinToPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "WMBinTo" && args.Entity is MoveQuantityHeader entity)
            {
                if (args.ActualValue != null)
                    entity.ExtLocATo = this.LoadBinId(entity.LocId, args.ActualValue?.ToString());
            }
        }
        protected virtual void ContainerToPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "WMContainerTo" && args.Entity is MoveQuantityHeader entity)
            {
                if (args.ActualValue != null)
                    entity.ExtLocBTo = this.LoadContainerId(args.ActualValue?.ToString());
            }
        }
        protected virtual void ContainerFromPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "WMContainerFrom" && args.Entity is MoveQuantityHeader entity)
            {
                if (args.ActualValue != null)
                    entity.ExtLocBFrom = this.LoadContainerId(args.ActualValue?.ToString());
            }
        }
        protected virtual void MoveByIdPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "MoveById" && args.Entity is MoveQuantityHeader entity)
            {
                if (string.IsNullOrEmpty(entity.MoveById))
                    entity.MoveById = args.ActualValue?.ToString();
                if (string.IsNullOrEmpty(entity.UOM))
                    entity.UOM = entity.InItem?.UomDflt;
            }
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
            Action<MoveQuantityHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as MoveQuantityHeader);
        }
        #endregion Event Handler

        #region Properties
        protected MoveQuantityHeader CurrentMoveQty { get; set; }
        protected MoveQuantityHeaderProvider Provider { get; } = new MoveQuantityHeaderProvider();
        protected virtual MoveQuantityPost PostingEngine
        {
            get
            {
                if (this._moveQuantityPost == null)
                {
                    this._moveQuantityPost = TransactionPostBase.LoadPostingEngine<MoveQuantityPost>(this.CompId);
                }
                return this._moveQuantityPost;
            }
        }
        protected SortedDictionary<string, Action<MoveQuantityHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<MoveQuantityHeader>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        protected string Workstation { get; set; }
        protected string UserPick { get; set; }
        #endregion Properties

        #region Fields
        private MoveQuantityPost _moveQuantityPost;
        public const string FunctionID = "b546cb1f-b270-4748-9fc4-02e1575f069d";
        public const string MoveQuantityFunctionId = "28F7D69C-BDCD-4C1D-BE49-031DDE130C6B";
        #endregion Fields
    }
}
