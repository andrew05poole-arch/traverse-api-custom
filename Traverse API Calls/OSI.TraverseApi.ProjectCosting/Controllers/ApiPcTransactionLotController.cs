#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.ProjectCosting;
using W = TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using PC = TRAVERSE.Business.ProjectCosting;
using T = System.Threading.Tasks;
#endregion Using Directives

namespace TRAVERSE.Web.API.ProjectCosting.Controllers
{
    public class ApiPcTransactionLotController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/extended/{id:int?}", typeof(TransactionExt))]
        public async Task<IHttpActionResult> Get(int transId, int? id = null)
        {
            return Ok(await Load(transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/extended/{id:int?}", typeof(TransactionExt))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/extended/{id:int?}", typeof(TransactionExt))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/extended/{id:int}", typeof(TransactionExt))]
        public async T.Task Delete(int transId, int id)
        {
            await MarkToDelete(transId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //bodyItem
            this.EntityPropertyDictionary.Add(TransactionExtBase.Columns.ExtLocA.ToString(), BinPropertyChanged);
            this.EntityPropertyDictionary.Add(TransactionExtBase.Columns.ExtLocB.ToString(),ContainerPropertyChanged);
            
            //Lot Item Property Changes
            PropertyDictionary.Add(TransactionExtBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            PropertyDictionary.Add(TransactionExtBase.Columns.ExtLocA.ToString(), (entity) =>
            {
                if (string.IsNullOrEmpty(entity.LotNum))
                    entity.SetCostDefault();
            });
            PropertyDictionary.Add(TransactionExtBase.Columns.ExtLocB.ToString(), (entity) =>
            {
                if (string.IsNullOrEmpty(entity.LotNum))
                    entity.SetCostDefault();
            });

            EntityPropertyDictionary.Add(TransactionExtBase.Columns.UnitCost.ToString(), ValidateCostUnitFgn);
            EntityPropertyDictionary.Add(TransactionExtBase.Columns.LotNum.ToString(), ValidateSerialLotNumber);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is TransactionExt transactionExt)
            {
                if (StringHelper.AreEqual(args.FieldName, TransactionExtBase.Columns.ExtLocA.ToString(), false))
                {
                    args.ActualValue = (EntityProvider.GetEntityList<W.ExtLocationBin, W.ExtLocationBinProvider>(this.CompId, null, null)?.Find(x => StringHelper.AreEqual(x.Id.ToString(), args.ActualValue.ToString(), false) && x.LocId == CurrentTransaction.LocId) as W.ExtLocationBin).ExtLocId;
                }

                if (StringHelper.AreEqual(args.FieldName, TransactionExtBase.Columns.ExtLocB.ToString(), false))
                {
                    args.ActualValue = (EntityProvider.GetEntityList<W.ExtLocationContainer, W.ExtLocationContainerProvider>(this.CompId, null, null)?.Find(x => StringHelper.AreEqual(x.Id.ToString(), args.ActualValue.ToString(), false)) as W.ExtLocationContainer).ExtLocId;
                }
            }
        }
        protected virtual async Task<EntityList<TransactionExt>> Load(int transId, int? id)
        {
            var list = CurrentTransaction?.ExtendedEntityList;

            if (CurrentTransaction == null || CurrentTransaction.Id != transId)
            {
                var builder = new SqlFilterBuilder<TransactionBase.Columns>();
                builder.AppendEquals(TransactionBase.Columns.Id, transId.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPcTransactionController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", transId));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.ExtendedEntityList;
                await this.FilterEntityListAsync(list, ApiPcTransactionLotController.FunctionID);
            }

            if (id.HasValue)
                return list.FindAll(TransactionExtBase.Columns.Id, id.Value);

            return list;
        }

        protected virtual async Task<TransactionExt> Find(int transId, int id)
        {
            var list = await Load(transId, id);
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<TransactionExt>> ProcessEditRequest(bool isCreate, dynamic body, int transId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new List<TransactionExt>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            RecalculateTotals();
            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionExt> ProcessBodyItem(bool isCreate, dynamic bodyItem, int transId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(transId, code);

            if (CurrentTransaction != null)
            {
                if (CurrentTransaction.PCTransactionType == PC.TransactionType.Expense || CurrentTransaction.PCTransactionType == PC.TransactionType.Other)
                    throw new InvalidValueException(string.Format("Current Transaction Type: '{0}' does not support adding extended items.", CurrentTransaction.PCTransactionType));
            }

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentTransaction?.ExtendedEntityList?.AddNew();
                entity.QtyNeed = CurrentTransaction.QtyNeed;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Extended ID {0} could not be found on Transaction ID '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(int transId, int id)
        {
            var entity = await this.Find(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Extended ID {0} could not be found on Transaction ID '{1}'.", id, transId));

            CurrentTransaction.ExtendedEntityList.Remove(entity);
            RecalculateTotals();
            this.Provider.Update(this.CompId);
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

        protected virtual void LotNumberPropertyChanged(TransactionExt entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || entity.InItem == null
                || !entity.InItem.IsLotted)
                return;

            Lot lot = entity.InItem?.AllLocations?.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && lot == null)
            {
                if (((Transaction)entity.ParentEntity).PCTransactionType == PC.TransactionType.MaterialReturn)
                    entity.NewLot = this.CreateBrandNewLot(entity.InItem.ItemId, entity.LocId, entity.LotNum);
                else
                    throw new InvalidValueException(string.Format("'{0}' is not on file.", entity.LotNum));
            }
            entity.SetCostDefault();
        }

        protected virtual void ValidateCostUnitFgn(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionExt lotInfo)
                    if (((Transaction)lotInfo.ParentEntity).PCTransactionType == PC.TransactionType.MaterialRequisition)
                        e.Handled = true;            
            }
        }

        protected virtual void ValidateSerialLotNumber(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionExt lotInfo)
                {
                    if (!((Transaction)lotInfo.ParentEntity).InItem.IsLotted)
                        e.Handled = true;
                }
            }
        }

        protected virtual void RecalculateTotals()
        {
            CurrentTransaction.Calculate();
        }

        #region Body Item Update Methods
        protected virtual void BinPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if(args.Entity is TransactionExt transactionExt)
            {
                args.ActualValue = (EntityProvider.GetEntityList<W.ExtLocationBin, W.ExtLocationBinProvider>(this.CompId, null, null)?.Find(x => StringHelper.AreEqual(x.ExtLocId, bodyItem.bin,false) && x.LocId == CurrentTransaction.LocId) as W.ExtLocationBin).Id;
            }
        }

        protected virtual void ContainerPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is TransactionExt transactionExt)
            {
                args.ActualValue = (EntityProvider.GetEntityList<W.ExtLocationContainer, W.ExtLocationContainerProvider>(this.CompId, null, null)?.Find(x => StringHelper.AreEqual(x.ExtLocId, bodyItem.container,false)) as W.ExtLocationContainer).Id;
            }
        }
        #endregion Body Item Update Methods
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
            Action<TransactionExt> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionExt);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionProvider Provider { get; } = new TransactionProvider();

        protected Transaction CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransactionExt>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionExt>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "FD74C61A-75AC-4F52-A78C-441C5E4E32EA";
        #endregion Fields
    }
}
