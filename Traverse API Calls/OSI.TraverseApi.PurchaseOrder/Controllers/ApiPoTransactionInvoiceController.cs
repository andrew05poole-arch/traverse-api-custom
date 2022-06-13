#region Using Directives
using TRAVERSE.Web.API.PurchaseOrder.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoTransactionInvoiceController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum?}", typeof(TransactionInvoiceTotal))]
        public async Task<IHttpActionResult> Get(string transId = null, string invcNum = null)
        {
            return Ok(await Load(transId, invcNum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum?}", typeof(TransactionInvoiceTotal))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId = null, string invcNum = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, invcNum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum?}", typeof(TransactionInvoiceTotal))]
        public async Task<IHttpActionResult> Post([FromBody] dynamic body, string transId = null, string invcNum = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, invcNum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}", typeof(TransactionInvoiceTotal))]
        public async Task Delete(string transId, string invcNum)
        {
            await this.MarkToDelete(transId, invcNum);
        }
        #endregion Web 

        #region Helper Methods
        protected virtual async Task<EntityList<TransactionInvoiceTotal>> Load(string transId, string invNum)
        {
            if (this.Provider.Items.Count <= 0
                || !this.Provider.Items.Exists(i => StringHelper.AreEqual(i.TransId, transId, false) && StringHelper.AreEqual(i.InvcNum, invNum, false)))
            {
                EntityList<TransactionInvoiceTotal> invoiceList = new EntityList<TransactionInvoiceTotal>();

                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);

                var list = new TransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (list?.Count > 0)
                {
                    invoiceList = list[0]?.GetInvoiceList();

                    if (invoiceList.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(invNum))
                            invoiceList = invoiceList.FindAll(x => StringHelper.AreEqual(x.InvcNum, invNum, false));

                        this.Provider.Items.AddRange(invoiceList);

                        await this.FilterEntityListAsync(this.Provider.Items);
                    }
                }
                else
                    throw new InvalidValueException("Transaction ID does not exist.");
            }
            return this.Provider.Items;
        }

        protected virtual async Task<TransactionInvoiceTotal> Find(string transId, string invNum)
        {
            var list = await Load(transId, invNum);
            return list.Find(x => StringHelper.AreEqual(x.TransId, transId, false) && StringHelper.AreEqual(x.InvcNum, invNum, false));
        }

        protected virtual async Task<List<TransactionInvoiceTotal>> ProcessEditRequest(bool isCreate, dynamic body, string transId, string invNum)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(invNum))
                throw new InvalidValueException("Call is ambiguous. Invoice Number is provided along with more than one record.");

            var entityList = new List<TransactionInvoiceTotal>();
            this.TransId = transId;

            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, invNum);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            
            this.Provider?.Update(this.CompId);
            this.SerialProvider?.Update(this.CompId);

            this.UpdateHeaderType();
            
            return entityList;
        }

        protected virtual async Task<TransactionInvoiceTotal> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, string invNum)
        {
            string code = invNum;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.InvcNum) || string.IsNullOrWhiteSpace(bodyItem.InvcNum))
                bodyItem.InvcNum = code;
            else
                code = bodyItem.InvcNum;

            var entity = await this.Find(transId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new TransactionInvoiceTotal(this.CompId);
                entity.TransactionHeader = this.POHeader;
                entity.TransId = this.TransId;
                entity.SetFiscalPeriodYearFromDate((DateTime)entity.InvcDate);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction ID '{0}' with Invoice Number '{1}' could not be found.", code, invNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, string invNum)
        {
            var entity = await this.Find(transId, invNum);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transaction ID '{0}' with Invoice Number '{1}' could not be found.", transId, invNum));

            (entity.DetailList as EntityList<TransactionInvoice>).RemoveAll();
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailList")
            {
                var invoice = (TransactionInvoiceTotal)args.ParentObject;

                if (invoice.IsNew)
                    return this.CreateInvoiceDetail(invoice, args.ItemModel);
                else
                    return this.UpdateInvoiceDetail(invoice, args.ItemModel);
            }           
            else if (args.PropertyName == "InvoiceReceiptList")
            {
                var transactionInvoice = (TransactionInvoice)args.ParentObject;
                this.CurrentTransactionInvoice = transactionInvoice;

                if (transactionInvoice.IsNew)
                    return this.CreateInvoiceReceipt(transactionInvoice, args.ItemModel);
                else
                    return this.UpdateInvoiceReceipt(transactionInvoice, args.ItemModel);
            }
            else if (args.PropertyName == "SerialList")
            {
                var lotInvoice = (TransactionInvoiceReceipt)args.ParentObject;

                if (this.CurrentTransactionInvoice.TransactionDetail?.INItemInfo == null || this.CurrentTransactionInvoice.TransactionDetail?.INItemInfo?.InventoryType != InventoryType.Serial)
                    throw new InvalidValueException("Current invoice does not support adding serial items.");

                if (lotInvoice.IsNew)
                    return this.CreateSerial(lotInvoice, args.ItemModel);
                else
                    return this.UpdateSerial(lotInvoice, args.ItemModel);
            }

            return null;
        }

        #region Header Update Methods
        protected virtual void InvcDatePropertyChanged(TransactionInvoiceTotal entity)
        {
            entity.SetFiscalPeriodYearFromDate(entity.InvcDate.Value);
            this.ValidatePeriodConversion(entity);
            Invoices.SynchronizeProjectActivity(entity);
        }

        protected virtual void GLPeriodYearPropertyChanged(TransactionInvoiceTotal entity)
        {
            this.ValidatePeriodConversion(entity);
            Invoices.SynchronizeProjectActivity(entity);
        }

        protected virtual void ValidatePeriodConversion(TransactionInvoiceTotal entity)
        {
            short period = (short)((DateTime)entity.InvcDate).Month;
            short year = (short)((DateTime)entity.InvcDate).Year;

            PeriodConversion fiscalPeriod = PeriodConversion.GetFiscalPeriod(period, year);
            if (fiscalPeriod != null && fiscalPeriod.ClosedPO.Value)
            {
                if (fiscalPeriod.ClosedGL.Value)
                    AddWarnings(string.Format("Period {0} is closed for fiscal year {1}.", period, year));
            }
        }

        protected virtual void PrepaidPropertyChanged(TransactionInvoiceTotal entity)
        {
            if (!(entity.CurrPrepaidFgn.GetValueOrDefault() == 0m & entity.CurrPrepaidFgn != null))
            {
                entity.CurrChkFiscalYear = new short?((short)ApplicationContext.SessionDate.Year);
                entity.CurrChkGlPeriod = new short?((short)ApplicationContext.SessionDate.Month);
                entity.SetPaymentDefaults();
            }
            entity.SetPayments(0);
        }

        protected virtual void CurrNetSalesTaxChanging(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            TransactionInvoiceTotal header = e.Entity as TransactionInvoiceTotal;
            e.Handled = true;

            if (header == null || ApiUserSkipped.IsApiUserSkipped(bodyItem.CurrNetSalesTaxFgn))
                return;

            decimal taxValue = 0M;
            decimal.TryParse(bodyItem.CurrNetSalesTaxFgn.ToString(), out taxValue);
            header.CurrTaxAdjAmtFgn = taxValue - header.CurrSalesTaxFgn;
        }

        protected virtual void UpdateHeaderType()
        {
            if ((this.POHeader.TransactionType == POTransactionType.NewOrder || 
            this.POHeader.TransactionType == POTransactionType.GoodsReceived) && 
            this.POHeader.GetInvoiceList().Count > 0)
            {
                this.POHeader.SetDefaultTransType(POTransactionType.InvoiceReceived);
            }
            else if(this.POHeader.TransactionType == POTransactionType.NewReturn && 
            this.POHeader.GetInvoiceList().Count > 0)
            {
                this.POHeader.SetDefaultTransType(POTransactionType.DebitMemo);
            }
            this.TransHeaderProvider?.Items.Add(POHeader);
            this.TransHeaderProvider?.Update(this.CompId);
        }
        #endregion Header Update Methods

        #region Invoice Detail Update Methods
        protected virtual TransactionInvoice UpdateInvoiceDetail(TransactionInvoiceTotal parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry Number is required.");

            this.FilterEntityList(parent.DetailList, ApiPoTransactionDetailController.FunctionID);

            TransactionInvoice entity = (parent.DetailList as EntityList<TransactionInvoice>).Find(x => x.EntryNum == Convert.ToInt32(bodyItem.EntryNum));
            if (entity == null)
                throw new InvalidValueException(string.Format("Entry Number '{0}' for Invoice Number '{1}' could not be found.", bodyItem.EntryNum, parent.InvcNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionInvoice CreateInvoiceDetail(TransactionInvoiceTotal parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry Number is required.");

            if (((parent.DetailList as EntityList<TransactionInvoice>)?.FindAll(x => x.EntryNum == Convert.ToInt32(bodyItem.EntryNum)) as EntityList<TransactionInvoice>)?.Count != 0)
                throw new InvalidValueException(string.Format("Entry Number '{0}' already exists.", bodyItem.EntryNum));

            TransactionInvoice entity = (parent.DetailList as EntityList<TransactionInvoice>).AddNew();
            entity.InvoiceId = Guid.NewGuid();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void EntryNumPropertyChanged(TransactionInvoice entity)
        {
            entity.LotNumber = null;
            entity.SetDefaults();

            Invoices.InvoiceHeaderList = this.Provider.Items;
            decimal? qtyFilled = entity.Qty;
            decimal d = 0m;
            if ((qtyFilled.GetValueOrDefault() == d & qtyFilled != null) && entity.TransactionDetail != null)
            {
                decimal num = entity.TransactionDetail.QtyOrd.Value - Invoices.GetQty(entity.EntryNum);

                if (num >= 0m)
                    entity.Qty = new decimal?(num);
            }
            entity.SetCostDefault();
        }

        protected virtual void ValidateLotExpired(Lot lot, DateTime date)
        {
            if (lot.ExpDate < date)
                AddWarnings(string.Format("Warning: Lot '{0}' is expired.", lot.LotNum));
        }

        protected virtual void LotUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionInvoice;
            entity.PropertyChanged -= LotEntity_PropertyChanged;
        }
        #endregion Invoice Detail Update Methods

        #region Serial Update Methods
        protected virtual TransactionSerialInvoice UpdateSerial(TransactionInvoiceReceipt parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerialNumber))
                throw new InvalidValueException("Serial Number is required.");

            this.FilterEntityList(parent.SerialList, ApiPoTransactionInvoiceSerialController.FunctionID);

            this.SerialInvoice = (parent.SerialList as EntityList<TransactionSerialInvoice>)?.Find(x => StringHelper.AreEqual(x.SerialNumber, bodyItem.SerialNumber, false));
            if (this.SerialInvoice == null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' could not be found.", bodyItem.SerialNumber));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            this.SerialInvoice.PropertyChanged += SerialEntity_PropertyChanged;
            parent.CalculateTotal();
            parent.TransactionInvoice.CalculateQty();

            Request.RegisterForDispose(this.SerialInvoice);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return this.SerialInvoice;
        }

        protected virtual TransactionSerialInvoice CreateSerial(TransactionInvoiceReceipt parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerialNumber))
                throw new InvalidValueException("Serial Number is required.");

            this.SerialInvoice = (parent.SerialList as EntityList<TransactionSerialInvoice>)?.Find(x => StringHelper.AreEqual(x.SerialNumber, bodyItem.SerialNumber, false));
            if (this.SerialInvoice != null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' already exists.",
                    bodyItem.SerialNumber));

            this.SerialInvoice = (parent.SerialList as EntityList<TransactionSerialInvoice>).AddNew();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            this.SerialInvoice.PropertyChanged += SerialEntity_PropertyChanged;
            parent.CalculateTotal();
            parent.TransactionInvoice.CalculateQty();

            Request.RegisterForDispose(this.SerialInvoice);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return this.SerialInvoice;
        }

        protected virtual EntityList<TransactionSerial> GetTransactionSerial()
        {
            EntityList<TransactionSerial> transactionSerial = new EntityList<TransactionSerial>(this.CompId);
            var builder = new SqlFilterBuilder<TransactionSerialBase.Columns>();
            builder.AppendEquals(TransactionSerialBase.Columns.SerNum, this.SerialInvoice.SerialNumber);

            return new TransactionSerialProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
        }
        protected virtual void UpdateSerialTransaction()
        {
            EntityList<TransactionSerial> transactionSerial = GetTransactionSerial();
            transactionSerial[0].InvcUnitCostFgn = this.SerialInvoice.InvoiceUnitCostFgn;
            transactionSerial[0].CalculateInvoiceBaseCost();
            this.SerialInvoice.InvoiceUnitCost = transactionSerial[0].InvcUnitCost ?? 0m;
            this.SerialProvider.Items.Add(transactionSerial[0]);
        }
        protected virtual void ValidateHandledProperty(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionInvoice lotInvoice)
                {
                    if (lotInvoice.TransactionDetail.INItemInfo.InventoryType == InventoryType.Serial)
                        e.Handled = true;
                }
            }
        }

        protected virtual void SerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionSerialInvoice;
            entity.PropertyChanged -= SerialEntity_PropertyChanged;
        }

        protected virtual void SerialNumberChanging(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            TransactionSerialInvoice header = e.Entity as TransactionSerialInvoice;
            e.Handled = true;

            if (header.IsNew)
            {
                this.SerialInvoice.SerialNumber = bodyItem.SerialNumber;
                EntityList<TransactionSerial> transactionSerial = this.GetTransactionSerial();
                this.SerialInvoice.InvoiceUnitCostFgn = (ApiUserSkipped.IsApiUserSkipped(bodyItem.InvoiceUnitCostFgn)) ? transactionSerial[0]?.RcptUnitCost : Convert.ToDecimal(bodyItem.InvoiceUnitCostFgn);
            }
            else
            {
                this.SerialInvoice.InvoiceUnitCostFgn = (ApiUserSkipped.IsApiUserSkipped(bodyItem.InvoiceUnitCostFgn)) ? this.SerialInvoice.InvoiceUnitCostFgn : (decimal)bodyItem.InvoiceUnitCostFgn;
                this.UpdateSerialTransaction();
            }
            
        }
        #endregion Serial Update Methods

        #region Non Serial Update Methods
        protected virtual TransactionInvoiceReceipt UpdateInvoiceReceipt(TransactionInvoice parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ReceiptId))
                throw new InvalidValueException("Receipt ID is required.");

           this.FilterEntityList(parent.InvoiceReceiptList, ApiPoTransactionInvoiceReceiptController.FunctionID);

            TransactionInvoiceReceipt entity = parent.InvoiceReceiptList?.Find(x => StringHelper.AreEqual(x.TransactionLotReceipt.RcptNum.ToString(), bodyItem.ReceiptId, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Receipt ID '{0}' could not be found.", bodyItem.ReceiptId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = NonSerialUpdateComplete;
            entity.PropertyChanged += NonSerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionInvoiceReceipt CreateInvoiceReceipt(TransactionInvoice parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ReceiptId))
                throw new InvalidValueException("Receipt number is required.");

            TransactionInvoiceReceipt entity = parent.InvoiceReceiptList?.Find(x => StringHelper.AreEqual(x.TransactionLotReceipt.RcptNum.ToString(), bodyItem.ReceiptId, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Receipt number '{0}' already exists.", bodyItem.ReceiptId));

            entity = parent.InvoiceReceiptList.AddNew();

            var list = TransactionReceiptProvider.GetEntityList(parent.TransId, parent.CompId);
            var receipt = list.Find(rcpt => 
                    rcpt.ReceiptNum.Equals(bodyItem.ReceiptId as string, StringComparison.OrdinalIgnoreCase) &&
                    rcpt.LotReceiptList != null &&
                    rcpt.LotReceiptList.Exists(n => n.EntryNum == parent.EntryNum));
            if (receipt == null)
                throw new InvalidValueException(string.Format("Receipt number '{0}' cannot be found.", bodyItem.ReceiptId));

            entity.ReceiptId = receipt.LotReceiptList.Find(n => n.EntryNum == parent.EntryNum).ReceiptId;

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.Qty = (ApiUserSkipped.IsApiUserSkipped(bodyItem.Qty)) ? entity.TransactionLotReceipt.QtyFilled : (Convert.ToDecimal(bodyItem.Qty) ?? 0m);
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = NonSerialUpdateComplete;
            entity.PropertyChanged += NonSerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void NonSerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionInvoiceReceipt;
            entity.PropertyChanged -= NonSerialEntity_PropertyChanged;
        }

        protected virtual void QtyPropertyChanged(TransactionInvoiceReceipt entity)
        {
            if (entity.Qty > entity.TransactionLotReceipt.QtyFilled)
            {
                if (POHeader.TransactionType > POTransactionType.Request)
                {
                    throw new InvalidValueException("Invoice Quantity cannot be greater than Receipt Quantity.");
                }
                else
                {
                    throw new InvalidValueException("Debit Memo Quantity cannot be greater than Return Quantity.");
                }
            }
        }
        #endregion Non Serial Update Methods
        #endregion Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //Header Property Changed
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.InvcDate.ToString(), InvcDatePropertyChanged);
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.GLPeriod.ToString(), GLPeriodYearPropertyChanged);
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.FiscalYear.ToString(), GLPeriodYearPropertyChanged);
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrSalesTaxFgn.ToString(), (entity) =>
            {
                entity.CurrTaxAdjAmtFgn = new decimal?(0m);
            });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrBankId.ToString(), (entity) =>
            {
                entity.SetBankDefaults();
                entity.SetPayments(0);
            });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrPmtAmt1Fgn.ToString(), (entity) => { entity.SetPayments(1); });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrPmtAmt2Fgn.ToString(), (entity) => { entity.SetPayments(2); });
            PropertyDictionary.Add("CurrTotalFgn", (entity) => { entity.SetPayments(0); });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrDiscFgn.ToString(), (entity) => { entity.SetPayments(0); });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrPrepaidFgn.ToString(), PrepaidPropertyChanged);
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrTaxClassFreight.ToString(), (entity) => { entity.CalculateTotals(); });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrTaxClassMisc.ToString(), (entity) => { entity.CalculateTotals(); });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrFreightFgn.ToString(), (entity) => { entity.CalculateTotals(); });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrMiscFgn.ToString(), (entity) => { entity.CalculateTotals(); });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrCheckDate.ToString(), (entity) =>
            {
                entity.SetPaymentFiscalPeriodYearFromDate(entity.CurrCheckDate.Value);
                this.ValidatePeriodConversion(entity);
            });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrChkGlPeriod.ToString(), (entity) => { this.ValidatePeriodConversion(entity); });
            PropertyDictionary.Add(TransactionInvoiceTotalBase.Columns.CurrChkFiscalYear.ToString(), (entity) => { this.ValidatePeriodConversion(entity); });           
            EntityPropertyDictionary.Add("CurrNetSalesTaxFgn", CurrNetSalesTaxChanging);
            
            //Invoice Detail Property Changed
            LotPropertyDictionary.Add(TransactionInvoiceBase.Columns.EntryNum.ToString(), EntryNumPropertyChanged);
            LotPropertyDictionary.Add(TransactionInvoiceBase.Columns.Qty.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateExtendedCost();
                entity.SuppressEntityEvents = false;
            });
            LotPropertyDictionary.Add(TransactionLotReceiptBase.Columns.UnitCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateExtendedCost();
                entity.SuppressEntityEvents = false;
            });
            LotPropertyDictionary.Add(TransactionLotReceiptBase.Columns.ExtCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateUnitCost();
                entity.SuppressEntityEvents = false;
            });

            //Invoice Serial Property Changed
            EntityPropertyDictionary.Add(TransactionLotReceiptBase.Columns.UnitCostFgn.ToString(), ValidateHandledProperty);
            EntityPropertyDictionary.Add("SerialNumber", SerialNumberChanging);

            //Invoice Non Serial Property Changed
            NonSerialPropertyDictionary.Add(TransactionInvoiceReceiptBase.Columns.Qty.ToString(), QtyPropertyChanged);            
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.FieldName == TransactionInvoiceReceiptBase.Columns.ReceiptId.ToString())
            {
                args.ActualValue = (args.Entity as TransactionInvoiceReceipt)?.TransactionLotReceipt.RcptNum;
            }

            if (args.FieldName == "InvoiceUnitCostFgn")
            {
                args.ActualValue = (args.Entity as TransactionSerialInvoice)?.InvoiceUnitCostFgn;
            }
        }
        #endregion Overrides

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var entity = sender as TransactionInvoiceTotal;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<TransactionInvoiceTotal> action = null;

            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);

            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionInvoice> action = null;
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionInvoice);
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionSerialInvoice> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerialInvoice);
        }

        private void NonSerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionInvoiceReceipt> action = null;
            if (NonSerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionInvoiceReceipt);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider TransHeaderProvider { get; } = new TransactionHeaderProvider();
        private TransactionInvoiceTotalProvider Provider { get; } = new TransactionInvoiceTotalProvider();
        private TransactionSerialProvider SerialProvider { get; } = new TransactionSerialProvider();
        protected TransactionInvoice CurrentTransactionInvoice { get; set; }
        protected SortedDictionary<string, Action<TransactionInvoiceTotal>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionInvoiceTotal>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        protected SortedDictionary<string, Action<TransactionInvoice>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionInvoice>>();
        protected SortedDictionary<string, Action<TransactionSerialInvoice>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerialInvoice>>();
        protected SortedDictionary<string, Action<TransactionInvoiceReceipt>> NonSerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionInvoiceReceipt>>();
        protected TransactionHeader POHeader
        {
            get
            {
                if (this._header == null)
                    this._header = EntityProvider.GetEntity<TransactionHeader, TransactionHeaderProvider>(new string[] { this.TransId }, this.CompId, null);

                return this._header;
            }
        }

        protected string TransId { get; set; }
        protected TransactionSerialInvoice SerialInvoice { get; set; }
        #endregion Properties

        #region Fields
        public const string FunctionID = "2115B53A-FA2D-4993-BFE1-C8C443F4B790";
        private TransactionHeader _header;
        #endregion Fields
    }
}


