#region Using Directives
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Inventory.Controllers
{
    public class ApiInSerialNumberController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "item/{itemid}/location/{locationid}/serial/{id?}", typeof(SerialItem))]
        public async Task<IHttpActionResult> Get(string itemId, string locationId, string id = null)
        {
            return this.Ok(await Load(itemId, locationId, id));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        { }

        protected virtual async Task Load(string itemId, string id)
        {
            var list = CurrentItem?.AllLocations;

            if (CurrentItem == null || !StringHelper.AreEqual(CurrentItem.ItemId, itemId, false))
            {
                var builder = new SqlFilterBuilder<ItemBase.Columns>();
                builder.AppendEquals(ItemBase.Columns.ItemId, itemId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiInItemController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Item '{0}' could not be found.", itemId));

                CurrentItem = Provider.Items[0];

                list = CurrentItem.AllLocations;
                await this.FilterEntityListAsync(list, ApiInItemLocationController.FunctionID);
            }

            CurrentItem.CurrentLocation = CurrentItem.GetLocationById(id);
            if (CurrentItem.CurrentLocation == null)
                throw new InvalidValueException(string.Format("Location '{0}' cannot be found on item '{1}'.", id, itemId));
        }

        private async Task<EntityList<SerialItem>> Load(string itemId, string locationId, string id)
        {
            var list = CurrentItem?.CurrentLocation?.SerialItems;

            if (CurrentItem?.CurrentLocation == null ||
                !StringHelper.AreEqual(CurrentItem?.ItemId ?? string.Empty, itemId, false) ||
                !StringHelper.AreEqual(CurrentItem?.CurrentLocation?.LocId ?? string.Empty, locationId, false))
            {
                await Load(itemId, locationId);

                list = CurrentItem.CurrentLocation.SerialItems;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                list = list.FindAll(SerialItemBase.Columns.SerNum, id, true);

            return list;
        }
        #endregion Helper Methods

        #region Properties
        protected ItemProvider Provider { get; } = new ItemProvider();

        protected Item CurrentItem { get; set; }

        private const string FunctionID = "84CE93B9-1438-405D-8939-CC5626C6D76C";
        #endregion Properties
    }
}
