#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;							 
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.SalesOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.SalesOrder.Controllers
{
    public class ApiSoTransactionOrderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiSoTransactionSerialController.FunctionID, typeof(TransactionSerial), ApiSoTransactionLotController.FunctionId, typeof(TransactionDetailExt) })]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id, false));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiSoTransactionSerialController.FunctionID, typeof(TransactionSerial), ApiSoTransactionLotController.FunctionId, typeof(TransactionDetailExt) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiSoTransactionSerialController.FunctionID, typeof(TransactionSerial), ApiSoTransactionLotController.FunctionId, typeof(TransactionDetailExt) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id}", typeof(TransactionHeader), new object[] { ApiSoTransactionSerialController.FunctionID, typeof(TransactionSerial), ApiSoTransactionLotController.FunctionId, typeof(TransactionDetailExt) })]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Header Property Changes
            PropertyDictionary.Add(TransactionHeaderBase.Columns.SoldToId.ToString(), SoldToIdPropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.ShipToId.ToString(), ShipToIdPropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxableYN.ToString(), (entity) => RecalculateOrder(entity, entity.TaxableYN.GetValueOrDefault()));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.CustId.ToString(), CustIdPropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.Rep1Id.ToString(), (entity) => entity.SetSalesRepDefaults(1));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.Rep2Id.ToString(), (entity) => entity.SetSalesRepDefaults(2));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxAmtAdjFgn.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.FreightFgn.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MiscFgn.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxClassMisc.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxClassFreight.ToString(), (entity) => entity.CalculateTaxes());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.OrgInvcNum.ToString(), (entity) => entity.SetOrginalInvoiceId());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.DistCode.ToString(), (entity) => TransactionHeader.SynchronizePayments(entity));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.InvcNum.ToString(), (entity) => TransactionHeader.SynchronizePayments(entity));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.InvcDate.ToString(), InvcDatePropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxableSalesFgn.ToString(), (entity) => entity.ResetTaxAdjustment());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.NonTaxableSalesFgn.ToString(), (entity) => entity.ResetTaxAdjustment());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TaxGrpId.ToString(), (entity) => RecalculateOrder(entity, true));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.CurrencyId.ToString(), (entity) => entity.UpdateExchangeRate());
            EntityPropertyDictionary.Add("NetSalesTaxFgn", NetSalesTaxChanging);
            EntityPropertyDictionary.Add(TransactionHeaderBase.Columns.TransType.ToString(), TransTypePropertyChanging);

            //Line Item Property Changes
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.ItemId.ToString(), ItemIdPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.LocId.ToString(), WhsePropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.TotQtyOrdSell.ToString(), QtyPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.PriceAdjType.ToString(), (entity) => entity.ResetPriceAdjustment());
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.PriceAdjPct.ToString(), (entity) => entity.CalculatePriceAdjustment());
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.PriceId.ToString(), (entity) => GetUnitPrice(entity, true));
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.UnitPriceSellBasisFgn.ToString(), UnitPriceSellPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.Rep1Id.ToString(), (entity) => entity.SetSalesRepDefaults(1));
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.Rep2Id.ToString(), (entity) => entity.SetSalesRepDefaults(2));
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.CatId.ToString(), (entity) =>
            {
                entity.SetSalesRepDefaults(1);
                entity.SetSalesRepDefaults(2);
            });
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.UnitsSell.ToString(), UnitsSellPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.TaxClass.ToString(), (entity) => ((TransactionHeader)entity.Parent).ResetTaxAdjustment());
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.AcctCode.ToString(), (entity) => { if (!string.IsNullOrWhiteSpace(entity.AcctCode)) entity.SetGLAccountDefaults(entity.AcctCode); });
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.UnitCostSellFgn.ToString(), UnitCostPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.QtyShipSell.ToString(), QtyShipPropertyChanged);

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransactionDetailExtBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            LotPropertyDictionary.Add(TransactionDetailExtBase.Columns.CostUnitFgn.ToString(),
                (entity) => entity.UpdateCosts(entity.LineItem.Parent.TransType < 0));

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(),
                (entity) => entity.UpdateCosts(entity.LineItem.Parent.TransType < 0));

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
            return await this.Load(id, false);
        }
        protected virtual async Task<EntityList<TransactionHeader>> Load(string id, bool isCreate)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.TransId, false))))
            {
                if (string.IsNullOrEmpty(id))
                {
                    if (!isCreate)
                    {
                        await Provider.Load<TransactionHeader>(this.CompId, PageNumber, PageSize);
                    }
                }
                else
                {
                    var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                    builder.AppendEquals(TransactionHeaderBase.Columns.TransId, id);
                    var list = new TransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<TransactionHeader> Find(string id)
        {
            return await this.Find(id, false);
        }
        protected virtual async Task<TransactionHeader> Find(string id, bool isCreate)
        {
            var list = await Load(id, isCreate);
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

            foreach (var pair in ProcessList)
                pair.Value.Invoke(pair.Key);

            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || string.IsNullOrWhiteSpace(bodyItem.TransId))
                bodyItem.TransId = code;
            else
                code = bodyItem.TransId;

            var entity = await this.Find(code, isCreate);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new TransactionHeader(this.CompId);
                var date = DateTime.Today;
                entity.TransDate = date;
                entity.PODate = date;
                entity.ReqShipDate = date;
                entity.CurrencyId = Utility.BaseCurrencyId;
                entity.ShipToCountry = Utility.DefaultCountry;
                entity.LocId = entity.DefaultLocationId;
                entity.BatchId = entity.DefaultBatchId;
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", code));

            if (this.IsBodyItemExist(bodyItem, "transaction_type"))
            {
                SOTransactionType transactionType = this.GetTransactionType(bodyItem.TransType);
                if (isCreate)
                {
                    entity.TransactionType = transactionType;
                }
                else
                {
                    entity.ChangeTransactionType(transactionType);
                }
                entity.CalculateTotals();
            }

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (string.IsNullOrWhiteSpace(entity.TransId))
            {
                this.Provider.CompId = this.CompId;
                entity.TransId = Provider.GetNextTransId();
            }

            entity.LineItemList.ForEach(d =>
            {
                if (d.IsNew)
                    AppendKitComponents(d);
            });

            return entity;
        }

        protected virtual bool IsBodyItemExist(dynamic item, string property)
        {
            return ((IDictionary<string, object>)item).ContainsKey(property);
        }

        protected virtual SOTransactionType GetTransactionType(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = value.All(char.IsDigit) ? value : value.ToLower();
                switch (value)
                {
                    case "9":
                    case "new":
                        return SOTransactionType.New;
                    case "1":
                    case "invoice":
                        return SOTransactionType.Invoice;
                    case "2":
                    case "price quote":
                        return SOTransactionType.PriceQuote;
                    case "-1":
                    case "credit memo":
                        return SOTransactionType.CreditMemo;
                    case "-2":
                    case "rma":
                        return SOTransactionType.RMA;
                    case "3":
                    case "backordered":
                        return SOTransactionType.Backordered;
                    case "4":
                    case "verified":
                        return SOTransactionType.Verified;
                    case "5":
                    case "picked":
                        return SOTransactionType.Picked;
                }
            }
            return SOTransactionType.New;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id, false);

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
                if (args.PropertyName == "ExtendedList")
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

        #region Header Update Methods
        protected virtual void SoldToIdPropertyChanged(TransactionHeader entity)
        {
            entity.ShipToId = null;
            if (entity.IsNew)
            {
                entity.SetDefaults();
                RecalculateOrder(entity, false);
                entity.UpdateExchangeRate();
            }
            entity.SetOrginalInvoiceId();
        }

        protected virtual void CustIdPropertyChanged(TransactionHeader entity)
        {
            entity.SetBillToCustomerDefaults();
            RecalculateOrder(entity, false);
        }

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

        protected virtual void InvcDatePropertyChanged(TransactionHeader entity)
        {
            entity.SetFiscalPeriodYearFromDate(entity.InvcDate.GetValueOrDefault(DateTime.Today));
            if (entity.IsNew)
            {
                entity.SetExchangeRateBase();
                entity.UpdateExchangeRate();
            }
        }

        protected virtual void TransTypePropertyChanging(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            TransactionHeader header = e.Entity as TransactionHeader;
            if (header == null)
                return;

            SOTransactionType newTransactionType = (SOTransactionType)e.ActualValue;

            if (newTransactionType == SOTransactionType.Verified)
            {
                if (Utility.WMSIsValidFeature(CompId) && Utility.GetWMYN(CompId) && (header.TransactionType == SOTransactionType.New || header.TransactionType == SOTransactionType.Backordered))
                {
                    if (header.LineItemList.Exists(l => !((l.IsTransactionLinked && l.IsDropShip) || l.InItem == null || !l.InItem.IsQuantityTracked)))
                        return;

                    if (header.KitComponentList != null && !header.KitComponentList.Exists(l => !((l.IsTransactionLinked && l.IsDropShip) || l.InItem == null || !l.InItem.IsQuantityTracked)))
                        return;
                }
            }

            header.ChangeTransactionType(newTransactionType);

            if (header.TransactionType == SOTransactionType.Verified)
            {
                header.InvcDate = DateTime.Today;
                header.SetFiscalPeriodYearFromDate(DateTime.Today);
                header.LineItemList.ForEach(l =>
                {
                    l.CalculateBackorderedQty();
                    if (l.Kit.GetValueOrDefault())
                        l.KitComponentList.ForEach(k => k.CalculateBackorderedQty());
                });
            }
            e.Handled = true;
        }

        protected virtual void VerifyOrder(TransactionHeader header)
        {
            if (Utility.WMSIsValidFeature(this.CompId) && Utility.GetWMYN(this.CompId))
            {
                if (header.LineItemList.Exists(l =>
                    (!l.IsTransactionLinked || !l.IsDropShip) && l.InItem != null && l.InItem.IsQuantityTracked) ||
                    header.KitComponentList.Exists(l =>
                    (!l.IsTransactionLinked || !l.IsDropShip) && l.InItem != null && l.InItem.IsQuantityTracked))
                {
                    return;
                }
            }

            header.ChangeTransactionType(SOTransactionType.Verified);
            header.InvcDate = DateTime.Today;
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

            this.FilterEntityList(parent.DetailList, ApiSoTransactionDetailController.FunctionID);
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
            TransactionDetailLineItem entity = parent.LineItemList.AddNew();

            entity.TotQtyOrdSell = 1M;
            if (parent.TransactionType == SOTransactionType.Invoice || parent.TransactionType == SOTransactionType.CreditMemo)
                entity.ShipNeededQuantity();
            else if (parent.TransactionType == SOTransactionType.Verified)
                entity.CalculateBackorderedQty();

            if (string.IsNullOrEmpty(parent.LocId))
            {
                string userLocationID = Utility.GetUserLocationID(CompId);
                if (!string.IsNullOrEmpty(userLocationID))
                    entity.LocationId = userLocationID;
                else
                    entity.LocationId = ConfigurationValue.GetRule<string>("SM", "WhseID", this.CompId);
            }
            else
                entity.LocationId = parent.LocId;

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

            entity.SetLocationDefaults();
            entity.SetDefaultCost();
            entity.SetDefaultPrice();
            entity.SetGLAccountDefaults(entity.AcctCode);
        }

        protected virtual void QtyPropertyChanged(TransactionDetailLineItem entity)
        {
            if (Utility.INYN && entity.InItem != null)
            {
                if (((TransactionHeader)entity.Parent).TransactionType == SOTransactionType.CreditMemo || ((TransactionHeader)entity.Parent).TransactionType == SOTransactionType.Invoice)
                {
                    if (entity.IsTransactionLinked)
                        entity.CalculateBackorderedQty();
                    else
                        entity.ShipNeededQuantity();
                }
                else if (((TransactionHeader)entity.Parent).TransactionType == SOTransactionType.Verified)
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
            entity.CalculateExtendedPrice();
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

                if (Utility.GetDefMinSalesQuantityYn(this.CompId) && ((TransactionHeader)entity.Parent).IsSale && entity.InItemSellingUnit != null &&
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

        protected virtual void AppendKitComponents(TransactionDetailLineItem entity)
        {
            if (entity.InItem == null || !entity.Kit.GetValueOrDefault() || entity.KitComponentList.Count != 0)
                return;

            var list = KitDefinition.GetKitDefinitions(entity.CompId, entity.ItemId, entity.LocationId, entity.TransMan);
            var definition = list.Find("Source", "BM");
            if (definition != null)
            {
                KitDefinition.AppendKitComponents(entity, definition.Components);
            }
        }

        protected virtual void LineItemUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionDetailLineItem;
            entity.PropertyChanged -= LineItem_PropertyChanged;
        }
        #endregion Line Item Update Methods

        #region Lot Update Methods
        protected virtual TransactionDetailExt UpdateLot(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Sequence number is required.");

            this.FilterEntityList(parent.ExtendedList, ApiSoTransactionLotController.FunctionId);
            TransactionDetailExt entity = ((EntityList<TransactionDetailExt>)parent.ExtendedList).Find(x => x.SeqNum == (int)bodyItem.SeqNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Lot '{0}' for Line item '{1}' with Transaction ID '{2}' could not be found.", bodyItem.SeqNum, parent.EntryNum, parent.TransId));

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


            Lot lot = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

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

            var serial = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .SerialItems.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

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


            Lot lot = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

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

        #region Payment Update Methods
        protected virtual TransactionPayment UpdatePayment(TransactionHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PmtNum))
                throw new InvalidValueException("Payment Num is required.");

            this.FilterEntityList(parent.PaymentList, ApiSoTransactionPaymentController.FunctionID);
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
                && entity.PaymentMethod.PaymentType != TRAVERSE.Business.AccountsReceivable.PaymentType.Cash
                && entity.PaymentMethod.PaymentType != TRAVERSE.Business.AccountsReceivable.PaymentType.Check
                && entity.PaymentMethod.PaymentType != TRAVERSE.Business.AccountsReceivable.PaymentType.Other
                && entity.PaymentMethod.PaymentType != TRAVERSE.Business.AccountsReceivable.PaymentType.WriteOff)
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
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out Action<dynamic, ApiEntityPropertyChangingArgs> action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var entity = sender as TransactionHeader;
            entity.PropertyChanged -= Entity_PropertyChanged;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionHeader> action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void LineItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionDetailLineItem> action))
                action.Invoke(sender as TransactionDetailLineItem);
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionDetailExt> action))
                action.Invoke(sender as TransactionDetailExt);
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionSerial> action))
                action.Invoke(sender as TransactionSerial);
        }

        private void PaymentEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PaymentPropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionPayment> action))
                action.Invoke(sender as TransactionPayment);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected Dictionary<TransactionHeader, Action<TransactionHeader>> ProcessList { get; } = new Dictionary<TransactionHeader, Action<TransactionHeader>>();

        protected SortedDictionary<string, Action<TransactionHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionHeader>>();

        protected SortedDictionary<string, Action<TransactionDetailLineItem>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetailLineItem>>();

        protected SortedDictionary<string, Action<TransactionDetailExt>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetailExt>>();

        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<TransactionPayment>> PaymentPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionPayment>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "765AE38B-AF2C-4529-B445-A914CA369A4B";
        #endregion Fields
    }
}
