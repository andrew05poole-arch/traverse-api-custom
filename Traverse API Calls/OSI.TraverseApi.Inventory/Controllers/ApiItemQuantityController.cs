#region Using Directives
using TRAVERSE.Web.API.Inventory.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Inventory.Controllers
{
    public class ApiItemQuantityController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "itemqty/{itemId}/unit/{uom?}", typeof(ItemQuantity))]
        [ApiRoute(FunctionID, 2f, "itemqty/{itemId}/unit/{uom}/location/{locationId?}", typeof(ItemQuantity))]
        public IHttpActionResult Get(string itemId = null, string uom = null, string locationId = null)
        {
            if (!string.IsNullOrEmpty(itemId))
                return Ok(Load(itemId, uom, locationId));

            return Ok(Load(null, null, null));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        { }

        private List<ItemQuantity> Load(string itemId, string unit, string locationId)
        {
            SqlFilterBuilder<ItemBase.Columns> builder = new SqlFilterBuilder<ItemBase.Columns>();
            builder.AppendEquals(ItemBase.Columns.ItemId, itemId);

            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The Item ID '{0}' could not be found.", itemId));

            return GetItemQty(Provider.Items[0], unit, locationId);
        }

        private List<ItemQuantity> GetItemQty(Item itemInfo, string unit, string locationId)
        {
            string selectedUnit = !string.IsNullOrEmpty(unit) ? unit : itemInfo.UomDflt;

            List<ItemQuantity> itemQtyList = new List<ItemQuantity>();

            foreach (object obj in itemInfo.GetQuantityForAllLocations(selectedUnit).Rows)
            {
                DataRow dataRow = (DataRow)obj;
                ItemQuantity itemDetail = new ItemQuantity
                {
                    ItemId = Convert.ToString(dataRow["ItemId"]),
                    LocationId = Convert.ToString(dataRow["LocId"]),
                    Available = Convert.ToDecimal(dataRow["QtyAvail"]),
                    Committed = Convert.ToDecimal(dataRow["QtyCmtd"]),
                    OnHand = Convert.ToDecimal(dataRow["QtyOnHand"]),
                    OnOrder = Convert.ToDecimal(dataRow["QtyOnOrder"]),
                    Unit = selectedUnit
                };

                itemQtyList.Add(itemDetail);
            }

            if (!string.IsNullOrEmpty(locationId))
            {
                return itemQtyList.FindAll(x => string.Compare(x.LocationId, locationId, true) == 0);
            }
            return itemQtyList;
        }
        #endregion Helper Methods

        #region Properties
        private ItemProvider Provider { get; } = new ItemProvider();

        private const string FunctionID = "51AD67F5-7693-43B0-A4FF-9040114ED2B0";
        #endregion Properties
    }
}
