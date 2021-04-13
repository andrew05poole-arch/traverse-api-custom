#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WM;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TraverseApi;
using System.Linq;
using TRAVERSE.Business.Inventory;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMPutAwayDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "putaway/location/{locationid}/receivingbin/{bin}/item/{id?}", typeof(PutAway))]
        public async Task<IHttpActionResult> Get(string locationid = null, string bin = null, string id = null)
        {
            return Ok(await Load(locationid, bin , id));
        }

        [ApiRoute(FunctionID, 2f, "putaway/location/{locationid}/receivingbin/{bin}/item/{id?}", typeof(PutAway))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string locationId = null, string bin = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, locationId,bin,id));
        }

        [ApiRoute(FunctionID, 2f, "putaway/{id:int}", typeof(TransactionBatch))]
        public async Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.EntityPropertyDictionary.Add("WMContainerFrom",this.ContainerFromPropertyChanged);
            this.EntityPropertyDictionary.Add("WMContainerTo",this.ContainerToPropertyChanged);
            this.EntityPropertyDictionary.Add("WMBinTo",this.BinToPropertyChanged);
        }        

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.Entity is MoveQuantityHeader move)
            {
                if (StringHelper.AreEqual(args.FieldName, "MoveById", false))
                {
                    args.ActualValue = move.InItem?.ItemId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMBinTo", false))
                {
                    args.ActualValue = move.ExtLocAToId;
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
            if (args.Entity is MoveQuantitySerial moveSer)
            {
                if (StringHelper.AreEqual(args.FieldName, "MoveById", false))
                {
                    args.ActualValue = moveSer.InItem?.ItemId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMBinTo", false))
                {
                    args.ActualValue = moveSer.ExtLocAToId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMContainerTo", false))
                {
                    args.ActualValue = moveSer.ExtLocBToId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMContainerFrom", false))
                {
                    args.ActualValue = moveSer.ExtLocBFromId;
                }
                if (StringHelper.AreEqual(args.FieldName, "Qty", false))
                {
                    args.ActualValue = moveSer.Qty;
                }
                if (StringHelper.AreEqual(args.FieldName, "TransDate", false))
                {
                    args.ActualValue = moveSer.TransDate;
                }
            }
        }
        #endregion

        protected virtual async Task<EntityList<MoveQuantityHeader>> Load(string locationId, string bin, string id)
        {
            if (Provider.Items.Count <= 0 || ((!string.IsNullOrEmpty(locationId) && !string.IsNullOrEmpty(bin)) &&
                !Provider.Items.Exists(i => StringHelper.AreEqual(locationId, i.LocId, false) && StringHelper.AreEqual(locationId, i.BinInfo?.ExtLocId, false))))
            {
                int binId = this.LoadBinId(locationId,bin);                

                EntityList<PutAway> entityList = PutAwayProvider.GetEntityList(locationId, binId, ApplicationContext.CurrentUser, ApplicationContext.SessionId, PutAwayScreenFunctionId, this.CompId);
                if (!string.IsNullOrEmpty(id))
                    this.Provider.Items.AddRange(entityList.FindAll(put => put.ItemId == id));
                else
                    this.Provider.Items.AddRange(entityList);
                await this.FilterEntityListAsync(this.Provider.Items);
            }
            EntityList<MoveQuantityHeader> moveQty = new EntityList<MoveQuantityHeader>();
            this.Provider.Items.ForEach(putAway => {
                moveQty.AddRange(putAway.MovementList);
            });
            return moveQty;
        }

        protected virtual async Task<MoveQuantity> Find(string locationId, string bin, string itemId)
        {
            var list = await Load(locationId,bin, itemId);
            return list?.Find(x => x.LocId == locationId && x.BinTo?.ExtLocId == bin && x.InItem?.ItemId == itemId);
        }

        protected virtual async Task<List<MoveQuantity>> ProcessEditRequest(bool isCreate, dynamic body, string locationId, string bin , string itemId)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(itemId))
                throw new InvalidValueException("Call is ambiguous. Item ID is provided along with more than one record.");

            var entityList = new List<MoveQuantity>();
            foreach (dynamic item in list)
            {

                var entity = await this.ProcessBodyItem(isCreate, item, locationId, bin, itemId);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<MoveQuantity> ProcessBodyItem(bool isCreate, dynamic bodyItem, string locationId,string bin, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.MoveById) || bodyItem.MoveById == null)
                bodyItem.MoveById = code;
            else
                code = bodyItem.MoveById;
                        
            int binId = this.LoadBinId(locationId,bin);
            

            EntityList<PutAway> entityList = PutAwayProvider.GetEntityList(locationId, binId, ApplicationContext.CurrentUser, ApplicationContext.SessionId, PutAwayScreenFunctionId, this.CompId);

            if (entityList == null || entityList.Count == 0)
                throw new Exception(string.Format("There are no Items to put away on Location '{0}' and Bin '{1}'." ,locationId,bin));
                        
            PutAway putAway = entityList.FirstOrDefault(put => StringHelper.AreEqual(put.ItemId,code));
            if (putAway == null)
                throw new Exception(string.Format("The Item '{2}' can not be put away on Location '{0} and bin {1}",locationId,bin,code));
                       
            var entity = new PutAwayDetailHeader();

            putAway.MovementList.Add(entity);
            entity.LocId = locationId;
            entity.ExtLocAFrom = binId;
            entity.SetDefaults();
            entity.FunctionId = Guid.Parse(PutAwayScreenFunctionId);

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (!this.Provider.Items.Contains(putAway))
                this.Provider.Items.Add(putAway);

            this.FindParentRecord(this.Provider.Items,entity,locationId,binId);

            return entity;
        }
        
        protected virtual async Task MarkToDelete(int id)
        {
            MoveQuantityHeaderProvider mProvider = new MoveQuantityHeaderProvider();
            SqlFilterBuilder<MoveQuantity.Columns> mBuilder = new SqlFilterBuilder<MoveQuantityBase.Columns>();
            mBuilder.AppendEquals(MoveQuantityBase.Columns.Id, id.ToString());
            mProvider.Load(this.CompId, new FilterCriteria(mBuilder.ToString(), string.Empty));
            if (mProvider.Items == null || mProvider.Items.Count == 0)
                throw new Exception(string.Format("PutAway Entry '{0}' could not be found.", id));
            else
            {
                mProvider.Items[0].MarkToDelete();
                mProvider.Update(this.CompId);
            }
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "SerialList")
            {
                return this.CreateSerialEntry((MoveQuantityHeader)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        protected virtual MoveQuantitySerial CreateSerialEntry(MoveQuantityHeader header, dynamic bodyItem)
        {
            string serNum = bodyItem.SerNum;
            MoveQuantitySerial serial = header.SerialList?.Find(x => StringHelper.AreEqual(x.SerNum, serNum, false));
            if (serial != null)
                throw new InvalidValueException($"Serial Number '{bodyItem.SerNum}' already exists.");
            else
                serial = header.SerialList.AddNew();
                        
            serial.SetDefaults();            
            return serial;
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

        private void FindParentRecord(EntityList<PutAway> putAwayDetailList, PutAwayDetailHeader putAwayDetail, string locId, int binId)
        {
            PutAway parent = null;
            if (putAwayDetail != null && !ListBase<PutAway>.IsNullOrEmpty(putAwayDetailList))
            {
                EntityList<PutAway> entityList = PutAwayProvider.GetEntityList(locId, binId, ApplicationContext.CurrentUser, ApplicationContext.SessionId, PutAwayScreenFunctionId, this.CompId);
                EntityList<PutAway> list = entityList.FindAll(x => this.IsMatch(x, putAwayDetail));
                if (ListBase<PutAway>.IsNullOrEmpty(list))
                    throw new Exception("No items to Put Away");
                if (putAwayDetail.InItem != null && putAwayDetail.InItem.InventoryType == InventoryType.Serial)
                {
                    if (list.Any(x => x.EntriesList.Find(e => StringHelper.AreEqual(e.SerNum, putAwayDetail.SerialNumber) && e.Id != putAwayDetail.Id) != null))
                        throw new Exception($"Serial Number '{putAwayDetail.SerialNumber}' already exists.");
                }
                parent = list.FirstOrDefault();
                this.ValidateQuantities(parent, putAwayDetail);
            }
        }

        protected virtual void ValidateQuantities(PutAway putAway, PutAwayDetailHeader putAwayDetail)
        {
            if (putAwayDetail != null && putAway != null)
            {
                decimal qty = putAwayDetail.UomInfo == null ? putAwayDetail.Qty : putAwayDetail.UomInfo.ConvertToBase(putAwayDetail.Qty);
                if ((putAway.QtyRemainingByUser - qty) < decimal.Zero)
                    throw new Exception(string.Format("Quantity is greater than required for Item ID: '{0}'", putAway.ItemId));
            }
        }

        protected virtual bool IsMatch(PutAway putAway, PutAwayDetailHeader putAwayDetail)
        {
            return putAway != null && StringHelper.AreEqual(putAway.ItemId, putAwayDetail.MoveById) && StringHelper.AreEqual(putAway.LocId, putAwayDetail.LocId) && putAway.ExtLocA == putAwayDetail.ExtLocAFrom && StringHelper.AreEqual(putAway.LotNum ?? string.Empty, putAwayDetail.LotNum ?? string.Empty) && putAway.ExtLocB == putAwayDetail.ExtLocBFrom;
        }
        #endregion

        #region BodyItem Update Methods
        protected virtual void BinToPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "WMBinTo" && args.Entity is PutAwayDetailHeader entity)
            {
                if (args.ActualValue != null)
                    entity.ExtLocATo = this.LoadBinId(entity.LocId, args.ActualValue?.ToString());
            }
        }

        protected virtual void ContainerToPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "WMContainerTo" && args.Entity is PutAwayDetailHeader entity)
            {
                if (args.ActualValue != null)
                    entity.ExtLocBTo = this.LoadContainerId(args.ActualValue?.ToString());
            }
        }

        protected virtual void ContainerFromPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "WMContainerFrom" && args.Entity is PutAwayDetailHeader entity)
            {
                if (args.ActualValue != null)
                    entity.ExtLocBFrom = this.LoadContainerId(args.ActualValue?.ToString());
            }
        }
        #endregion

        #region Event Handlers 
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (this.EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<MoveQuantity> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as MoveQuantity);
        }
        #endregion

        #region Properties
        protected PutAwayProvider Provider { get; } = new PutAwayProvider();

        protected SortedDictionary<string, Action<MoveQuantity>> PropertyDictionary { get; } = new SortedDictionary<string, Action<MoveQuantity>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "1b0ebfc8-a19c-43f4-bec9-9452340b2e2e";

        public const string PutAwayScreenFunctionId = "204e3c3e-a936-4aff-a65b-9cac0df9a411";
        #endregion
    }
}
