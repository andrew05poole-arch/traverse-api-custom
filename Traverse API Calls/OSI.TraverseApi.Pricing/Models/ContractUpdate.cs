#region Using Directives
using System.ComponentModel;
#endregion Using Directives

namespace TRAVERSE.Web.API.Pricing.Models
{
    public class ContractUpdate
    {
        #region Constructor
        public ContractUpdate()
        { }
        #endregion Constructor

        #region Properties
        [Bindable(true)]
        public bool UpdateCustomers { get; set; }
        [Bindable(true)]
        public bool UpdateItems { get; set; }
        [Bindable(true)]
        public string Comments { get; set; }
        #endregion Properties
    }
}
