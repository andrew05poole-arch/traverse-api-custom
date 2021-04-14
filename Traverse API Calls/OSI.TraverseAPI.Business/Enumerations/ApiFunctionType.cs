using System.ComponentModel;

namespace OSI.TraverseApi.Business
{
    public enum ApiFunctionType : byte
    {
        [Description("Setup and Maintenance")]
        SetupMaintenance = 1,
        Transactions = 2,
        Other = 3
    }
}
