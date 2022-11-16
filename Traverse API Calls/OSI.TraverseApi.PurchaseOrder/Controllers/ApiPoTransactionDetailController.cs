#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoTransactionDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetail))]
        public async Task<IHttpActionResult> Get(string transId = null, int? id = null)
        {
            return Ok(await Load(transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int}", typeof(TransactionDetail))]
        public async Task Delete(string transId, int id)
        {
            await this.MarkToDelete(transId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates() 
        {
            //Entity Property Changes
            EntityPropertyDictionary.Add(TransactionDetailBase.Columns.EntryNum.ToString(), ProcessEntryNum);

            //Detail Property Changes
            PropertyDictionary.Add(TransactionDetailBase.Columns.UnitCostFgn.ToString(), (entity) => entity.CalculateExtendedCost());
            PropertyDictionary.Add(TransactionDetailBase.Columns.ExtCostFgn.ToString(), (entity) => entity.CalculateUnitCost());
            PropertyDictionary.Add(TransactionDetailBase.Columns.ItemId.ToString(), ItemIdPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.LandedCostId.ToString(), (entity) => entity.SetLandedCostDetail());
            PropertyDictionary.Add(TransactionDetailBase.Columns.TaxClass.ToString(), TaxClassPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.QtyOrd.ToString(), QtyOrdPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.Units.ToString(), UnitsPropertyChanged);
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
        protected virtual async Task<EntityList<TransactionDetail>> Load(string transId, int? id)
        {
            var list = CurrentTransaction?.DetailList as EntityList<TransactionDetail>;

            if (CurrentTransaction == null || !StringHelper.AreEqual(CurrentTransaction.TransId, transId, false))
            {
                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPoTransactionHeaderController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", transId));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.DetailList as EntityList<TransactionDetail>;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(TransactionDetailBase.Columns.EntryNum, id.Value);

            return list;
        }

        protected virtual async Task<TransactionDetail> Find(string transId, int id)
        {
            var list = await Load(transId, id);
            return list.Find(x => x.EntryNum == id);
        }

        protected virtual async Task<List<TransactionDetail>> ProcessEditRequest(bool isCreate, dynamic body, string transId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Line Item is provided along with more than one record.");

            var entityList = new List<TransactionDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            RecalculateTotals();
            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, int? id)
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

                entity = CurrentTransaction.DetailList.AddNew() as TransactionDetail;
                entity.Quantity = 1m;
                entity.SetDefaults();
                entity.SetItemDefault();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Line Item {0} not be found on transaction '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
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
            RecalculateTotals();
            this.Provider.Update(this.CompId);
        }
       
        #region Detail Update Methods
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

        #region Entity Methods       
        protected virtual void ProcessEntryNum(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (((TransactionDetail)e.Entity).EntryNum != 0)
                    e.Handled = true;
            }
        }
        protected virtual void RecalculateTotals()
        {
            CurrentTransaction.CalculateTotals();

            if (ConfigurationValueProvider.GetRule<bool>(AppId.AP, ConfigurationValue.DiscAutoYn, this.CompId))
                CurrentTransaction.MemoDiscFgn = CurrentTransaction.GetDiscountAmount();

            CurrentTransaction.SetPayments(0);
        }
        #endregion Entity Methods
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
            Action<TransactionDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionDetail);
        }        
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected TransactionHeader CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransactionDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetail>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "709529b9-0e66-4822-a647-7cfabf388a31";
        #endregion Fields
    }
}
