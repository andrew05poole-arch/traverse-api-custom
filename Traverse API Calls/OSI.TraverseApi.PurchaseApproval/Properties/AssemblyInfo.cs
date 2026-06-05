using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("OSI.TraverseApi.PurchaseApproval")]
[assembly: AssemblyDescription("TRAVERSE API custom plugin — PO Approval workflow endpoint. " +
    "Exposes POST /api/v2/po/approval/{transId} to approve or decline a PO Request via " +
    "the TRAVERSE PurchaseApproval business layer.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("c4d5e6f7-a8b9-4c2d-9e3f-0a1b2c3d4e5f")]

// Version is shared with the rest of the plugin suite via ApiAssemblyInfo.cs (linked file).
// Do NOT add AssemblyVersion / AssemblyFileVersion here — they live in ApiAssemblyInfo.cs.
