-- ============================================================
-- TRAVERSE API Custom — Tier 4: PO Approval
-- Register-PoApproval.sql
--
-- Run against the TRAVERSE SYS database before deploying
-- OSI.TraverseApi.PurchaseApproval.dll.
--
-- Idempotent: safe to re-run (IF NOT EXISTS guards on all steps).
--
-- Before running:
--   Replace 'tradmin' with your API service account username.
--   Replace 'CPU'     with your TRAVERSE company ID.
--   Repeat step 1c for every additional company as needed.
-- ============================================================

USE [SYS];
GO

-- ── Step 1a: Register the API function ───────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[tblSmApiFunction]
    WHERE  [FunctionId] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
)
BEGIN
    INSERT INTO [dbo].[tblSmApiFunction]
        ([FunctionId], [FunctionName], [Description],
         [AllowRead], [AllowNew], [AllowEdit], [AllowDelete])
    VALUES
        ('5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F',
         'po/approval',
         'PO Approval — approve or decline a Purchase Order Request via the TRAVERSE Engage workflow',
         1,   -- AllowRead  (GET  /api/v2/po/approval/{transId})
         1,   -- AllowNew   (POST /api/v2/po/approval/{transId})
         0,   -- AllowEdit  (PUT  — not used)
         0);  -- AllowDelete (DELETE — not used)
    PRINT 'Step 1a: Registered tblSmApiFunction for PO Approval.';
END
ELSE
    PRINT 'Step 1a: tblSmApiFunction entry already exists — skipped.';
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
    WHERE  [UserId]     = @UserId
      AND  [FunctionId] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
)
BEGIN
    INSERT INTO [dbo].[tblSmApiUserFunction]
        ([UserId], [FunctionId], [AllowRead], [AllowNew], [AllowEdit], [AllowDelete])
    VALUES
        (@UserId, '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F', 1, 1, 0, 0);
    PRINT 'Step 1b: Granted tblSmApiUserFunction.';
END
ELSE
    PRINT 'Step 1b: tblSmApiUserFunction entry already exists — skipped.';
GO

-- ── Step 1c: Grant per company ────────────────────────────────────────────────
-- !! Change 'tradmin' and 'CPU' to match your environment !!
-- Repeat this block for every additional company that needs access.
DECLARE @UserId BIGINT = (
    SELECT [ID] FROM [dbo].[tblSmApiUser]
    WHERE  [UserId] = 'tradmin'    -- <-- UPDATE THIS
);

IF @UserId IS NULL
BEGIN
    RAISERROR('Step 1c ERROR: API user not found. Update the WHERE clause with the correct UserId from tblSmApiUser.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[tblSmApiUserFunctionComp]
    WHERE  [UserId]     = @UserId
      AND  [FunctionId] = '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F'
      AND  [CompId]     = 'CPU'    -- <-- UPDATE THIS
)
BEGIN
    INSERT INTO [dbo].[tblSmApiUserFunctionComp]
        ([UserId], [FunctionId], [CompId], [AllowRead], [AllowNew], [AllowEdit], [AllowDelete])
    VALUES
        (@UserId, '5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F',
         'CPU',   -- <-- UPDATE THIS
         1, 1, 0, 0);
    PRINT 'Step 1c: Granted tblSmApiUserFunctionComp for company CPU.';
END
ELSE
    PRINT 'Step 1c: tblSmApiUserFunctionComp entry already exists — skipped.';
GO

PRINT '';
PRINT 'Registration complete. Proceed to Step 2 — copy DLL to ApiPlugins\.';
GO
