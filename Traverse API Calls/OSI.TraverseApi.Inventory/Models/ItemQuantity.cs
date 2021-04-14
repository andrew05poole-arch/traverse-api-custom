#region Using Directives
using TRAVERSE.Business;
#endregion Using Directives

namespace OSI.TraverseApi.Inventory.Models
{
    public class ItemQuantity
    {
        #region Properties
        public string ItemId { get; set; }
        public string LocationId { get; set; }
        public decimal Available { get; set; }
        public decimal Committed { get; set; }
        public decimal OnHand { get; set; }
        public decimal OnOrder { get; set; }
        public string Unit { get; set; }
        #endregion Properties
    }
}