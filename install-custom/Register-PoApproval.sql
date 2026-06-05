-- ============================================================
-- TRAVERSE API Custom — Tier 4: PO Approval
-- Register-PoApproval.sql
--
-- Run against the TRAVERSE SYS database before deploying
-- OSI.TraverseApi.PurchaseApproval.dll.
--
-- Idempotent: safe to re-run (IF NOT EXISTS / SCOPE guards).
--
-- Before running, update the two placeholders:
--   'tradmin'  → your API service account (from tblSmApiUser.UserId)
--   'CPU'      → your TRAVERSE company ID
-- Repeat Step 1c for each additional company as needed.
--
-- Scope bitfield (tblSmApiFunctionHeader.Scope and
--                 tblSmApiUserFunctionComp.Scope):
--   1 = GET    (AllowRead)
--   2 = PUT    (AllowEdit)
--   4 = POST   (AllowNew)
--   8 = DELETE (AllowDelete)
--   Values are combined with bitwise OR.
--   This endpoint uses 5 = GET(1) + POST(4).
-- ============================================================

USE [SYS];
GO

-- ── Step 1a: Register the API function ───────────────────────────────────────
-- Inserts into tblSmApiFunctionHeader — the master function registry.
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[tblSmApiFunctionHeader]
    WHERE  [ID] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
)
BEGIN
    INSERT INTO [dbo].[tblSmApiFunctionHeader]
        ([ID], [Name], [AppId], [Type], [Notes], [Scope], [OverrideID])
    VALUES
        ('5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F',
         'po/approval',              -- display name; matches route prefix
         'PO',                       -- TRAVERSE application code
         2,                          -- Type: 1=Setup, 2=Transactions, 3=Other
         'PO Approval — approve or decline a Purchase Order Request via the TRAVERSE Engage workflow',
         5,                          -- Scope: 1(GET) + 4(POST) = 5
         NULL);                      -- OverrideID: NULL = new function
    PRINT 'Step 1a: Registered tblSmApiFunctionHeader for PO Approval.';
END
ELSE
    PRINT 'Step 1a: tblSmApiFunctionHeader entry already exists — skipped.';
GO

-- ── Step 1b: Grant to the API user ───────────────────────────────────────────
-- !! Change 'tradmin' to your API service account username !!
DECLARE @UserId BIGINT = (
    SELECT [ID] FROM [dbo].[tblSmApiUser]
    WHERE  [UserId] = 'tradmin'    -- <-- UPDATE THIS
);

IF @UserId IS NULL
BEGIN
    RAISERROR('Step 1b ERROR: API user not found. Update the WHERE clause with the correct UserId from tblSmApiUser.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[tblSmApiUserFunction]
    WHERE  [UserID]     = @UserId
      AND  [FunctionID] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
)
BEGIN
    INSERT INTO [dbo].[tblSmApiUserFunction]
        ([UserID], [FunctionID], [AccessExpireDate],
         [DateCreated], [DateModified], [ModifiedBy])
    VALUES
        (@UserId,
         '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F',
         NULL,                       -- AccessExpireDate: NULL = never expires
         GETDATE(), GETDATE(), SYSTEM_USER);
    PRINT 'Step 1b: Granted tblSmApiUserFunction.';
END
ELSE
    PRINT 'Step 1b: tblSmApiUserFunction entry already exists — skipped.';
GO

-- ── Step 1c: Grant per company ────────────────────────────────────────────────
-- tblSmApiUserFunctionComp is keyed by UserFunctionID (FK to tblSmApiUserFunction.ID),
-- not by UserID + FunctionID directly.
-- !! Change 'tradmin' and 'CPU' to match your environment !!
-- Repeat this entire block for every additional company.
DECLARE @UserId BIGINT = (
    SELECT [ID] FROM [dbo].[tblSmApiUser]
    WHERE  [UserId] = 'tradmin'    -- <-- UPDATE THIS
);

IF @UserId IS NULL
BEGIN
    RAISERROR('Step 1c ERROR: API user not found. Update the WHERE clause with the correct UserId from tblSmApiUser.', 16, 1);
    RETURN;
END

DECLARE @UserFunctionId BIGINT = (
    SELECT [ID] FROM [dbo].[tblSmApiUserFunction]
    WHERE  [UserID]     = @UserId
      AND  [FunctionID] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
);

IF @UserFunctionId IS NULL
BEGIN
    RAISERROR('Step 1c ERROR: tblSmApiUserFunction row not found. Ensure Step 1b completed successfully.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[tblSmApiUserFunctionComp]
    WHERE  [UserFunctionID] = @UserFunctionId
      AND  [CompID]         = 'CPU'    -- <-- UPDATE THIS
)
BEGIN
    INSERT INTO [dbo].[tblSmApiUserFunctionComp]
        ([UserFunctionID], [CompID], [Scope],
         [Filter], [DisplayFilter],
         [DateCreated], [DateModified], [ModifiedBy])
    VALUES
        (@UserFunctionId,
         'CPU',                      -- <-- UPDATE THIS (company ID)
         5,                          -- Scope: 1(GET) + 4(POST)
         NULL,                       -- Filter: NULL = no row-level restriction
         NULL,                       -- DisplayFilter: human-readable version of Filter
         GETDATE(), GETDATE(), SYSTEM_USER);
    PRINT 'Step 1c: Granted tblSmApiUserFunctionComp for company CPU.';
END
ELSE
    PRINT 'Step 1c: tblSmApiUserFunctionComp entry already exists — skipped.';
GO

PRINT '';
PRINT 'Registration complete.';
PRINT 'Proceed to Step 2 — copy OSI.TraverseApi.PurchaseApproval.dll to bin\ApiPlugins\.';
GO
