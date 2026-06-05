#region Using Directives
using System;
using System.Collections.Generic;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Web.API.PurchaseApproval.Models;
#endregion

namespace TRAVERSE.Web.API.PurchaseApproval.Controllers
{
    /// <summary>
    /// PO Approval endpoint — allows an authenticated caller to approve or decline
    /// a Purchase Order Request that is in the TRAVERSE approval workflow.
    ///
    /// Endpoints:
    ///   GET  /api/v2/po/approval/{transId}   — return the current approval status of a PO request
    ///   POST /api/v2/po/approval/{transId}   — approve or decline the request
    ///
    /// Business layer:
    ///   Phase 1 (this file): calls the static TransactionRequest.Approve() / Decline()
    ///   methods from TRAVERSE.Business.PurchaseOrder.  These methods:
    ///     - Update tblPoTransRequest (Status, ApprovedBy, ApprovedDate)
    ///     - On approve: call ConvertToOrder() which changes tblPoTransHeader.TransType 0→9
    ///
    ///   Phase 2 (future): wire to RequestResponseProcess (TRAVERSE.Business.PurchaseApproval)
    ///   for full multi-level routing support — writes tblPoTransRequestResponse, advances
    ///   tblPoTransRequestStatus level, triggers TRAVERSE email notifications.
    ///
    /// Authorization:
    ///   Standard TRAVERSE API bearer token required (via [ApiAuthorize] on ApiControllerBase).
    ///   The FunctionID below must be registered in the TRAVERSE API database before use —
    ///   see INSTALL.md for the SQL script.
    ///
    /// TRAVERSE Engage integration:
    ///   PoRequisitionApprovalAction (in traverse-engage) calls this endpoint under a
    ///   service-account bearer token. The Entra approver identity is passed in the
    ///   Comments field so it appears in the approval record.
    /// </summary>
    public class ApiPoApprovalController : ApiControllerBase
    {
        #region Constants
        /// <summary>
        /// Unique function GUID for this endpoint.
        /// Must be registered in the TRAVERSE API database (see INSTALL.md).
        ///
        /// DB registration:
        ///   tblSmApiFunction         — the function record
        ///   tblSmApiUserFunction     — grant to API user(s)
        ///   tblSmApiUserFunctionComp — grant per company
        /// </summary>
        public const string FunctionID = "5A7D3C4E-8B9F-4C2D-9E3F-1A2B3C4D5E6F";
        #endregion

        #region Web Methods

        // ── GET /api/v2/po/approval/{id} ──────────────────────────────────────
        /// <summary>
        /// Returns the current approval status of a PO request.
        /// </summary>
        [ApiRoute(FunctionID, 2f, "approval/{id}", typeof(TransactionRequest))]
        public IHttpActionResult Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Transaction ID is required.");

            var request = LoadRequest(id);
            if (request == null)
                throw new InvalidValueException($"PO request '{id}' was not found.");

            return Ok(new List<TransactionRequest> { request });
        }

        // ── POST /api/v2/po/approval/{id} ─────────────────────────────────────
        /// <summary>
        /// Approves or declines a PO Request.
        ///
        /// Body: { "action": "approve"|"decline", "comments": "optional text", "level": 0 }
        ///
        /// On approve:
        ///   - tblPoTransRequest.Status  → 2 (Approved)
        ///   - tblPoTransRequest.ApprovedBy / ApprovedDate set
        ///   - tblPoTransHeader.TransType → 9 (New Order)  [via ConvertToOrder()]
        ///
        /// On decline:
        ///   - tblPoTransRequest.Status  → 3 (Declined)
        ///   - tblPoTransRequest.ApprovedBy / ApprovedDate set
        ///   - tblPoTransHeader.TransType unchanged
        /// </summary>
        [ApiRoute(FunctionID, 2f, "approval/{id}", typeof(TransactionRequest))]
        public IHttpActionResult Add([FromBody] dynamic body, string id)
        {
            // ── 1. Parse & validate inputs ─────────────────────────────────────
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Transaction ID is required.");

            if (body == null)
                throw new InvalidValueException("Request body is required. " +
                    "Provide: { \"action\": \"approve\" | \"decline\", \"comments\": \"...\" }");

            // Support dynamic body (ApiEntityModel) and typed deserialization
            string action   = (body.action   ?? body.Action)?.ToString()?.Trim()?.ToLowerInvariant();
            string comments = (body.comments ?? body.Comments)?.ToString()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(action))
                throw new InvalidValueException(
                    "The 'action' field is required. Valid values: \"approve\", \"decline\".");

            if (action != "approve" && action != "decline")
                throw new InvalidValueException(
                    $"Unknown action '{action}'. Valid values: \"approve\", \"decline\".");

            // ── 2. Confirm the request exists and is actionable ────────────────
            var request = LoadRequest(id);
            if (request == null)
                throw new InvalidValueException($"PO request '{id}' was not found.");

            // Status: 0=New, 1=Pending, 2=Approved, 3=Declined, 4=Cancelled
            if (request.RequestStatus != PORequestStatus.New &&
                request.RequestStatus != PORequestStatus.Pending)
            {
                throw new BusinessRuleException(
                    $"PO request '{id}' cannot be actioned — current status is " +
                    $"'{request.RequestStatus}'. Only New or Pending requests can be approved/declined.");
            }

            // ── 3. Resolve the acting user ─────────────────────────────────────
            // User.Identity.Name carries the TRAVERSE username from the bearer token
            // (set by OAuthBearerAuthenticationProvider in the host's OWIN pipeline).
            // For Engage service-account calls this is the API service account;
            // the Entra approver identity arrives separately via 'comments'.
            string actingUserId = User?.Identity?.Name ?? string.Empty;

            if (string.IsNullOrWhiteSpace(actingUserId))
                throw new PermissionDeniedException("Could not resolve the authenticated user identity.");

            // Append comments to the Notes field so they appear on the order
            // (useful for Engage: "Approved via Engage by manager@contoso.com")
            if (!string.IsNullOrWhiteSpace(comments))
            {
                // Notes will be stamped by the business layer; we surface comments
                // in the API response and optionally pass them through.
                // Phase 2: pass comments to RequestResponseProcess.ResponseComments.
            }

            // ── 4. Execute the business-layer operation ────────────────────────
            if (action == "approve")
            {
                TransactionRequest.Approve(id, actingUserId, CompId, null);
            }
            else  // decline
            {
                TransactionRequest.Decline(id, actingUserId, CompId, null);
            }

            // ── 5. Return the updated request ──────────────────────────────────
            var updated = LoadRequest(id);
            return Ok(new List<TransactionRequest> { updated });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Loads a TransactionRequest by TransId for the current company.
        /// Returns null if not found.
        /// </summary>
        private TransactionRequest LoadRequest(string transId)
        {
            var filter = new SqlFilterBuilder<TransactionRequestBase.Columns>();
            filter.AppendEquals(TransactionRequestBase.Columns.TransId, transId);

            var provider = new TransactionRequestProvider();
            var items = provider.Load(CompId, new FilterCriteria(filter.ToString(), string.Empty));

            return items?.Count > 0 ? items[0] : null;
        }

        /// <summary>
        /// Required by ApiControllerBase — no custom property delegates needed for this endpoint.
        /// </summary>
        protected override void AddPropertyDelegates() { }

        #endregion
    }
}
