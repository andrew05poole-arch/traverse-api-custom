#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
using System.Threading.Tasks;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoRequisitionController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "requisition/{reqid?}", typeof(Requisition))]
        public async Task<IHttpActionResult> Get(int id)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "requisition/{reqid?}", typeof(Requisition))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int id)
        {

            return Ok(await ProcessEditRequest(false, body, id));

        }

        [ApiRoute(FunctionID, 2f, "requisition", typeof(Requisition))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "requisition/{reqid}", typeof(Requisition))]
        public async Task<IHttpActionResult> Delete(int id)
        {
            await this.MarkToDelete(id);
            this.Provider?.Update(this.CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {

            PropertyDictionary.Add(RequisitionBase.Columns.VendorId.ToString(), VendorIdPropertyChanged);
            PropertyDictionary.Add(RequisitionBase.Columns.ItemId.ToString(), ItemIdPropertyChanged);
            PropertyDictionary.Add(RequisitionBase.Columns.CurrencyId.ToString(), VendorIdPropertyChanged);
            PropertyDictionary.Add(RequisitionBase.Columns.LocId.ToString(), LocIdIdPropertyChanged);
            PropertyDictionary.Add(RequisitionBase.Columns.Qty.ToString(), QtyPropertyChanged);
            PropertyDictionary.Add(RequisitionBase.Columns.Uom.ToString(), UOMPropertyChanged);
            PropertyDictionary.Add(RequisitionBase.Columns.UnitCost.ToString(), UnitCostPropertyChanged);
            PropertyDictionary.Add(RequisitionBase.Columns.ExtCost.ToString(), ExtCostPropertyChanged);


        }
        protected virtual async Task<List<Requisition>> ProcessEditRequest(bool isCreate, dynamic bodyItem, int? id)
        {
            object[] list;

            if (bodyItem is object[])
                list = bodyItem;
            else
                list = new object[1] { bodyItem };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ReqId Id is provided along with more than one record.");

            var entityList = new List<Requisition>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);

                await this.ValidateEntityListAsync(entityList);
                this.Provider?.Update(this.CompId);
            }

            return entityList;
        }

        protected virtual async Task<Requisition> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
        {
            int? code = ApiUserSkipped.IsApiUserSkipped(bodyItem.ReqId) ? bodyItem.ReqId = id : Convert.ToInt32(bodyItem.ReqId);

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Requisition(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("ReqId  '{0}' could not be found.", id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            //entity.Lock = (!ApiUserSkipped.IsApiUserSkipped(bodyItem.Lock)) ? (bool)bodyItem.Lock : false;
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int id)
        {
            Requisition entity = null;
            var list = await Load(id);
            if (list.Count > 0)
                entity = list[0];

            if (entity == null)
                throw new InvalidValueException(string.Format("ReqId '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual async Task<EntityList<Requisition>> Load(int? id)
        {
            if (id != null && (Provider.Items.Count <= 0 || !Provider.Items.Exists(i => Equals(id, i.ReqId))))
            {

                var builder = new SqlFilterBuilder<Requisition.Columns>();
                builder.AppendEquals(Requisition.Columns.ReqId, id.ToString());
                var list = new RequisitionProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                Provider.Items.AddRange(list);
                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Requisition> Find(int? id)
        {
            var list = await Load(id);
            return list.Find(x => Equals(x.ReqId, id));
        }
        #endregion Helper Methods

        #region  Update Methods
        protected virtual void VendorIdPropertyChanged(Requisition entity)
        {
            if (!string.IsNullOrEmpty(entity.VendorId) && entity.IsNew)
            {
                entity.SetVendorDefaults();
            }

        }

        protected virtual void ItemIdPropertyChanged(Requisition entity)
        {
            if (entity.ItemId != null && entity.IsNew)
            {
                entity.SetItemDefaults();

            }
        }
        protected virtual void LocIdIdPropertyChanged(Requisition entity)
        {
            if ((entity.LocId) != null && entity.IsNew)
            {
                entity.SetItemLocationDefaults();
            }
        }
        protected virtual void QtyPropertyChanged(Requisition entity)
        {
            entity.SetCostDefaults();
            entity.CalculateExtendedCost();
            return;
        }
        protected virtual void UOMPropertyChanged(Requisition entity)
        {
            if (!string.IsNullOrEmpty(entity.LocId))
            {
                entity.SetCostDefaults();
            }
        }
        protected virtual void UnitCostPropertyChanged(Requisition entity)
        {
            if ((entity.UnitCost) != null && entity.IsNew)
            {
                entity.SetVendorDefaults();
            }
        }
        protected virtual void ExtCostPropertyChanged(Requisition entity)
        {
            if ((entity.ExtCost) != null && entity.IsNew)
            {
                entity.SetVendorDefaults();
            }
        }
        #endregion Update Methods

        #region Event Handler
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Requisition> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Requisition);
        }

        #endregion Event Handler

        #region Properties
        private RequisitionProvider Provider { get; } = new RequisitionProvider();

        protected SortedDictionary<string, Action<Requisition>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Requisition>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "F9B9EE5D-B001-468C-9C84-B13224FA4AE9";
        #endregion Fields
    }
}