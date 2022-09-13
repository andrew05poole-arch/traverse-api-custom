#region Using Directives
using TRAVERSE.Business.AccountsPayable;
#endregion Using Directives

namespace TRAVERSE.Web.API.AccountsPayable.Models
{
    public class ApOpenInvoice : OpenInvoice
    {
    public enum OIStatus : byte
        {
            Released = 0,
            Hold = 1,
            Temp = 2,
            Prepaid = 3,
            Paid = 4
        }
    }
}
