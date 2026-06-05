# TRAVERSE API Custom — Tier 4: PO Approval

> **Package:** `tier4.zip`
> **DLL:** `OSI.TraverseApi.PurchaseApproval.dll` (v11.2.26030.0)
> **Adds:** `POST /api/v2/po/approval/{transId}` — approve or decline a PO Request via the TRAVERSE business layer

This is a drop-in TRAVERSE API plugin. Copy the DLL, run the database script, recycle the app pool — done.

---

## What it does

| Endpoint | Description |
|---|---|
| `GET  /api/v2/po/approval/{transId}?company={compId}` | Returns current approval status of a PO request |
| `POST /api/v2/po/approval/{transId}?company={compId}` | Approves or declines the request |

**On Approve:**
- `tblPoTransRequest.Status` → `2` (Approved)
- `tblPoTransRequest.ApprovedBy` / `ApprovedDate` stamped
- `tblPoTransHeader.TransType` → `9` (New Order) — via `ConvertToOrder()` in the TRAVERSE business layer

**On Decline:**
- `tblPoTransRequest.Status` → `3` (Declined)
- `tblPoTransRequest.ApprovedBy` / `ApprovedDate` stamped
- `TransType` unchanged

---

## Prerequisites

| Requirement | Detail |
|---|---|
| TRAVERSE REST API | Installed and running in IIS |
| SQL Server access | `db_owner` on the **SYS** database for the registration script |
| TRAVERSE API bearer token | Service account recommended for automated (Engage) calls |

---

## Step 1 — Register the Function in the SYS Database

The script **`Register-PoApproval.sql`** is included inside `tier4.zip`. Extract the zip and run it against the **SYS** database.

```powershell
# Option A — SQLCMD (adjust server name as needed)
sqlcmd -S localhost\GLOBAL -d SYS -i Register-PoApproval.sql

# Option B — open in SSMS and execute against SYS
```

**Before running, edit the two placeholders at the top of each block:**

| Placeholder | Replace with |
|---|---|
| `'tradmin'` | Your API service account username (from `tblSmApiUser.UserId`) |
| `'CPU'` | Your TRAVERSE company ID |

The script is idempotent — safe to re-run on upgrades. It registers three rows:
- `tblSmApiFunction` — the function record
- `tblSmApiUserFunction` — grants the user read + new access
- `tblSmApiUserFunctionComp` — grants access per company

> **Multiple companies?** Repeat the Step 1c block in the script for each additional `CompId`.

---

## Step 2 — Copy the DLL to ApiPlugins

Extract `tier4.zip` and copy the DLL to the TRAVERSE API `bin\ApiPlugins\` folder:

```powershell
# Adjust the path to match your IIS site
Copy-Item "bin\OSI.TraverseApi.PurchaseApproval.dll" `
          "C:\inetpub\wwwroot\{your-site}\bin\ApiPlugins\" -Force
```

> The TRAVERSE API host discovers plugin DLLs automatically on startup from `bin\ApiPlugins\`.
> No Web.config changes required.

---

## Step 3 — Recycle the App Pool

```powershell
Restart-WebAppPool -Name "TraverseAPI"
# Or: iisreset
```

---

## Step 4 — Verify

```powershell
# Should return 401 (route is live, auth required) — NOT 404
Invoke-WebRequest "http://localhost/api/v2/po/approval/00000339?company=CPU" -Method GET

# With a bearer token:
$headers = @{ Authorization = "Bearer YOUR_TOKEN_HERE" }
Invoke-RestMethod "http://localhost/api/v2/po/approval/00000339?company=CPU" `
    -Method GET -Headers $headers
# Expected: JSON with transId, status=0, requestedBy, etc.
```

---

## Step 5 — Test Approve / Decline

```powershell
$headers = @{
    Authorization  = "Bearer YOUR_TOKEN_HERE"
    "Content-Type" = "application/json"
}

# Approve
$body = '{ "action": "approve", "comments": "Approved via API test" }'
Invoke-RestMethod "http://localhost/api/v2/po/approval/00000339?company=CPU" `
    -Method POST -Headers $headers -Body $body

# Verify in SQL:
# SELECT TransId, Status, ApprovedBy, ApprovedDate FROM tblPoTransRequest WHERE TransId = '00000339'
# SELECT TransId, TransType FROM tblPoTransHeader WHERE TransId = '00000339'
# Expected: Status=2, TransType=9
```

---

## Request / Response Reference

### POST body

```json
{
  "action":   "approve",
  "comments": "Approved via TRAVERSE Engage by manager@contoso.com"
}
```

| Field | Required | Values |
|---|---|---|
| `action` | ✅ | `"approve"` or `"decline"` |
| `comments` | ✗ | Free text; recorded for audit. Include Entra identity for Engage-originated approvals. |
| `level` | ✗ | Route level (reserved for Phase 2 multi-level routing; ignored in Phase 1) |

### Response

Returns the updated `TransactionRequest` entity wrapped in the standard TRAVERSE API envelope:

```json
{
  "total": 1,
  "data": [{
    "transId":       "00000339",
    "groupId":       "00000339",
    "requestedDate": "2026-06-04T00:00:00",
    "requestedBy":   "tradmin",
    "approvedDate":  "2026-06-05T14:22:00",
    "approvedBy":    "tradmin",
    "status":        2
  }]
}
```

### Status codes

| Status | Meaning |
|---|---|
| `0` | New |
| `1` | Pending |
| `2` | **Approved** |
| `3` | **Declined** |
| `4` | Cancelled |

### HTTP errors

| HTTP | Cause |
|---|---|
| `400` | Missing/invalid `action`, or request already actioned |
| `401` | Missing or invalid bearer token |
| `403` | API user not authorized for this function/company |
| `404` | `transId` not found |

---

## TRAVERSE Engage Integration

This endpoint is the Phase 2 backend for `PoRequisitionApprovalAction` in `traverse-engage`.

`PoRequisitionApprovalAction` calls:
```
POST /api/v2/po/approval/{transId}?company={compId}
Authorization: Bearer <service-account-token>
{ "action": "approve"|"decline",
  "comments": "Approved via TRAVERSE Engage by {Entra UPN} at {UTC timestamp}" }
```

Configure the service base URL in `traverse-engage` Web.config:
```xml
<add key="TraverseApi.EngageServiceBaseUrl" value="http://localhost" />
```

---

## Troubleshooting

| Symptom | Check |
|---|---|
| `404` on the route | DLL not in `ApiPlugins\`, or app pool not recycled |
| `403 Forbidden` | Run Step 1 SQL; confirm `CompId` and `UserId` match your environment |
| `400 — Only New or Pending requests can be actioned` | Request already approved/declined; check `tblPoTransRequest.Status` |
| `400 — Transaction is invalid` (from business layer) | `TransType` on the header is not `0` (Request); request may already be an order |
| DLL loads but route not found | Confirm `FunctionID` in Step 1 SQL matches the one in the controller: `5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F` |

---

## Uninstalling

1. Delete `OSI.TraverseApi.PurchaseApproval.dll` from `bin\ApiPlugins\`
2. Recycle the app pool — the route returns `404` immediately
3. (Optional) Remove the three rows inserted in Step 1 from `tblSmApiFunction`, `tblSmApiUserFunction`, `tblSmApiUserFunctionComp`

---

## Source

Plugin source: `Traverse API Calls/OSI.TraverseApi.PurchaseApproval/`
GitHub: https://github.com/andrew05poole-arch/traverse-api-custom
