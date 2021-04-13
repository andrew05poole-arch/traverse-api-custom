#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.AccountsReceivable.Controllers
{
    public class ApiArTransactionOrderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiArTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiArTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiArTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id}", typeof(TransactionHeader), new object[] { ApiArTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Header Property Changes
            PropertyDictionary.Add(TransactionHeaderBase.Columns.ShipToId.ToString(), ShipToIdPropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxGrpId.ToString(), (entity) => RecalculateOrder(entity, true));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxableYN.ToString(), (entity) => RecalculateOrder(entity, entity.TaxableYN.GetValueOrDefault()));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.Rep1Id.ToString(), (entity) => entity.SetSalesRepDefaults(1));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.Rep2Id.ToString(), (entity) => entity.SetSalesRepDefaults(2));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TermsCode.ToString(), TermsCodePropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.InvcDate.ToString(), InvcDatePropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.OrgInvcNum.ToString(), (entity) => entity.SetOrginalInvoiceId());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.InvcNum.ToString(), (entity) => TransactionHeader.SynchronizePayments(entity));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.CurrencyId.ToString(), (entity) => entity.UpdateExchangeRate());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.DistCode.ToString(), (entity) => TransactionHeader.SynchronizePayments(entity));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.CustId.ToString(), CustIdPropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.FreightFgn.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxClassFreight.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MiscFgn.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxClassMisc.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.NonTaxSubtotalFgn.ToString(), (entity) => entity.ResetTaxAdjustment());
            EntityPropertyDictionary.Add("NetSalesTaxFgn", NetSalesTaxChanging);

            //Line Item Property Changes
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.PartId.ToString(), ItemIdPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.WhseId.ToString(), WhsePropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.AcctCode.ToString(), (entity) => { if (!string.IsNullOrWhiteSpace(entity.AcctCode)) entity.SetGLAccountDefaults(entity.AcctCode); });
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.UnitsSell.ToString(), UnitsSellPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.UnitCostSellFgn.ToString(), UnitCostPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.UnitPriceSellBasisFgn.ToString(), (entity) => entity.CalculateExtendedPrice());
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.QtyOrdSell.ToString(), QtyPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.QtyShipSell.ToString(), QtyShipPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.TaxClass.ToString(), (entry) => ((TransactionHeader)entry.Parent).ResetTaxAdjustment());

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransactionLotBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            LotPropertyDictionary.Add(TransactionLotBase.Columns.CostUnitFgn.ToString(), (entity) =>
            {
                entity.Calculate();
                entity.UpdateCosts(true);
            });

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(), (entity) => entity.UpdateCosts(true));

            //Payment Property Changes
            PaymentPropertyDictionary.Add(TransactionPaymentBase.Columns.PmtMethodId.ToString(), PmtMethodIdPropertyChanged);
            PaymentPropertyDictionary.Add(TransactionPaymentBase.Columns.ExchRate.ToString(), (entity) => { if (!string.IsNullOrEmpty(entity.PmtMethodId)) entity.Calculate(); });
            PaymentPropertyDictionary.Add(TransactionPaymentBase.Columns.PmtAmtFgn.ToString(), (entity) =>
            {
                entity.Calculate();
                entity.Parent.CalculateTotals();
            });
            PaymentPropertyDictionary.Add(TransactionPaymentBase.Columns.CurrencyId.ToString(), (entity) => entity.Parent.CalculateTotals());
        }

        protected virtual async Task<EntityList<TransactionHeader>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.TransId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<TransactionHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                    builder.AppendEquals(TransactionHeaderBase.Columns.TransId, id);
                    builder.AppendEquals(TransactionHeaderBase.Columns.VoidYn, "0");
                    var list = new TransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<TransactionHeader> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.TransId, id, false));
        }

        protected virtual async Task<List<TransactionHeader>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Transaction ID is provided along with more than one record.");

            var entityList = new List<TransactionHeader>();
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

        protected virtual async Task<TransactionHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || string.IsNullOrWhiteSpace(bodyItem.TransId))
                bodyItem.TransId = code;
            else
                code = bodyItem.TransId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new TransactionHeader(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (string.IsNullOrWhiteSpace(entity.TransId))
                entity.TransId = Provider.GetNextTransId();

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transaction '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "LineItemList")
            {
                if (((TransactionHeader)args.ParentObject).IsNew)
                    return this.CreateDetail((TransactionHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDetail((TransactionHeader)args.ParentObject, args.ItemModel);
            }
            else if (args.PropertyName == "PaymentList")
            {
                if (((TransactionHeader)args.ParentObject).IsNew)
                    return this.CreatePayment((TransactionHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdatePayment((TransactionHeader)args.ParentObject, args.ItemModel);
            }
            else
            {
                var lineItem = (TransactionDetailLineItem)args.ParentObject;
                if (args.PropertyName == "LotList")
                {
                    if (lineItem.InItem == null || !lineItem.InItem.IsLotted || lineItem.InItem.InventoryType == InventoryType.Serial)
                    {
                        args.Ignore = true;
                        return null;
                    }

                    if (lineItem.IsNew)
                        return this.CreateLot(lineItem, args.ItemModel);
                    else
                        return this.UpdateLot(lineItem, args.ItemModel);
                }
                else if (args.PropertyName == "SerialList")
                {
                    if (lineItem.InItem == null || lineItem.InItem.InventoryType != InventoryType.Serial)
                    {
                        args.Ignore = true;
                        return null;
                    }

                    if (lineItem.IsNew)
                        return this.CreateSerial(lineItem, args.ItemModel);
                    else
                        return this.UpdateSerial(lineItem, args.ItemModel);
                }
            }
            return null;
        }

        #region Header Update Methods
        protected virtual void ShipToIdPropertyChanged(TransactionHeader entity)
        {
            if (string.IsNullOrEmpty(entity.ShipToId))
                entity.SetShipToDefaults(false, false, false);
            else
            {
                entity.SetShipToDefaults(true, true, true);
                RecalculateOrder(entity, true);
            }
        }

        protected virtual void TermsCodePropertyChanged(TransactionHeader entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.TermsCode))
                entity.UpdateDueDateValues();

            entity.SetDiscountDate(DateTime.Today);
        }

        protected virtual void InvcDatePropertyChanged(TransactionHeader entity)
        {
            entity.SetFiscalPeriodYearFromDate(entity.InvcDate.GetValueOrDefault(DateTime.Today));
            entity.UpdateDueDateValues();
            if (entity.IsNew)
            {
                entity.SetExchangeRateBase();
                entity.UpdateExchangeRate();
            }
        }

        protected virtual void CustIdPropertyChanged(TransactionHeader entity)
        {
            if (entity.IsNew)
            {
                entity.SetCustomerDefaults();
                entity.UpdateExchangeRate();
            }
            entity.SetOrginalInvoiceId();
        }

        protected virtual void RecalculateOrder(TransactionHeader entity, bool resetTaxes)
        {
            if (resetTaxes)
                entity.ResetTaxAdjustment();

            entity.CalculateTotals();            
        }

        protected virtual void NetSalesTaxChanging(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            TransactionHeader header = e.Entity as TransactionHeader;
            e.Handled = true;

            if (header == null || !header.TaxableYN.GetValueOrDefault() || ApiUserSkipped.IsApiUserSkipped(bodyItem.NetSalesTaxFgn))
                return;

            decimal taxValue = 0M;
            decimal.TryParse(bodyItem.NetSalesTaxFgn.ToString(), out taxValue);
            header.TaxAmtAdjFgn = taxValue - header.SalesTaxFgn;
        }
        #endregion Header Update Methods

        #region Line Item Update Methods
        protected virtual TransactionDetailLineItem UpdateDetail(TransactionHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry number is required.");

            this.FilterEntityList(parent.DetailList, ApiArTransactionDetailController.FunctionID);
            TransactionDetailLineItem entity = (parent.DetailList as EntityList<TransactionDetailLineItem>).Find(TransactionDetailBase.Columns.EntryNum, (int)bodyItem.EntryNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Line item '{0}' with Transaction ID '{1}' could not be found.", bodyItem.EntryNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LineItemUpdateComplete;
            entity.PropertyChanged += LineItem_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionDetailLineItem CreateDetail(TransactionHeader parent, dynamic bodyItem)
        {
            TransactionDetailLineItem entity = (parent.DetailList as EntityList<TransactionDetailLineItem>).AddNew();

            entity.LineSeq = entity.EntryNum;
            entity.Quantity = 1M;
            entity.QtyOrdSell = 1M;
            entity.QtyShipSell = 1M;
            entity.ReqShipDate = DateTime.Today;

            if (string.IsNullOrEmpty(parent.WhseId))
            {
                string userLocationID = Utility.GetUserLocationID(CompId);
                if (!string.IsNullOrEmpty(userLocationID))
                    entity.LocationId = userLocationID;
                else
                    entity.LocationId = ConfigurationValue.GetRule<string>("SM", "WhseID", this.CompId);
            }
            else
                entity.LocationId = parent.WhseId;

            entity.Unit = ConfigurationValue.GetRule<string>("SM", "Units", this.CompId);
            entity.SetGLAccountDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LineItemUpdateComplete;
            entity.PropertyChanged += LineItem_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void WhsePropertyChanged(TransactionDetailLineItem entity)
        {
            if (string.IsNullOrEmpty(entity.LocationId))
                return;

            entity.ExtLocA = null;
            entity.SetGLAccountDefaults();
            entity.SetDefaultCost();
            entity.SetDefaultBin();
            entity.SetDefaultPrice();
            entity.Calculate();
        }

        protected virtual void QtyPropertyChanged(TransactionDetailLineItem entity)
        {
            if (entity.IsNew && entity.InItem != null)
            {
                if (entity.InItem.InventoryType != InventoryType.Serial || !entity.InItem.IsLotted)
                {
                    entity.QtyShipSell = entity.QtyOrdSell;
                }
            }
            else if (entity.IsNew && entity.InItem == null)
            {
                entity.QtyShipSell = entity.QtyOrdSell;
            }
            ValidateQty(entity);
            entity.SetDefaultCost();
            entity.SetDefaultPrice();
            entity.Calculate();
        }

        protected virtual void QtyShipPropertyChanged(TransactionDetailLineItem entity)
        {
            ValidateQty(entity);
            entity.SetDefaultCost();
            entity.SetDefaultPrice();
            entity.Calculate();
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

            ValidateQty(entity);
            entity.SetDefaultPrice();
            entity.SetDefaultCost();
        }

        protected virtual void ItemIdPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.CustomerPartNumber = null;
            if (Utility.INYN)
            {
                if (entity.InItem == null)
                    return;

                entity.Unit = null;
                entity.LocationId = string.Empty;
                entity.SetDefaults();

                if (entity.InItem.InventoryStatus == InventoryStatus.Obsolete)
                    throw new InvalidValueException(string.Format("Item '{0}' is obsolete and is not valid for this transaction", entity.ItemId));

                if (entity.InItem.AllLocations.Count <= 0)
                    throw new InvalidValueException(string.Format("Item '{0}' has no locations", entity.ItemId));

                if (entity.InItem.InventoryStatus == InventoryStatus.Superseded && !string.IsNullOrWhiteSpace(entity.InItem.SuperId))
                    entity.ItemId = entity.InItem.SuperId;

                if (entity.InItem == null)
                    return;

                if (entity.InItem.IsKit)
                    throw new InvalidValueException(string.Format("Item '{0}' is a kitted item. Kitted items are not valid for this transaction", entity.ItemId));

                this.ValidateQty(entity);

                if (entity.InItem.IsLotted || entity.InItem.InventoryType == InventoryType.Serial)
                {
                    entity.Quantity = 0M;
                    entity.CalculateExtendedPrice();
                }

                if (string.IsNullOrEmpty(entity.CustomerPartNumber))
                {
                    EntityList<Alias> entityList = Alias.FindCustomerItemByAlias(CompId, entity.ItemId, ((TransactionHeader)entity.Parent).CustId);
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

        protected virtual void ValidateQty(TransactionDetailLineItem entity)
        { }

        protected virtual void LineItemUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionDetailLineItem;
            entity.PropertyChanged -= LineItem_PropertyChanged;

            entity.Parent.CalculateTotals();
        }
        #endregion Line Item Update Methods

        #region Lot Update Methods
        protected virtual TransactionLot UpdateLot(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Sequence number is required.");

            this.FilterEntityList(parent.LotList, ApiArTransactionLotController.FunctionId);
            TransactionLot entity = parent.LotList.Find(x => x.SeqNum == (int)bodyItem.SeqNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Lot '{0}' for Line item '{1}' with Transaction ID '{2}' could not be found.", bodyItem.SeqNum, parent.EntryNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionLot CreateLot(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            TransactionLot entity = parent.LotList.AddNew();
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

        protected virtual void LotNumberPropertyChanged(TransactionLot entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || entity.InItem == null
                || !entity.InItem.IsLotted)
                return;

            entity.SetDefaults();

            Lot lot = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && ((TransactionHeader)entity.Parent).TransactionType == ARTransactionType.CreditMemo && lot == null)
                entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);

            ValidateQty(entity);
        }

        protected virtual void LotUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionLot;
            entity.PropertyChanged -= LotEntity_PropertyChanged;
        }

        protected virtual void ValidateQty(TransactionLot entity)
        { }
        #endregion Lot Update Methods

        #region Serial Update Methods
        protected virtual TransactionSerial UpdateSerial(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            this.FilterEntityList(parent.SerialList, ApiArTransactionSerialController.FunctionID);
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
                || entity.InItem == null
                || entity.InItem.InventoryType != InventoryType.Serial)
                return;

            var serial = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .SerialItems.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            if (serial == null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' is not on file.", entity.SerNum));

            if (((TransactionHeader)entity.Parent).TransactionType == ARTransactionType.Invoice)
            {
                if (serial.SerialItemStatus == SerialItemStatus.Available)
                {
                    serial.SerialItemStatus = SerialItemStatus.Sold;
                    entity.SetDefaults();
                }

                return;
            }

            if (serial.SerialItemStatus == SerialItemStatus.Sold)
            {
                serial.SerialItemStatus = SerialItemStatus.Available;
                entity.SetDefaults();
            }
            else
                throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Sold.", entity.SerNum));
        }

        protected virtual void LotNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || entity.InItem == null
                || !entity.InItem.IsLotted)
                return;

            Lot lot = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && ((TransactionHeader)entity.Parent.Parent).TransactionType == ARTransactionType.CreditMemo && lot == null)
                entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
        }

        protected virtual void SerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionSerial;
            entity.PropertyChanged -= SerialEntity_PropertyChanged;
        }
        #endregion Serial Update Methods

        #region Payment Update Methods
        protected virtual TransactionPayment UpdatePayment(TransactionHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PmtNum))
                throw new InvalidValueException("Payment Num is required.");

            this.FilterEntityList(parent.PaymentList, ApiArPaymentsController.FunctionID);
            TransactionPayment entity = parent.PaymentList.Find(x => x.PmtNum == (int)bodyItem.PmtNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Payment Num {0} for Transaction '{1}' could not be found.", bodyItem.PmtNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = PaymentUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionPayment CreatePayment(TransactionHeader parent, dynamic bodyItem)
        {
            TransactionPayment entity = parent.PaymentList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = PaymentUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void PmtMethodIdPropertyChanged(TransactionPayment entity)
        {
            entity.SetPaymentMethodDefaults(entity.PmtMethodId);

            if (entity.PaymentMethod != null
                && entity.PaymentMethod.PaymentType != PaymentType.Cash
                && entity.PaymentMethod.PaymentType != PaymentType.Check
                && entity.PaymentMethod.PaymentType != PaymentType.Other
                && entity.PaymentMethod.PaymentType != PaymentType.WriteOff)
                throw new InvalidValueException(string.Format("The selected payment method '{0}' is not supported via the API", entity.PmtMethodId));
        }

        protected virtual void PaymentUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionPayment;
            entity.PropertyChanged -= PaymentEntity_PropertyChanged;

            entity.Parent.CalculateTotals();
        }
        #endregion Payment Update Methods
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
            var entity = sender as TransactionHeader;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<TransactionHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void LineItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionDetailLineItem> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionDetailLineItem);
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionLot> action = null;
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionLot);
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionSerial> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerial);
        }

        private void PaymentEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionPayment> action = null;
            if (PaymentPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionPayment);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected SortedDictionary<string, Action<TransactionHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionHeader>>();

        protected SortedDictionary<string, Action<TransactionDetailLineItem>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetailLineItem>>();

        protected SortedDictionary<string, Action<TransactionLot>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionLot>>();

        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<TransactionPayment>> PaymentPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionPayment>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "15353E23-A8E4-4938-8850-90146632778B";
        #endregion Fields
    }
}
