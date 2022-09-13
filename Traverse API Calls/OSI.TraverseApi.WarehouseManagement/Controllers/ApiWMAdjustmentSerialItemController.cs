#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.WarehouseManagement.Controllers
{
    public class ApiWMAdjustmentSerialItemController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionId, 2f, "adjustment/{transId}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> Get(string transId =null,  string id = null)
        {
            return Ok(await Load(transId, id));
        }

        [ApiRoute(FunctionId, 2f, "adjustment/{transId}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId =null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, id));
        }

        [ApiRoute(FunctionId, 2f, "adjustment/{transId}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, id));
        }

        [ApiRoute(FunctionId, 2f, "adjustment/{transId}/serial/{id}", typeof(TransactionSerial))]
        public async Task Delete(string transId, string id)
        {
            await this.MarkToDelete(transId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
        }

        protected virtual async Task<EntityList<TransactionSerial>> Load(string transId, string id)
        {
            EntityList<TransactionSerial> list = new EntityList<TransactionSerial>();
            var builder = new SqlFilterBuilder<TransactionBase.Columns>();
            builder.AppendEquals(TransactionBase.Columns.TransId, transId);
            CurrentTransaction = new TransactionProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty))[0];
            this.Provider.Items.Add(CurrentTransaction);
            await this.FilterEntityListAsync(CurrentTransaction.SerialList);

            if (id == null)
                list = CurrentTransaction.SerialList;
            else
                list = CurrentTransaction.SerialList?.FindAll(x => StringHelper.AreEqual(x.SerNum, id,false));

            return list;
        }

        protected virtual async Task<TransactionSerial> Find(string transId, string id)
        {
            var list = await Load(transId, id);
            return list?.Find(x => StringHelper.AreEqual(x.SerNum, id, false));
        }

        protected virtual async Task<List<TransactionSerial>> ProcessEditRequest(bool isCreate, dynamic body, string transId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Call is ambiguous. Serial Number is provided along with more than one record.");

            var entityList = new List<TransactionSerial>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionSerial> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum) || string.IsNullOrWhiteSpace(bodyItem.SerNum))
                bodyItem.SerNum = code;
            else
                code = bodyItem.SerNum;

            var entity = await this.Find(transId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = (CurrentTransaction.SerialList as EntityList<TransactionSerial>).AddNew();
                entity.SetPriceCostDefault();
                entity.SetBinContainerDefault();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Serial Num {0} could not be found on line item {1} on transaction '{2}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, string id)
        {
            var entity = await this.Find(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Serial Num {0} could not be found on line item {1} on transaction '{2}'", id, transId));

            CurrentTransaction.SerialList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void LotNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || ((Transaction)entity.Parent).ItemInfo == null
                || !((Transaction)entity.Parent).ItemInfo.IsLotted)
                return;

            Lot lot = ((Transaction)entity.Parent).ItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId)?.LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, false);

            if (entity.IsNew &&
                lot == null)
            {
                entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
            }
            else
            {
                if (lot == null)
                    throw new InvalidValueException(string.Format("'{0}' is not on file.", entity.LotNum));

                if (lot.ExpDate.GetValueOrDefault(DateTime.Today) < DateTime.Today)
                    this.AddWarnings("Lot is expired.");
            }
        }

        protected virtual Lot CreateBrandNewLot(string itemId, string locId, string lotNum)
        {
            Lot newLot = new Lot(this.CompId)
            {
                LotNum = lotNum,
                LocId = locId,
                ItemId = itemId,
                InitialDate = DateTime.Today
            };
            return newLot;
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
            Action<TransactionSerial> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerial);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionProvider Provider { get; } = new TransactionProvider();

        protected Transaction CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransactionSerial>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionId = "b6712265-f759-4ace-9b53-64e0e15cbf92";
        #endregion Properties
    }
}
