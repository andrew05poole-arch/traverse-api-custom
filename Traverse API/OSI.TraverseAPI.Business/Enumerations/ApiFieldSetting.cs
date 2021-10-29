using System;
using System.ComponentModel;
namespace OSI.TraverseApi.Business
{
    [Flags]
    public enum ApiFieldSetting : byte
    {
        None = 0,
        Read = 1,
        Create = 2,
        Edit = 4,
        Delete = 8,
        [Description("Required to Create")]
        Required_Create = 16,
        [Description("Required to Edit")]
        Required_Edit = 32,
        [Description("Required to Delete")]
        Required_Delete = 64
    }
}
