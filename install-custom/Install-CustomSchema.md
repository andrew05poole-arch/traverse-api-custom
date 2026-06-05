# OSI.TraverseApi.PurchaseApproval — Installation Guide

Custom TRAVERSE API plugin exposing the PO Approval workflow endpoint.

---

## Prerequisites

- TRAVERSE REST API host already installed and running in IIS.
- `TRAVERSE.Business.PurchaseOrder.dll` present in the host `\bin\` (standard for any TRAVERSE install).
- A TRAVERSE API user account with a bearer token (service account recommended for Engage integration).

---

## Step 1 — Build the DLL

Open a Developer Command Prompt and run from the repo root:

```powershell
nuget restore "Traverse API Calls\OSI.TraverseApi.Global.sln" -ConfigFile nuget.config
msbuild "Traverse API Calls\OSI.TraverseApi.PurchaseApproval\OSI.TraverseApi.PurchaseApproval.csproj" `
        /p:Configuration=Release /p:Platform=x64
```

Output: `Traverse API Calls\OSI.TraverseApi.PurchaseApproval\bin\x64\Release\OSI.TraverseApi.PurchaseApproval.dll`

If `$(EverstarGlobalAPI)` is set, the post-build event copies the DLL automatically.

---

## Step 2 — Copy DLL to ApiPlugins

```powershell
copy /Y "...\OSI.TraverseApi.PurchaseApproval.dll" "C:\inetpub\wwwroot\bin\ApiPlugins\"
```

---

## Step 3 — Register the function in the TRAVERSE API database

The `[ApiAuthorize]` attribute checks that the `FunctionID` is registered for the calling user
and company. Run this script against the **SYS** database:

```sql
-- ── 1. Register the function ──────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[tblSmApiFunction]
    WHERE [FunctionId] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
)
BEGIN
    INSERT INTO [dbo].[tblSmApiFunction]
        ([FunctionId], [FunctionName], [Description], [AllowRead], [AllowNew], [AllowEdit], [AllowDelete])
    VALUES
        ('5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F',
         'po/approval',
         'PO Approval — approve or decline a Purchase Order Request',
         1,  -- AllowRead  (GET)
         1,  -- AllowNew   (POST)
         0,  -- AllowEdit  (PUT — not used)
         0); -- AllowDelete (DELETE — not used)
    PRINT 'Registered tblSmApiFunction for PO Approval';
END
ELSE
    PRINT 'tblSmApiFunction entry already exists — skipped';
GO

-- ── 2. Grant to the API user ───────────────────────────────────────────────────
-- Replace @ApiUserId with the numeric ID from tblSmApiUser for your service account.
DECLARE @ApiUserId BIGINT = (
    SELECT [ID] FROM [dbo].[tblSmApiUser]
    WHERE  [UserId] = 'tradmin'   -- <-- change to your API service account username
);

IF @ApiUserId IS NULL
BEGIN
    RAISERROR ('API user not found. Update the WHERE clause with the correct UserId.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[tblSmApiUserFunction]
    WHERE [UserId] = @ApiUserId
      AND [FunctionId] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
)
BEGIN
    INSERT INTO [dbo].[tblSmApiUserFunction]
        ([UserId], [FunctionId], [AllowRead], [AllowNew], [AllowEdit], [AllowDelete])
    VALUES
        (@ApiUserId, '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F', 1, 1, 0, 0);
    PRINT 'Granted tblSmApiUserFunction';
END
GO

-- ── 3. Grant per company ──────────────────────────────────────────────────────
-- Run once per company database the approver endpoint should be accessible for.
-- Replace 'CPU' with your company ID.
DECLARE @ApiUserId BIGINT = (
    SELECT [ID] FROM [dbo].[tblSmApiUser]
    WHERE  [UserId] = 'tradmin'   -- <-- change to match
);

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[tblSmApiUserFunctionComp]
    WHERE [UserId] = @ApiUserId
      AND [FunctionId] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
      AND [CompId] = 'CPU'        -- <-- change to your company ID
)
BEGIN
    INSERT INTO [dbo].[tblSmApiUserFunctionComp]
        ([UserId], [FunctionId], [CompId], [AllowRead], [AllowNew], [AllowEdit], [AllowDelete])
    VALUES
        (@ApiUserId, '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F', 'CPU', 1, 1, 0, 0);
    PRINT 'Granted tblSmApiUserFunctionComp for company CPU';
END
GO
```

---

## Step 4 — Restart IIS

```powershell
Restart-WebAppPool -Name "TraverseAPI"
```

---

## Step 5 — Verify

```powershell
# Should return 401 (route found, auth required) not 404
Invoke-WebRequest "http://localhost/api/v2/po/approval/00000339" -Method GET

# With a valid bearer token (replace <token> and company):
Invoke-RestMethod "http://localhost/api/v2/po/approval/00000339?company=CPU" `
    -Method GET `
    -Headers @{ Authorization = "Bearer <token>" }
```

---

## API Reference

### GET `/api/v2/po/approval/{transId}?company={compId}`

Returns the current approval status of a PO request.

```json
{
  "total": 1,
  "data": [{
    "transId": "00000339",
    "groupId": "00000339",
    "requestedDate": "2026-06-04T00:00:00",
    "requestedBy": "tradmin",
    "approvedDate": null,
    "approvedBy": null,
    "status": 0
  }]
}
```

| Status | Meaning |
|---|---|
| `0` | New |
| `1` | Pending |
| `2` | Approved |
| `3` | Declined |
| `4` | Cancelled |

---

### POST `/api/v2/po/approval/{transId}?company={compId}`

Approve or decline the request.

**Request:**
```json
{
  "action":   "approve",
  "comments": "Approved via TRAVERSE Engage by manager@contoso.com"
}
```

**On approve:** `tblPoTransRequest.Status → 2`, `ApprovedBy`/`ApprovedDate` set,
`tblPoTransHeader.TransType 0 → 9` (New Order).

**On decline:** `tblPoTransRequest.Status → 3`, `ApprovedBy`/`ApprovedDate` set,
`tblPoTransHeader.TransType` unchanged.

**Response:** updated `TransactionRequest` entity (same shape as GET).

**Errors:**

| HTTP | Condition |
|---|---|
| `400` | Missing/invalid `action` field |
| `400` | Request not in New or Pending status |
| `401` | Missing or invalid bearer token |
| `403` | User not authorized for this function/company |
| `404` | TransId not found |

---

## Phase 2 — Multi-level routing (future)

Phase 1 calls `TransactionRequest.Approve()` / `Decline()` directly, which is correct for
single-level and direct-approval scenarios. Phase 2 will wire to
`RequestResponseProcess` (in `TRAVERSE.Business.PurchaseApproval`) to support:

- Multi-level route progression
- Writing `tblPoTransRequestResponse` per approver
- Advancing `tblPoTransRequestStatus` to the next approver level
- Triggering TRAVERSE's own email notifications to the next approver

To enable: uncomment the `TRAVERSE.Business.PurchaseApproval` reference in the `.csproj`
and update the controller to instantiate and execute `RequestResponseProcess`.

---

## TRAVERSE Engage integration

`PoRequisitionApprovalAction` (in `traverse-engage`) calls this endpoint using the API
service-account bearer token. The Entra identity of the person who clicked the Engage
link is passed in the `comments` field.

Configure in `traverse-engage` `Web.config`:
```xml
<add key="TraverseApi.EngageServiceBaseUrl"  value="http://localhost" />
<add key="ApiTravUsername"                   value="tradmin" />
```
