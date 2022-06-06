#region Using Directives
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using TRAVERSE.Business.API;
using TraverseApi.Properties;
#endregion Using Directives

namespace TraverseApi
{
    internal sealed class ApiOkResult : OkNegotiatedContentResult<List<dynamic>>
    {
        #region Constructors
        public ApiOkResult(KeyValuePair<int, List<dynamic>> content, ApiController controller)
            : base(content.Value, controller)
        {
            AvailableRecords = content.Key;
            RecordCount = content.Value.Count;
            WarningList = (controller as ApiControllerBase)?.WarningList ?? new List<string>();
        }

        public ApiOkResult(KeyValuePair<int, List<dynamic>> content, IContentNegotiator contentNegotiator, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
            : base(content.Value, contentNegotiator, request, formatters)
        {
            AvailableRecords = content.Key;
            RecordCount = content.Value.Count;
            WarningList = new List<string>();
        }
        #endregion Constructors

        #region Overrides
        public override async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.ExecuteAsync(cancellationToken);

            response.Headers.Add(Resources.ApiResponseRecordAvail, AvailableRecords.ToString());
            response.Headers.Add(Resources.ApiResponseRecordCount, RecordCount.ToString());

            if (Request.GetOwinContext().Get<bool>(Resources.ApiSecurityNoticeSetting))
                response.Headers.Add(Resources.ApiOAuthReqDate, string.Format(Resources.ApiOAuthReqDate, ApiUtility.OAuthRequireDate));

            foreach (string message in WarningList)
            {
                response.Headers.Add(Resources.ApiProcessingDetail, message);
            }

            return response;
        }
        #endregion Overrides

        #region Properties
        private List<string> WarningList { get; set; }

        private int AvailableRecords { get; set; }

        private int RecordCount { get; set; }
        #endregion Properties
    }
}