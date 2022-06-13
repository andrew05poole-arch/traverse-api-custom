#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives


namespace TRAVERSE.Web.API.WarehouseManagement.Controllers
{
    public class ApiPackOrderDetailController : ApiControllerBase
    {
        #region Helper Methods
        [ApiRoute(FunctionID, 2f, "pack/{orderNo}/detail", typeof(PickGenerated))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, orderNo, id));
        }

        [ApiRoute(FunctionID, 2f, "pack/{orderNo}/detail/{id:int}", typeof(PickGenerated))]
        public async Task Delete(string orderNo, int id)
        {
            await MarkToDelete(orderNo, id);
        }
        #endregion Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {

        }
        #endregion Overrides

        #region Helper Region
        protected virtual async Task<EntityList<Pick>> Load(string orderNo, int? id)
        {
            var builder = new SqlFilterBuilder<PickBase.Columns>();
            builder.AppendEquals(PickBase.Columns.TransId, orderNo);
            Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (CurrentPick == null || CurrentPick.TransId != orderNo)
            {

                if (!id.HasValue || id <= 0)
                    Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                else
                {
                    builder.AppendEquals(PickBase.Columns.PickKey, id.ToString());
                    Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                }
            }
            await this.FilterEntityListAsync(this.Provider.Items);
            return Provider.Items;
        }

        protected virtual async Task<Pick> Find(string orderNo, int? id)
        {
            var list = await Load(orderNo, id);
            return list.Find(x => x.PickKey == id);
        }

        protected virtual async Task<EntityList<Pick>> ProcessEditRequest(bool isCreate, dynamic body, string orderNo, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new EntityList<Pick>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, orderNo, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }
            this.SavePickData(entityList);
            return entityList;
        }

        protected virtual async Task<Pick> ProcessBodyItem(bool isCreate, dynamic bodyItem, string orderNo, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PickKey) || bodyItem.PickKey == null)
                bodyItem.PickKey = code;
            else
                code = Convert.ToInt32(bodyItem.PickKey);

            var entity = await this.Find(orderNo, code);

            var builder = new SqlFilterBuilder<PickGeneratedBase.Columns>();
            builder.AppendEquals(PickGeneratedBase.Columns.TransId, orderNo);
            Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
            CurrentPickGen = (new PickGeneratedProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty)))[0];

            if (isCreate)
            {
                entity = CurrentPickGen.PickDetailList.AddNew();
                entity.LocId = CurrentPickGen.LocId;
                entity.ExtLocA = CurrentPickGen.ExtLocA;
                entity.TransId = orderNo;
                entity.ItemId = CurrentPickGen.ItemId;
                entity.LotNum = CurrentPickGen.LotNum;
                entity.EntryNum = CurrentPickGen.EntryNum;
                entity.SeqNum = CurrentPickGen.SeqNum;
                entity.RecordPickStatus = PickStatus.New;
                entity.PickType = WMPickType.Pack;
                entity.SetDefaults();
                entity.SetPickDefaults();
                entity.QtyPicked = 1m;
                int? num = null;
                entity.ExtLocBTo = num;
                entity.ExtLocATo = num;
            }

            if (entity == null)
                throw new InvalidValueException(string.Format("Pick Detail '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string orderNo, int id)
        {
            var entity = await this.Find(orderNo, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("ID {0} could not be found on Order'{1}'.", id, orderNo));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void SavePickData(EntityList<Pick> pickDetailList)
        {
            foreach (Pick pick in pickDetailList)
            {
                if (this.IsValidToAppend(CurrentPickGen, pick))
                {
                    pick.TransId = CurrentPickGen.TransId;
                    pick.SourceId = CurrentPickGen.SourceId;
                    pick.EntryNum2 = CurrentPickGen.EntryNum2;
                    pick.SeqNum = CurrentPickGen.SeqNum;
                    pick.UOM = CurrentPickGen.UOM;
                }
            }
            this.UpdatePickGen(CurrentPickGen);
        }

        protected virtual void UpdatePickGen(PickGenerated pickGenerated)
        {
            if (pickGenerated == null)
            {
                return;
            }
            this.SavePicks(pickGenerated.PickDetailList);
        }

        protected virtual void SavePicks(EntityList<Pick> pickDetailList)
        {
            if (pickDetailList.IsDirty)
            {
                PickProvider pickProvider = new PickProvider();
                pickProvider.Items.AddRange(pickDetailList);
                pickProvider.Items.DeletedItems.AddRange(pickDetailList.DeletedItems);
                pickProvider.Update(this.CompId);
                pickDetailList.DeletedItems.Clear();
            }
        }


        protected virtual bool IsValidToAppend(PickGenerated header, Pick pick)
        {
            IEnumerable<Pick> arg_67_0 = this.FindPicksByPackKey(header.PickGenKey);
            decimal d = header.QtyReq;
            decimal packQty = PickGenerated.GetPackQty(CurrentPickGen.PickDetailList.FindAll((Pick x) => this.IsValidPackPick(x, header, new PickStatus[]
            {
                PickStatus.Completed
            })));
            decimal packQty2 = PickGenerated.GetPackQty(CurrentPickGen.PickDetailList.FindAll((Pick x) => this.IsValidPackPick(x, header, new PickStatus[]
            {
                PickStatus.New,
                PickStatus.Confirmed
            })));
            if (pick.QtyPicked > (d - packQty2 - packQty))
            {
                throw new Exception("Cannot pack more than pick");
            }
            return true;
        }

        protected virtual List<Pick> FindPicksByPackKey(int id)
        {
            List<Pick> list;
            this.PackKeys.TryGetValue(id, out list);
            return list ?? new List<Pick>();
        }

        protected virtual Dictionary<int, List<Pick>> PackKeys
        {
            get
            {
                Dictionary<int, List<Pick>> pack;
                if ((pack = this._packKeys) == null)
                {
                    pack = (this._packKeys = new Dictionary<int, List<Pick>>());
                }
                return pack;
            }
        }

        protected virtual bool IsValidPackPick(Pick pick, PickGenerated pickGen, params PickStatus[] pickStatuses)
        {
            int? extLocA = (pickGen != null) ? pickGen.ExtLocA : null;
            int? extLocB = (pickGen != null) ? pickGen.ExtLocB : null;
            string lotNum = (pickGen != null) ? pickGen.LotNum : null;
            return this.IsValidPackPick(pick, extLocA, extLocB, lotNum, this.FindPackSerialNum(pickGen.PickGenKey), pickStatuses) && pick.PickType == WMPickType.Pack;
        }

        protected virtual bool IsValidPackPick(Pick pick, int? extLocA, int? extLocB, string lotNum, string serNum, params PickStatus[] pickStatuses)
        {
            return this.IsValidPick(pick, extLocA, extLocB, lotNum, serNum, pickStatuses);
        }

        protected virtual bool IsValidPick(Pick pick, PickGenerated pickGen, params PickStatus[] pickStatuses)
        {
            int? extLocA = (pickGen != null) ? pickGen.ExtLocA : null;
            int? extLocB = (pickGen != null) ? pickGen.ExtLocB : null;
            string lotNum = (pickGen != null) ? pickGen.LotNum : null;
            return this.IsValidPick(pick, extLocA, extLocB, lotNum, this.FindPackSerialNum(pickGen.PickGenKey), new PickStatus[0]);
        }

        protected virtual bool IsValidPick(Pick pick, int? extLocA, int? extLocB, string lotNum, string serNum, params PickStatus[] pickStatuses)
        {
            if (pick != null && StringHelper.AreEqual(lotNum, pick.LotNum) && StringHelper.AreEqual(serNum, pick.SerNum))
            {
                int? num = pick.ExtLocA;
                int? num2 = extLocA;
                if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num.HasValue == num2.HasValue)
                {
                    num2 = pick.ExtLocB;
                    num = extLocB;
                    if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2.HasValue == num.HasValue)
                    {
                        return pickStatuses.Contains(pick.RecordPickStatus);
                    }
                }
            }
            return false;
        }

        protected virtual string FindPackSerialNum(int id)
        {
            List<Pick> list = this.FindPicksByPackKey(id);
            if (list.Count > 0)
            {
                return list[0].SerNum;
            }
            return null;
        }
        protected virtual void ValidatePickItem()
        {
            if (ListBase<PickGenerated>.IsNullOrEmpty(this.PickGenList) || this.PickGenList.FindAll((PickGenerated x) => this.ValidPickGenItemYN(x, this.CurrentPick, this.CurrentPickGen)).Count == 0)
            {
                if (this.CurrentPickGen != null && this.CurrentPick != null && !string.IsNullOrEmpty(this.CurrentPick.ItemId))
                {
                    throw new InvalidValueException(string.Format("Item not found"));
                }
                this.CurrentPick.ItemId = (this.CurrentPick.ItemId = null);
            }
        }

        protected virtual bool ValidPickGenItemYN(PickGenerated pickGen, Pick pick, PickGenerated groupedHeader)
        {
            return pickGen != null && pick != null && StringHelper.AreEqual(pickGen.ItemId, pick.ItemId) && StringHelper.AreEqual(pickGen.Ref1, (groupedHeader != null) ? groupedHeader.Ref1 : string.Empty);
        }

        #endregion Helper Region

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Pick> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Pick);
        }
        #endregion Event Handlers

        #region Properties
        protected Pick CurrentPick { get; set; }
        protected PickGenerated CurrentPickGen { get; set; }
        protected PickProvider Provider { get; } = new PickProvider();
        protected EntityList<PickGenerated> PickGenList { get; set; } = new EntityList<PickGenerated>();

        private Dictionary<int, List<Pick>> _packKeys;
        protected SortedDictionary<string, Action<Pick>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Pick>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "cdfc27ad-7978-480b-99f1-168a112b1397";
        #endregion Fields
    }
}
