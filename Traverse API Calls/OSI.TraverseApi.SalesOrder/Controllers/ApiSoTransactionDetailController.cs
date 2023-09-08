#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.SalesOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.SalesOrder.Controllers
{
    public class ApiSoTransactionDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiSoTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Get(string transId, int? id = null)
        {
            return Ok(await Load(transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiSoTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiSoTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int}", typeof(TransactionDetailLineItem), new object[] { ApiSoTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task Delete(string transId, int id)
        {
            await this.MarkToDelete(transId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Line Item Property Changes
            PropertyDictionary.Add(TransactionDetailBase.Columns.ItemId.ToString(), ItemIdPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.LocId.ToString(), WhsePropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.TotQtyOrdSell.ToString(), QtyPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.PriceAdjType.ToString(), (entity) => entity.ResetPriceAdjustment());
            PropertyDictionary.Add(TransactionDetailBase.Columns.PriceAdjPct.ToString(), (entity) => entity.CalculatePriceAdjustment());
            PropertyDictionary.Add(TransactionDetailBase.Columns.PriceId.ToString(), (entity) => GetUnitPrice(entity, true));
            PropertyDictionary.Add(TransactionDetailBase.Columns.UnitPriceSellBasisFgn.ToString(), UnitPriceSellPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.Rep1Id.ToString(), (entity) => entity.SetSalesRepDefaults(1));
            PropertyDictionary.Add(TransactionDetailBase.Columns.Rep2Id.ToString(), (entity) => entity.SetSalesRepDefaults(2));
            PropertyDictionary.Add(TransactionDetailBase.Columns.CatId.ToString(), (entity) =>
            {
                entity.SetSalesRepDefaults(1);
                entity.SetSalesRepDefaults(2);
            });
            PropertyDictionary.Add(TransactionDetailBase.Columns.UnitsSell.ToString(), UnitsSellPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.TaxClass.ToString(), (entry) => CurrentTransaction.ResetTaxAdjustment());
            PropertyDictionary.Add(TransactionDetailBase.Columns.AcctCode.ToString(), (entity) => { if (!string.IsNullOrWhiteSpace(entity.AcctCode)) entity.SetGLAccountDefaults(entity.AcctCode); });
            PropertyDictionary.Add(TransactionDetailBase.Columns.UnitCostSellFgn.ToString(), UnitCostPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.QtyShipSell.ToString(), QtyShipPropertyChanged);

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransactionDetailExtBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            LotPropertyDictionary.Add(TransactionDetailExtBase.Columns.CostUnitFgn.ToString(), (entity) =>
            {
                entity.Calculate();
                entity.UpdateCosts(true);
            });

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(), (entity) => entity.UpdateCosts(true));
        }

        protected virtual async Task<EntityList<TransactionDetailLineItem>> Load(string transId, int? id)
        {
            var list = CurrentTransaction?.DetailList as EntityList<TransactionDetailLineItem>;

            if (CurrentTransaction == null || !StringHelper.AreEqual(CurrentTransaction.TransId, transId, false))
            {
                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiSoTransactionOrderController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction '{0}' could not be found.", id));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.DetailList as EntityList<TransactionDetailLineItem>;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(TransactionDetailBase.Columns.EntryNum, id.Value);

            return list;
        }

        protected virtual async Task<TransactionDetailLineItem> Find(string transId, int id)
        {
            var list = await Load(transId, id);
            return list.Find(x => x.EntryNum == id);
        }

        protected virtual async Task<List<TransactionDetailLineItem>> ProcessEditRequest(bool isCreate, dynamic body, string transId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Line Item is provided along with more than one record.");

            var entityList = new List<TransactionDetailLineItem>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            entityList.ForEach(d =>
             {
                 if (d.IsNew)
                     AppendKitComponents(d);
             });

            CurrentTransaction.CalculateTotals();
            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionDetailLineItem> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum) || bodyItem.EntryNum == null)
                bodyItem.EntryNum = code;
            else
                code = Convert.ToInt32(bodyItem.EntryNum);

            var entity = await this.Find(transId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentTransaction.LineItemList.AddNew();
                if (code != 0)
                    entity.EntryNum = code;

                entity.TotQtyOrdSell = 1M;
                if (CurrentTransaction.TransactionType == SOTransactionType.Invoice || CurrentTransaction.TransactionType == SOTransactionType.CreditMemo)
                    entity.ShipNeededQuantity();
                else if (CurrentTransaction.TransactionType == SOTransactionType.Verified)
                    entity.CalculateBackorderedQty();

                if (string.IsNullOrEmpty(CurrentTransaction.LocId))
                {
                    string userLocationID = Utility.GetUserLocationID(CompId);
                    if (!string.IsNullOrEmpty(userLocationID))
                        entity.LocationId = userLocationID;
                    else
                        entity.LocationId = ConfigurationValue.GetRule<string>("SM", "WhseID", this.CompId);
                }
                else
                    entity.LocationId = CurrentTransaction.LocId;

                entity.Unit = ConfigurationValue.GetRule<string>("SM", "Units", this.CompId);
                entity.SetGLAccountDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Line Item {0} not be found on transaction '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);

            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, int id)
        {
            var entity = await this.Find(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Line item {0} could not be found on transaction '{1}'", id, transId));

            CurrentTransaction.DetailList.Remove(entity);
            CurrentTransaction.CalculateTotals();
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.ParentObject is TransactionDetailLineItem)
            {
                if (args.PropertyName == "ExtendedList")
                {
                    if (((TransactionDetailLineItem)args.ParentObject).IsNew)
                        return this.CreateLot((TransactionDetailLineItem)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdateLot((TransactionDetailLineItem)args.ParentObject, args.ItemModel);
                }
                else if (args.PropertyName == "SerialList")
                {
                    if (((TransactionDetailLineItem)args.ParentObject).IsNew)
                        return this.CreateSerial((TransactionDetailLineItem)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdateSerial((TransactionDetailLineItem)args.ParentObject, args.ItemModel);
                }
            }
            return null;
        }

        protected virtual void GetUnitPrice(TransactionDetailLineItem lineItem, bool checkSerialDetailList)
        {
            string promoId = string.Empty;
            Decimal unitPrice = TransactionDetailLineItem.GetUnitPrice(ref promoId, lineItem);
            if (lineItem.InItem != null && lineItem.InItem.InventoryType == InventoryType.Serial)
            {
                if (checkSerialDetailList)
                {
                    if (lineItem.SerialList.Count > 0)
                    {
                        decimal totalSerial = 0M;

                        foreach (TransactionSerial serial in lineItem.SerialList)
                            totalSerial += serial.PriceUnitFgn;

                        Decimal avgSerial = Rounding.Round(totalSerial / Convert.ToDecimal(lineItem.SerialList.Count), Rounding.RoundingType.UnitPrice, string.Empty, this.CompId);

                        if (avgSerial != lineItem.UnitPriceSellBasisFgn.GetValueOrDefault())
                        {
                            lineItem.PromoId = string.Empty;
                            lineItem.UpdateUnitPrice(avgSerial);
                            lineItem.Calculate(false);
                        }
                        else if (unitPrice != lineItem.UnitPriceSellBasisFgn.GetValueOrDefault() && unitPrice != 0)
                        {
                            lineItem.PromoId = promoId;
                            lineItem.UpdateUnitPrice(unitPrice);
                            lineItem.Calculate(false);
                        }
                    }
                    else
                        lineItem.Calculate(false);
                }
                else
                {
                    if (unitPrice != lineItem.UnitPriceSellBasisFgn.GetValueOrDefault() && unitPrice != 0)
                    {
                        lineItem.PromoId = promoId;
                        lineItem.UpdateUnitPrice(unitPrice);
                    }
                }
            }
            else
            {
                if (unitPrice != lineItem.UnitPriceSellBasisFgn.GetValueOrDefault() && unitPrice != 0)
                {
                    lineItem.PromoId = promoId;
                    lineItem.UpdateUnitPrice(unitPrice);
                }
                else
                    lineItem.CalculatePriceAdjustment();
            }
        }

        #region Line Item Update Methods
        protected virtual void WhsePropertyChanged(TransactionDetailLineItem entity)
        {
            if (string.IsNullOrEmpty(entity.LocationId))
                return;

            entity.SetLocationDefaults();
            entity.SetDefaultCost();
            entity.SetDefaultPrice();
            entity.SetGLAccountDefaults(entity.AcctCode);
        }

        protected virtual void QtyPropertyChanged(TransactionDetailLineItem entity)
        {
            if (Utility.INYN && entity.InItem != null)
            {
                if (CurrentTransaction.TransactionType == SOTransactionType.CreditMemo || CurrentTransaction.TransactionType == SOTransactionType.Invoice)
                {
                    if (entity.IsTransactionLinked)
                        entity.CalculateBackorderedQty();
                    else
                        entity.ShipNeededQuantity();
                }
                else if (CurrentTransaction.TransactionType == SOTransactionType.Verified)
                    entity.CalculateBackorderedQty();

                this.GetUnitPrice(entity, false);
                if (entity.ExtendedList != null && entity.ExtendedList.Count == 1)
                    ((TransactionDetailExt)entity.ExtendedList[0]).QtyOrder = entity.QtyOrdSell.Value;

                if (entity.InItem.IsKit)
                    return;

                entity.SetDefaultCost();
                entity.SetDefaultPrice();
                entity.Calculate();
            }
        }

        protected virtual void QtyShipPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.CalculateBackorderedQty();
            if (Utility.INYN && entity.InItem != null && 
                entity.QtyShipSell.GetValueOrDefault() > 0 && 
                entity.InItem.InventoryType != InventoryType.Serial)
                this.GetUnitPrice(entity, false);

            if (entity.InItem != null && entity.InItem.IsKit)
                return;

            entity.CalculateExtendedCost();
            entity.CalculateExtendedPrice();
        }

        protected virtual void UnitCostPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.CalculateExtendedCost();
            entity.UpdateCosts(true);
        }

        protected virtual void UnitsSellPropertyChanged(TransactionDetailLineItem entity)
        {
            if (entity.InItem == null)
                return;

            if (entity.GetQtyToUseSell(false) > 0)
                this.GetUnitPrice(entity, false);

            entity.SetDefaultCost();
        }

        protected virtual void UnitPriceSellPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.CalculatePriceAdjustment();

            if (!Utility.GetSerialItemsUseDiscountPricing(this.CompId))
                entity.SynchronizeSerialList();
        }

        protected virtual void ItemIdPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.CustomerPartNumber = null;
            entity.SetDefaults();

            if (Utility.INYN)
            {
                if (entity.InItem == null)
                    return;

                entity.Unit = null;
                entity.LocationId = string.Empty;

                if (entity.InItem.InventoryStatus == InventoryStatus.Superseded && !string.IsNullOrWhiteSpace(entity.InItem.SuperId))
                    entity.ItemId = entity.InItem.SuperId;

                if (entity.InItem == null)
                    return;

                if (entity.InItem.InventoryStatus == InventoryStatus.Obsolete)
                    throw new InvalidValueException(string.Format("Item '{0}' is obsolete and is not valid for this transaction", entity.ItemId));

                if (entity.InItem.AllLocations.Count <= 0)
                    throw new InvalidValueException(string.Format("Item '{0}' has no locations", entity.ItemId));

                entity.SetDefaults();

                if (string.IsNullOrEmpty(entity.LocId))
                    entity.LocId = entity.InItem.AllLocations[0].LocId;

                entity.UpdateCosts(true);

                if (Utility.GetDefMinSalesQuantityYn(this.CompId) && CurrentTransaction.IsSale && entity.InItemSellingUnit != null &&
                    entity.InItemSellingUnit.MinSaleQty > entity.TotQtyOrdSell)
                {
                    entity.TotQtyOrdSell = entity.InItemSellingUnit.MinSaleQty;
                    entity.CalculateBackorderedQty();
                }

                if (entity.InItem.IsLotted || entity.InItem.InventoryType == InventoryType.Serial)
                {
                    entity.Quantity = 0M;
                    entity.CalculateExtendedPrice();
                }

                if (string.IsNullOrEmpty(entity.CustomerPartNumber))
                {
                    EntityList<Alias> entityList = Alias.FindCustomerItemByAlias(CompId, entity.ItemId, CurrentTransaction.CustId);
                    entityList.Sort(AliasBase.Columns.AliasId.ToString());
                    if (entityList.Count > 0)
                    {
                        entity.CustomerPartNumber = entityList[0].AliasId;
                    }
                }
                return;
            }

            if (entity.SmItem != null)
                entity.SetDefaults();
        }

        protected virtual void AppendKitComponents(TransactionDetailLineItem entity)
        {
            var list = KitDefinition.GetKitDefinitions(entity.CompId, entity.ItemId, entity.LocationId, entity.TransMan);
            var definition = list.Find("Source", "BM");
            if (definition != null)
            {
                KitDefinition.AppendKitComponents(entity, definition.Components);
            }
        }
        #endregion Line Item Update Methods

        #region Lot Update Methods
        protected virtual TransactionDetailExt UpdateLot(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Sequence number is required.");

            this.FilterEntityList(parent.ExtendedList, ApiSoTransactionLotController.FunctionId);
            var entity = ((EntityList<TransactionDetailExt>)parent.ExtendedList).Find(x => x.SeqNum == (int)bodyItem.SeqNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Extension record '{0}' for Line item '{1}' with Transaction ID '{2}' could not be found.", bodyItem.SeqNum, parent.EntryNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionDetailExt CreateLot(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            TransactionDetailExt entity = parent.ExtendedList.AddNew() as TransactionDetailExt;
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
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

        protected virtual void LotNumberPropertyChanged(TransactionDetailExt entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || ((TransactionDetailLineItem)entity.LineItem).InItem == null
                || !((TransactionDetailLineItem)entity.LineItem).InItem.IsLotted)
                return;

            entity.SetDefaults();


            Lot lot = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId)?.LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, false);

            if (entity.IsNew &&
                ((TransactionHeader)entity.LineItem.Parent).TransactionType == SOTransactionType.CreditMemo &&
                ((TransactionHeader)entity.LineItem.Parent).TransactionType == SOTransactionType.RMA &&
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

            ValidateQty(entity);
        }

        protected virtual void LotUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionDetailExt;
            entity.PropertyChanged -= LotEntity_PropertyChanged;
        }

        protected virtual void ValidateQty(TransactionDetailExt entity)
        { }
        #endregion Lot Update Methods

        #region Serial Update Methods
        protected virtual TransactionSerial UpdateSerial(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            this.FilterEntityList(parent.SerialList, ApiSoTransactionSerialController.FunctionID);
            TransactionSerial entity = (parent.SerialList as EntityList<TransactionSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Line item '{1}' with Transaction ID '{2}' could not be found.",
                    bodyItem.SerNum, parent.EntryNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionSerial CreateSerial(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            TransactionSerial entity = (parent.SerialList as EntityList<TransactionSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Line item '{1}' with Transaction ID '{2}' already exists.",
                    bodyItem.SerNum, parent.EntryNum, parent.TransId));

            entity = (parent.SerialList as EntityList<TransactionSerial>).AddNew();

            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void SerialNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum)
                || entity.LineItem.InItem == null
                || entity.LineItem.InItem.InventoryType != InventoryType.Serial)
                return;

            var serial = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId)?.SerialItems.Find(SerialItemBase.Columns.SerNum, entity.SerNum, false);

            if (serial == null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' is not on file.", entity.SerNum));

            entity.SetDefaults();
            if (((TransactionHeader)entity.LineItem.Parent).TransactionType == SOTransactionType.Invoice
                || ((TransactionHeader)entity.LineItem.Parent).TransactionType == SOTransactionType.Verified)
            {
                if (serial.SerialItemStatus == SerialItemStatus.Available)
                {
                    serial.SerialItemStatus = SerialItemStatus.Sold;
                }
                else
                    throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Sold.", entity.SerNum));
            }
            else if (serial.SerialItemStatus == SerialItemStatus.Sold)
            {
                serial.SerialItemStatus = SerialItemStatus.Available;
            }

            decimal price = entity.GetSerialItemPrice();
            if (Utility.GetSerialItemsUseDiscountPricing(this.CompId) &&
                price < entity.PriceUnitFgn)
            {
                entity.PriceUnitFgn = price;
            }
        }

        protected virtual void LotNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || ((TransactionDetailLineItem)entity.Parent).InItem == null
                || !((TransactionDetailLineItem)entity.Parent).InItem.IsLotted)
                return;

            entity.SetDefaults();


            Lot lot = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId)?.LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, false);

            if (entity.IsNew &&
                ((TransactionHeader)entity.LineItem.Parent).TransactionType == SOTransactionType.CreditMemo &&
                ((TransactionHeader)entity.LineItem.Parent).TransactionType == SOTransactionType.RMA &&
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

        protected virtual void SerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionSerial;
            entity.PropertyChanged -= SerialEntity_PropertyChanged;
        }
        #endregion Serial Update Methods
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
            Action<TransactionDetailLineItem> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionDetailLineItem);
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionDetailExt> action = null;
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionDetailExt);
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionSerial> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerial);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected TransactionHeader CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransactionDetailLineItem>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetailLineItem>>();

        protected SortedDictionary<string, Action<TransactionDetailExt>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetailExt>>();

        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "C16315F8-8F21-419D-90C1-3176C35DDDC6";
        #endregion Properties
    }
}
