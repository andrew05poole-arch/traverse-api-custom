using OSI.TraverseApi.Business;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using TRAVERSE.Business;
using TraverseApi.Properties;

namespace TraverseApi
{
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        #region Constructors
        /// <summary>
        /// Authorization attribute. This attribute requires the request to be authorized using the Traverse Api methodology
        /// </summary>
        public ApiAuthorizeAttribute()
            : base()
        { }

        /// <summary>
        /// Authorization attribute. This attribute requires the request to be authorized using the Traverse Api methodology
        /// </summary>
        /// <param name="disableAuthorization">A private option to disable validating the user. This is only for designated system calls that are open to the public</param>
        internal ApiAuthorizeAttribute(bool publiclyAccessible)
            : base()
        {
            _disableAuthCheck = publiclyAccessible;
        }
        #endregion Constructors

        #region Methods
        private async Task<ApiUser> LoadTokenizedUser(HttpRequestMessage request)
        {
            if (request == null)
                return null;

            //check to see if the expiration date for this login method has surpassed
            if (DateTimeOffset.UtcNow >= ApiUtility.OAuthRequireDate || request.Headers.Contains(Resources.ApiClientIdHeader))
            {
                //Load the client/token credentials from the header
                string clientId = request.Headers.FirstOrDefault(x => x.Key.Equals(Resources.ApiClientIdHeader, StringComparison.OrdinalIgnoreCase)).Value.FirstOrDefault();
                string token = request.Headers.FirstOrDefault(x => x.Key.Equals(Resources.ApiTokenHeader, StringComparison.OrdinalIgnoreCase)).Value.FirstOrDefault();

                //return validated user
                return await ApiUserProvider.ValidateClientAsync(TravApiConfig.ApiDatabase, clientId, token);
            }
            return null;
        }

        private async Task<ApiUser> LoadOAuthUser(HttpRequestMessage request)
        {
            if (request == null)
                return null;

            string id = request.GetOwinContext().Authentication.User.Identity.Name;
            return await Task.Run<ApiUser>(() => EntityProvider.GetEntity<ApiUser, ApiUserProvider>(new string[] { id }, TravApiConfig.ApiDatabase, null));
        }

        private async Task<string> GetProcessSettings(HttpActionContext context)
        {
            string companyId = null;
            string query = null;
            int page = 0;
            int page_size = 50;

            await Task.Run(() =>
            {
                //Try to load settings for company, paging and queries from query string provided in request
                companyId = context.Request.GetQueryNameValuePairs()?.FirstOrDefault(q => q.Key.ToLower() == Resources.ApiRequestCompany).Value;
                int.TryParse(context.Request.GetQueryNameValuePairs()?.FirstOrDefault(q => q.Key.ToLower() == Resources.ApiRequestPage).Value, out page);
                int.TryParse(context.Request.GetQueryNameValuePairs()?.FirstOrDefault(q => q.Key.ToLower() == Resources.ApiRequestPageSize).Value, out page_size);
                query = context.Request.GetQueryNameValuePairs()?.FirstOrDefault(q => q.Key.ToLower() == Resources.ApiRequestQuery).Value;

                //If not company ID, try to find the company ID in the header collection; take the first one found
                if (string.IsNullOrWhiteSpace(companyId))
                {
                    companyId = context.Request.Headers?.FirstOrDefault(h => h.Key.ToLower() == Resources.ApiRequestCompany).Value?.FirstOrDefault();
                }

                //Store the request settings in the Owin Context for use later
                context.Request.GetOwinContext().Set<string>(Resources.ApiStorageCompany, companyId);
                context.Request.GetOwinContext().Set<string>(Resources.ApiStorageQuery, query);
                context.Request.GetOwinContext().Set<int>(Resources.ApiStoragePage, page);
                context.Request.GetOwinContext().Set<int>(Resources.ApiStoragePageSize, page_size);
            });

            return companyId;
        }

        private bool ValidateUserAccess(ApiUser user, Guid functionId, string companyId, string method, ref ApiUserFunctionComp userAccess)
        {
            //Verify that we have a user; the user is not expired and a company is provided
            if (user != null && (user.ExpirationDate.GetValueOrDefault(DateTime.Now) - DateTime.Now).Days >= 0 && !string.IsNullOrWhiteSpace(companyId))
            {
                //make sure that user has access to the function and that access has not expired and they can perform the function in the specified company
                userAccess = user.FunctionList.FirstOrDefault(f => f.FunctionId == functionId && (f.AccessExpireDate.GetValueOrDefault(DateTime.Now) - DateTime.Now).Days >= 0)?.CompanyList.FirstOrDefault(c => c.CompanyId.ToLower() == companyId.ToLower());

                if (userAccess != null)
                {
                    //return that the user also has permission to perform the requested access; User can always retrieve schema if they have function access
                    switch (method.ToLower())
                    {
                        case "get":
                        case "head":
                            return userAccess.AllowRead;
                        case "put":
                            return userAccess.AllowEdit;
                        case "post":
                            return userAccess.AllowNew;
                        case "delete":
                            return userAccess.AllowDelete;
                        case "schema":
                            return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> ValidateMessage(HttpRequestMessage request, ApiUser user)
        {
            if (ApiUtility.ApiConfig.UseMsgSecurity.GetValueOrDefault())
            {
                if (!request.Headers.Contains(Resources.ApiSignatureHeaderKey))
                    return false;

                byte[] hash = null;
                HashAlgorithm hashProvider = null;

                string encryptedMsg = request.Headers.GetValues(Resources.ApiSignatureHeaderKey).First();
                string uri = request.RequestUri.ToString();
                string message = await request.Content.ReadAsStringAsync();
                string key = string.IsNullOrWhiteSpace(ApiUtility.ApiConfig.MsgSharedKey) ? user.ClientId.ToString() : ApiUtility.ApiConfig.MsgSharedKey;

                var bytes = Encoding.UTF8.GetBytes(uri + (message ?? string.Empty) + key);

                switch (ApiUtility.ApiConfig.MsgEncryption)
                {
                    case 0:
                        hashProvider = SHA256.Create();
                        break;
                    case 1:
                        hashProvider = SHA384.Create();
                        break;
                    case 2:
                        hashProvider = SHA512.Create();
                        break;
                    case 3:
                        hashProvider = SHA256Cng.Create();
                        break;
                    case 4:
                        hashProvider = SHA384Cng.Create();
                        break;
                    case 5:
                        hashProvider = SHA512Cng.Create();
                        break;
                }

                hash = hashProvider.ComputeHash(bytes);

                var computedMessage = Convert.ToBase64String(hash);
                return string.Equals(encryptedMsg, computedMessage);
            }

            return true;
        }
        #endregion Methods

        #region Overrides
        public override async Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            object routeToken;
            ApiRouteAttribute routeInfo = null;
            ApiUser user = null;
            ApiUserFunctionComp action = null;
            HttpRequestMessage request = actionContext.Request;

            //Check to see if we have a ApiRoute info
            var dataTokens = request.GetRouteData().Route.DataTokens;
            if (dataTokens != null && dataTokens.TryGetValue(Resources.ApiFunctionDataToken, out routeToken))
                routeInfo = routeToken as ApiRouteAttribute;

            //If custom route attribute exists but has no Function Id, return 404 Error
            if (routeInfo != null && (routeInfo.FunctionId.Equals(Guid.Empty) && !routeInfo.SkipUserAccessValidation))
            {
                actionContext.Response = request.CreateResponse(HttpStatusCode.NotFound);
                return;
            }

            //Load settings and company Id
            string companyId = await GetProcessSettings(actionContext);

            //Validate user if this is not a public call
            if (!_disableAuthCheck)
            {
                //Check to see if we are using the deprecated static client/token method
                if (request.Headers.Contains(Resources.ApiClientIdHeader))
                {
                    user = await LoadTokenizedUser(request);

                    request.GetOwinContext().Set<bool>(Resources.ApiSecurityNoticeSetting, true);
                    request.GetOwinContext().Set<ApiUser>(Resources.ApiUserInfoStorage, user);
                }
                else
                {
                    //Use OAuth to validate user
                    await base.OnAuthorizationAsync(actionContext, cancellationToken);
                    //Load user using ID stored on authorized user; store in request for use later
                    request.GetOwinContext().Set<ApiUser>(Resources.ApiUserInfoStorage, user = await LoadOAuthUser(request));
                    request.GetOwinContext().Set<bool>(Resources.ApiSecurityNoticeSetting, false);
                }

                if (routeInfo != null &&    //we have route info, so going through the custom route and security steps
                    !routeInfo.SkipUserAccessValidation &&    //access validation will be verified
                    !ValidateUserAccess(user, routeInfo.FunctionId, companyId, request.Method.Method, ref action))  //Validate user has access to method and action
                {
                    actionContext.Response = request.CreateResponse(HttpStatusCode.Unauthorized);
                    return;
                }

                //Check to see if this is an administrator function call for support or otherwise, if so, check if user is an administrator
                if (routeInfo != null && routeInfo.AdminFunction && user.RoleType != ApiUserRoleType.Administrator)
                {
                    actionContext.Response = request.CreateResponse(HttpStatusCode.Unauthorized);
                    return;
                }

                //Check to see if the application is valid for the user license
                if (action != null && action.Parent != null && action.Parent.FunctionInfo != null && !TRAVERSE.Core.ApplicationContext.IsFeatureValid(action.Parent.FunctionInfo.AppId, companyId))
                {
                    actionContext.Response = request.CreateResponse(HttpStatusCode.Unauthorized);
                    return;
                }

                if (!await ValidateMessage(request, user))
                {
                    actionContext.Response = request.CreateResponse<string>(HttpStatusCode.Unauthorized, Resources.ApiInvalidMessage);
                    return;
                }
            }

            request.GetOwinContext().Set<bool>(Resources.ApiIgnoreUserAccessValidation, routeInfo != null && routeInfo.SkipUserAccessValidation);

            //Store our User Access record for this request so that we do not have to look it up again
            request.GetOwinContext().Set<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage, action);

            //Store request body message in the event of an error
            request.GetOwinContext().Set<string>(Resources.ApiBodyContent, await request.Content.ReadAsStringAsync());

            //Update User Function History
            if (action != null)
                await action.Parent.UpdateRequestInfo(request.Method.Method);
        }
        #endregion Overrides

        #region Fields
        private bool _disableAuthCheck;
        #endregion Fields
    }
}