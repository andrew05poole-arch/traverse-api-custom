#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoTransactionHeaderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id}", typeof(TransactionHeader))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates()
        {
            //Header Property Changes
            PropertyDictionary.Add(TransactionHeaderBase.Columns.VendorId.ToString(), VendorIdPropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoPrepaidFgn.ToString(), (entity) => entity.SetPayments(0));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoPmtAmt1Fgn.ToString(), (entity) => entity.SetPayments(1));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoPmtAmt2Fgn.ToString(), (entity) => entity.SetPayments(2));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoDiscFgn.ToString(), (entity) => entity.SetPayments(0));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoSalesTaxFgn.ToString(), (entity) => entity.SetPayments(0));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.ShipToId.ToString(), ShipToIdPropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.CurrencyId.ToString(), ExchangeRatePropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TermsCode.ToString(), TermsCodePropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoTaxClassFreight.ToString(), (entity) => entity.CalculateTotals(true));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoTaxClassMisc.ToString(), (entity) => entity.CalculateTotals(true));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoFreightFgn.ToString(), (entity) => entity.CalculateTotals(true));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.MemoMiscFgn.ToString(), (entity) => entity.CalculateTotals(true));

            //Detail Property Changes
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.UnitCostFgn.ToString(), (entity) => entity.CalculateExtendedCost());
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.ExtCostFgn.ToString(), (entity) => entity.CalculateUnitCost());
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.ItemId.ToString(), ItemIdPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.LandedCostId.ToString(), (entity) => entity.SetLandedCostDetail());
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.TaxClass.ToString(), TaxClassPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.QtyOrd.ToString(), QtyOrdPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.Units.ToString(), UnitsPropertyChanged);
            EntityPropertyDictionary.Add(TransactionDetailBase.Columns.ExtLocA.ToString(), BinPropertyChanged);

        }
        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is TransactionDetail transactionDetail)
            {
                if (StringHelper.AreEqual(args.FieldName, TransactionDetailBase.Columns.ExtLocA.ToString(), false))
                {
                    args.ActualValue = (EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, null, null)?.Find(x => StringHelper.AreEqual(x.Id.ToString(), args.ActualValue?.ToString(), false)) as ExtLocationBin)?.ExtLocId;
                }
            }
        }
        #endregion Override
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
                    var list = new TransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
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
            if (args.PropertyName == "DetailList")
            {
                if (((TransactionHeader)args.ParentObject).IsNew)
                    return this.CreateDetail((TransactionHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDetail((TransactionHeader)args.ParentObject, args.ItemModel);
            }
            return null;
        }
        #region Header Update Methods
        protected virtual void VendorIdPropertyChanged(TransactionHeader entity)
        {
            entity.SetVendorDefaults();
            TransactionHeader.RecalculateLineItems(entity);
            entity.CalculateTotals(true);
        }

        protected virtual void ExchangeRatePropertyChanged(TransactionHeader entity)
        {
            TransactionHeader.RecalculateLineItems(entity);
            entity.CalculateTotals(true);
        }

        protected virtual void ShipToIdPropertyChanged(TransactionHeader entity)
        {
            if (!string.IsNullOrEmpty(entity.ShipToId))
            {
                entity.SetShipToDefaults();
                ShipTo shipTo = this.ShipToList.Find(ShipToBase.Columns.ShiptoId, entity.ShipToId);
                if (shipTo != null)
                {
                    if (!string.IsNullOrEmpty(shipTo.DistCode) && string.Compare(shipTo.DistCode, entity.DistCode, true) != 0)
                    {
                        entity.DistCode = shipTo.DistCode;
                    }
                    if (!string.IsNullOrEmpty(shipTo.TaxLocId) && string.Compare(shipTo.TaxLocId, entity.TaxGrpId, true) != 0 && !this.IsInvoicePosted(entity))
                    {
                        entity.TaxGrpId = shipTo.TaxLocId;
                        this.ProcessTaxGrpIdChange(entity);
                        return;
                    }
                }
            }
            else
            {
                entity.ShipToName = null;
                entity.ShipToAddr1 = null;
                entity.ShipToAddr2 = null;
                entity.ShipToCity = null;
                entity.ShipToRegion = null;
                entity.ShipToCountry = null;
                entity.ShipVia = null;
                entity.ShipToAttn = null;
                entity.ShipToPostalCode = null;
            }
        }

        protected virtual bool IsInvoicePosted(TransactionHeader entity)
        {
            bool result = false;
            if (entity != null && !entity.IsNew)
            {
                using (IEnumerator<TransactionInvoiceTotal> enumerator = entity.GetInvoiceList().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if ((enumerator.Current.DetailList as EntityList<TransactionInvoice>).Find(TransactionInvoiceBase.Columns.Status, 1) != null)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            return result;
        }

        protected virtual void ProcessTaxGrpIdChange(TransactionHeader entity)
        {
            if (entity != null)
            {
                if (entity.GetInvoiceList().Count > 0)
                {
                    entity.RecalculateInvoiceTotal = true;
                }
                entity.CalculateTotals(true);
            }
        }

        protected virtual void TermsCodePropertyChanged(TransactionHeader entity)
        {
            if (entity.GetInvoiceList().Count > 0)
            {
                entity.TermsCodeChanged = true;
            }
            entity.SetFirstNetDueDate();
            entity.CalculateTotals(true);
        }
        #endregion Header Update Methods

        #region Detail Update Methods
        protected virtual TransactionDetail UpdateDetail(TransactionHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry number is required.");

            this.FilterEntityList(parent.DetailList, ApiPoTransactionDetailController.FunctionID);
            TransactionDetail entity = (parent.DetailList as EntityList<TransactionDetail>).Find(TransactionDetailBase.Columns.EntryNum, (int)bodyItem.EntryNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Line item '{0}' with Transaction ID '{1}' could not be found.", bodyItem.EntryNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;


            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionDetail CreateDetail(TransactionHeader parent, dynamic bodyItem)
        {
            TransactionDetail entity = (parent.DetailList as EntityList<TransactionDetail>).AddNew();

            entity.Quantity = 1M;
            entity.SetDefaults();
            entity.SetItemDefault();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionDetail;
            entity.PropertyChanged -= Detail_PropertyChanged;

            var header = entity.Parent as TransactionHeader;
            TransactionHeader.RecalculateLineItems(header);
            header.CalculateTotals();
            header.SetPayments(0);
        }
        protected virtual void ItemIdPropertyChanged(TransactionDetail entity)
        {
            entity.SetItemDefault();
            entity.SetCostDefault();
            entity.SetGLAccountAccrualDefault();
        }

        protected virtual void TaxClassPropertyChanged(TransactionDetail entity)
        {
            entity.CalculateExtendedCost();

            if (entity.Parent is TransactionHeader transactionHeader && transactionHeader.GetInvoiceList().Count > 0)
            {
                transactionHeader.RecalculateInvoiceTotal = true;
            }
        }

        protected virtual void QtyOrdPropertyChanged(TransactionDetail entity)
        {
            if (!string.IsNullOrEmpty(entity.LocId) && entity.IsNew)
            {
                entity.SetCostDefault();
            }
            entity.CalculateExtendedCost();
        }

        protected virtual void UnitsPropertyChanged(TransactionDetail entity)
        {
            if (!string.IsNullOrEmpty(entity.LocId))
            {
                entity.SetCostDefault();
            }
        }

        protected virtual void BinPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is TransactionDetail transactionExt)
            {
                args.ActualValue = (EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, null, null)?.Find(x => StringHelper.AreEqual(x.ExtLocId, bodyItem.bin, false) && x.LocId == bodyItem.LocId) as ExtLocationBin).Id;
            }
        }
        #endregion Detail Update Methods
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

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionDetail> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected SortedDictionary<string, Action<TransactionHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionHeader>>();

        protected SortedDictionary<string, Action<TransactionDetail>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetail>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private EntityList<ShipTo> ShipToList
        {
            get
            {
                return ListFactory.CreateList<ShipTo, ShipToProvider>(this.CompId);
            }
        }
        #endregion Properties

        #region Fields
        public const string FunctionID = "FEBD955F-42A3-4F95-8290-78AF5F534043";
        #endregion Fields
    }
}
