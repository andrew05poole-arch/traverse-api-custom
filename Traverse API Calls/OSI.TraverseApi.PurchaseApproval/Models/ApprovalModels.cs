namespace TRAVERSE.Web.API.PurchaseApproval.Models
{
    /// <summary>
    /// Request body for POST /api/v2/po/approval/{transId}.
    /// </summary>
    public class ApprovalActionBody
    {
        /// <summary>
        /// The decision: "approve" or "decline".
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Optional free-text comment recorded against the decision.
        /// For Engage-originated approvals this should include the Entra identity,
        /// e.g. "Approved via TRAVERSE Engage by manager@contoso.com".
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// Approval route level being responded to (0-based).
        /// Defaults to 0 (first level) when omitted.
        /// Reserved for Phase 2 — multi-level routing via RequestResponseProcess.
        /// </summary>
        public int? Level { get; set; }
    }

    /// <summary>
    /// Response body for the approval action.
    /// </summary>
    public class ApprovalActionResult
    {
        public string TransId     { get; set; }
        public string Status      { get; set; }   // "Approved" | "Declined" | error description
        public bool   Success     { get; set; }
        public string ApprovedBy  { get; set; }
        public string Message     { get; set; }
    }
}
