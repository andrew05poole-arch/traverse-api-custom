using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoShipmentLandedCostDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "shipment/{shipNum}/landedCostDetail/{id:long}", typeof(ShipmentLandedCostDetail))]
        public async Task<IHttpActionResult> Get(string shipNum, long id)
        {
            return Ok(await this.Load(shipNum, id, false));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipNum}/landedCostDetail/{id:long}", typeof(ShipmentLandedCostDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string shipNum, long id)
        {
            return Ok(await ProcessEditRequest(false, body, shipNum, id));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipNum}/landedCostDetail", typeof(ShipmentLandedCostDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string shipNum)
        {
            return Ok(await ProcessEditRequest(true, body, shipNum, null));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipNum}/landedCostDetail/{id:long}", typeof(ShipmentLandedCostDetail))]
        public async Task Delete(string shipNum, long id)
        {
            await this.MarkToDelete(shipNum, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
        }

        protected virtual async Task<EntityList<ShipmentLandedCostDetail>> Load(string shipNum, long? id, bool isCreate)
        {
            var list = CurrentShipmentHeader?.LandedCostDetailList as EntityList<ShipmentLandedCostDetail>;

            if (CurrentShipmentHeader == null || !this.Provider.Items.Exists(i => i.ShipNum == shipNum))
            {
                var builder = new SqlFilterBuilder<ShipmentHeaderBase.Columns>();
                builder.AppendEquals(ShipmentHeaderBase.Columns.ShipNum, shipNum.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPoShipmentController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Shipment No '{0}' could not be found.", shipNum));

                CurrentShipmentHeader = Provider.Items[0];

                list = CurrentShipmentHeader.LandedCostDetailList as EntityList<ShipmentLandedCostDetail>;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue && !isCreate)
            {
                list = list.FindAll(ShipmentLandedCostDetailBase.Columns.Id, id.Value);
                if (list.Count <= 0)
                {
                    throw new InvalidValueException(string.Format("Landed Cost Detail Id '{0}' could not be found on Shipment '{1}'.", id, shipNum));
                }
                else
                    return list;
            }

            return list;
        }

        protected virtual async Task<ShipmentLandedCostDetail> Find(string shipNum, long id, bool isCreate)
        {
            var list = await Load(shipNum, id, isCreate);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<ShipmentLandedCostDetail>> ProcessEditRequest(bool isCreate, dynamic body, string shipNum, long? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Landed Cost Detail Id is provided along with more than one record.");

            var entityList = new List<ShipmentLandedCostDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, shipNum, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ShipmentLandedCostDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string shipNum, long? id)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = id.GetValueOrDefault();
            else
                id = bodyItem.Id;

            var entity = await this.Find(shipNum, id.GetValueOrDefault(), isCreate);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentShipmentHeader?.LandedCostDetailList?.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Landed Cost Detail Id '{0}' could not be found on Shipment '{1}'.", id, shipNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string shipNum, long id)
        {
            var entity = await this.Find(shipNum, id, false);

            if (entity == null)
                throw new InvalidValueException(string.Format("Landed Cost Detail Id '{0}' could not be found on Shipment '{1}'", id, shipNum));

            CurrentShipmentHeader?.LandedCostDetailList?.Remove(entity);
            this.Provider.Update(this.CompId);
        }
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
            Action<ShipmentLandedCostDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ShipmentLandedCostDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected ShipmentHeaderProvider Provider { get; } = new ShipmentHeaderProvider();

        protected ShipmentHeader CurrentShipmentHeader { get; set; }

        protected SortedDictionary<string, Action<ShipmentLandedCostDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ShipmentLandedCostDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "8a7b2a84-b815-4781-bef1-c8771b76c4fc";
        #endregion Fields
    }
}
