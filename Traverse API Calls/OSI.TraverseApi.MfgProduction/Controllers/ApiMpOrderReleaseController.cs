#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class ApiMpOrderReleaseController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int?}", typeof(OrderReleases))]
        public async Task<IHttpActionResult> Get(string orderNo, int? releaseNo = null)
        {
            return Ok(await this.Load(orderNo, releaseNo));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int?}", typeof(OrderReleases))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string orderNo, int? releaseNo = null)
        {
            return Ok(await ProcessEditRequest(false, body, orderNo, releaseNo));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int?}", typeof(OrderReleases))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo, int? releaseNo = null)
        {
            return Ok(await ProcessEditRequest(true, body, orderNo, releaseNo));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{releaseno:int}", typeof(OrderReleases))]
        public async Task Delete(string orderNo, int releaseNo)
        {
            await this.MarkToDelete(orderNo, releaseNo);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            PropertyDictionary.Add(OrderReleasesBase.Columns.EstCompletionDate.ToString(), CompletionDatePropertyChanged);
            PropertyDictionary.Add(OrderReleasesBase.Columns.EstStartDate.ToString(), EstStartDatePropertyChanged);
            PropertyDictionary.Add(OrderReleasesBase.Columns.CustId.ToString(), CustIdPropertyChanged);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.EstStartDate.ToString(), SetDetailControls);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.Qty.ToString(), SetDetailControls);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.UOM.ToString(), SetDetailControls);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.Routing.ToString(), SetDetailControls);
            EntityPropertyDictionary.Add(OrderReleasesBase.Columns.EstCompletionDate.ToString(), SetDetailControls);
        }

        protected virtual async Task LoadOrder(string orderNo)
        {
            if (this.Provider.Items.Exists(o => StringHelper.AreEqual(o.OrderNo, orderNo, false)))
                return;

            SqlFilterBuilder<OrderBase.Columns> builder = new SqlFilterBuilder<OrderBase.Columns>();
            builder.AppendEquals(OrderBase.Columns.OrderNo, orderNo);

            await Task.Run(() => this.Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), "")));
            if (this.Provider.Items.Count > 0)
                this.Order = this.Provider.Items[0];
            else
                throw new InvalidValueException(string.Format("Order No '{0}' could not be found.", orderNo));
        }

        protected virtual async Task<EntityList<OrderReleases>> Load(string orderNo, int? releaseNo)
        {
            await this.LoadOrder(orderNo);

            if (releaseNo.HasValue)
            {
                var list = this.Order.DetailList.FindAll(OrderReleasesBase.Columns.ReleaseNo, releaseNo.Value);
                return list;
            }

            return this.Order.DetailList;
        }

        protected virtual async Task<OrderReleases> Find(string orderNo, int releaseNo)
        {
            var list = await Load(orderNo, releaseNo);
            return list.Count > 0 ? list[0] : null;
        }

        protected virtual async Task<List<OrderReleases>> ProcessEditRequest(bool isCreate, dynamic body, string orderNo, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Order Release Number is provided along with more than one record.");

            var entityList = new List<OrderReleases>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, orderNo, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<OrderReleases> ProcessBodyItem(bool isCreate, dynamic bodyItem, string orderNo, int? id)
        {
            int? code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ReleaseNo) || bodyItem.ReleaseNo == null)
                bodyItem.ReleaseNo = code;
            else
                code = (int)bodyItem.ReleaseNo;

            var entity = await this.Find(orderNo, code.GetValueOrDefault());

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.Order.DetailList.AddNew();
                entity.SetDefaults();
                if (code.GetValueOrDefault() == 0)
                    bodyItem.ReleaseNo = code = entity.ReleaseNo; //be sure to set the object to the default value if the user did not provide a number
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Release Number '{0}' could not be found on order '{1}'.", code, orderNo));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string orderNo, int releaseNo)
        {
            var entity = await this.Find(orderNo, releaseNo);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Release number '{0}' could not be found for order '{1}'.", releaseNo, orderNo));

            this.Order.DetailList.RemoveEntity(entity);
            this.Provider.Update(this.CompId);
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
                this.LoadPurchaseOrder(entity.CustId, entity.SalesOrder);
        }
        #endregion Helper Methods

        #region Event Methods
        private void OrderRelease_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnOrderRelease_PropertyChanged(sender as OrderReleases, e);
        }
        public virtual void OnOrderRelease_PropertyChanged(OrderReleases entity, PropertyChangedEventArgs e)
        {
            if (entity != null)
            {                             
                if (e.PropertyName == OrderReleasesBase.Columns.EstCompletionDate.ToString())
                {
                    this.SetEstStartDate(entity);
                    entity.Validate(OrderReleasesBase.Columns.EstStartDate.ToString());
                }
                if (e.PropertyName == OrderReleasesBase.Columns.EstStartDate.ToString())
                {
                    entity.Validate(OrderReleasesBase.Columns.EstCompletionDate.ToString());
                }
            }           
        }

        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<OrderReleases> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as OrderReleases);
        }
        #endregion Event Methods

        #region Properties
        protected SortedDictionary<string, Action<OrderReleases>> PropertyDictionary { get; } = new SortedDictionary<string, Action<OrderReleases>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private OrderProvider Provider { get; } = new OrderProvider();

        public const string FunctionID = "5CE0A03F-A994-48DA-A3A3-53D152E24C08";

        private Order Order { get; set; }
        #endregion Properties
    }
}
