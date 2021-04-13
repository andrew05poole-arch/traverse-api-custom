#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Business.SalesOrder;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMpOrderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "order/{id?}", typeof(Order))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "order/{id?}", typeof(Order))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{id?}", typeof(Order))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{id}", typeof(Order))]
        public async Task Delete(string id = null)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region overrides
        protected override void AddPropertyDelegates()
        {
            //Order
            PropertyDictionary.Add(OrderBase.Columns.AssemblyId.ToString(), AssemblyIdPropertyChanged);

            //Order Releases
            ReleasePropertyDictionary.Add(OrderReleasesBase.Columns.EstCompletionDate.ToString(), CompletionDatePropertyChanged);
            ReleasePropertyDictionary.Add(OrderReleasesBase.Columns.EstStartDate.ToString(), EstStartDatePropertyChanged);
            ReleasePropertyDictionary.Add(OrderReleasesBase.Columns.CustId.ToString(), CustIdPropertyChanged);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.EstStartDate.ToString(), SetDetailControls);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.Qty.ToString(), SetDetailControls);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.UOM.ToString(), SetDetailControls);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.Routing.ToString(), SetDetailControls);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.EstCompletionDate.ToString(), SetDetailControls);
  
        }
        #endregion overrides

        #region Helper Methods

        protected virtual async Task<EntityList<Order>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => id == i.OrderNo)))
            {
                if (id == null)
                    await Provider.Load<Order>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<OrderBase.Columns>();
                    builder.AppendEquals(OrderBase.Columns.OrderNo, id);
                    var list = new OrderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Order> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => x.OrderNo == id);
        }

        protected virtual async Task<List<Order>> ProcessEditRequest(bool isCreate, dynamic body, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Order ID is provided along with more than one record.");

            var entityList = new List<Order>();
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


        protected virtual async Task<Order> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.OrderNo) || bodyItem.OrderNo == null)
                bodyItem.OrderNo = code;
            else
                code = bodyItem.OrderNo;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Order(this.CompId);
                entity.SetDefaults();
      
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Order ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (string.IsNullOrWhiteSpace(entity.OrderNo))
                entity.OrderNo = Provider.GetNextOrderNo();

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Order ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailList")
            {
                if (((Order)args.ParentObject).IsNew)
                    return this.CreateOrderRelease((Order)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateOrderRelease((Order)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        protected virtual void AssemblyIdPropertyChanged(Order entity)
        {
            if (!string.IsNullOrEmpty(entity.AssemblyId))
            {
                entity.SetItemDefauls();
                if (string.IsNullOrEmpty(entity.LocId) && entity.InItem != null)
                    entity.LocId = TRAVERSE.Business.Manufacturing.Utility.GetUserLocationID(this.CompId) ?? ConfigurationValueProvider.GetRule<string>("SM", "WhseID", this.CompId);
            }
        }

        #region Order Release
        protected virtual OrderReleases UpdateOrderRelease(Order parent, dynamic bodyItem)
        {

            this.FilterEntityList(parent.DetailList);
            OrderReleases entity = ((EntityList<OrderReleases>)parent.DetailList).Find(x => x.ReleaseNo == (int)bodyItem.ReleaseNo);
            if (entity == null)
                throw new InvalidValueException(string.Format("Release Number '{0}' for Order '{1}' could not be found.", bodyItem.ReleaseNo, parent.OrderNo));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ReleaseUpdateComplete;
            entity.PropertyChanged += OrderRelease_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual OrderReleases CreateOrderRelease(Order parent, dynamic bodyItem)
        {
            OrderReleases entity = (parent.DetailList as EntityList<OrderReleases>).AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ReleaseUpdateComplete;
            entity.PropertyChanged += OrderRelease_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void ReleaseUpdateComplete(object entityObject)
        {
            var entity = entityObject as OrderReleases;
            entity.PropertyChanged -= OrderRelease_PropertyChanged;

            entity.SetDefaults();
        }

        protected virtual void SetEstStartDate(OrderReleases entity)
        {
            entity.SetDefaults();

            if (entity != null && entity.EstCompletionDate.HasValue && !entity.EstStartDate.HasValue && entity != null && this.Order.InItem != null && this.Order.InItem.CurrentLocation != null)
            {
                double num = (double)this.Order.InItem.CurrentLocation.DfltLeadTime.Value;
                if (num != 0.0)
                {
                    entity.EstStartDate = new DateTime?(entity.EstCompletionDate.Value.AddDays(-num));
                }
            }
        }

        protected virtual void LoadSalesOrder(string custId)
        {
            SqlFilterBuilder<OrderReleasesBase.Columns> sqlFilterBuilder = new SqlFilterBuilder<OrderReleasesBase.Columns>();
            sqlFilterBuilder.AppendEquals(OrderReleasesBase.Columns.CustId, custId);
        }

        protected virtual void LoadPurchaseOrder(string custId, string salesOrder)
        {
            SqlFilterBuilder<TransactionHeaderBase.Columns> sqlFilterBuilder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
            if (salesOrder != null)
            {
                sqlFilterBuilder.AppendEquals(TransactionHeaderBase.Columns.CustId, custId);
                sqlFilterBuilder.AppendEquals(TransactionHeaderBase.Columns.TransId, salesOrder);
            }
            else
            {
                sqlFilterBuilder.AppendEquals(TransactionHeaderBase.Columns.CustId, custId);
            }
        }

        protected virtual void SalesOrderPropertyChanged(OrderReleases entity)
                {
                    this.LoadPurchaseOrder(entity.CustId, entity.SalesOrder);
                    return;
                }

        protected virtual void SetDetailControls(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is OrderReleases releases)
                {
                    if (releases.Status == 0 || releases.Status == 1 || releases.Status == 2 || releases.Status == 3)
                        e.Handled = false;

                    if (releases.Status == 4 || releases.Status == 5 || releases.Status == 6)
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        protected virtual void CompletionDatePropertyChanged(OrderReleases entity)
        {
            this.SetEstStartDate(entity);
            entity.Validate(OrderReleasesBase.Columns.EstStartDate.ToString());
        }

        protected virtual void EstStartDatePropertyChanged(OrderReleases entity)
        {
            entity.Validate(OrderReleasesBase.Columns.EstCompletionDate.ToString());
        }

        protected virtual void CustIdPropertyChanged(OrderReleases entity)
        {
            if (!string.IsNullOrEmpty(entity.CustId))
            {
                this.LoadSalesOrder(entity.CustId);
                this.LoadPurchaseOrder(entity.CustId, entity.SalesOrder);
                return;
            }
        }
        #endregion OrderRelease

        #endregion Helper Methods

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out Action<dynamic, ApiEntityPropertyChangingArgs> action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var entity = sender as Order;
            entity.PropertyChanged -= Entity_PropertyChanged;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out Action<Order> action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void OrderRelease_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ReleasePropertyDictionary.TryGetValue(e.PropertyName, out Action<OrderReleases> action))
                action.Invoke(sender as OrderReleases);
        }
        #endregion Event Handlers

        #region Properties
        private OrderProvider Provider { get; } = new OrderProvider();

        protected Dictionary<Order, Action<Order>> ProcessList { get; } = new Dictionary<Order, Action<Order>>();

        protected SortedDictionary<string, Action<Order>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Order>>();

        protected SortedDictionary<string, Action<OrderReleases>> ReleasePropertyDictionary { get; } = new SortedDictionary<string, Action<OrderReleases>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private Order Order { get; set; }
        #endregion Properties

        #region Fields
        public const string FunctionID = "421B44B2-CBED-4CEE-A368-ADC3F9F3F4FC";
        #endregion Fields
    }
}
