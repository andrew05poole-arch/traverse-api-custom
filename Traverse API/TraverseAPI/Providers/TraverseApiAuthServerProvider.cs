#region Using Directives
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TRAVERSE.Business.API;
using TRAVERSE.Core;
using TRAVERSE.Web.API.Properties;
#endregion Using Directives

namespace TRAVERSE.Web.API
{
    public sealed class TraverseApiAuthServerProvider : OAuthAuthorizationServerProvider
    {
        #region Overrides
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            try
            {
                string clientId;
                string clientSecret;

                //Validate our client id credentials
                if ((context.TryGetFormCredentials(out clientId, out clientSecret) || context.TryGetBasicCredentials(out clientId, out clientSecret)))
                {
                    var user = await ApiUserProvider.ValidateClientAsync(TravApiConfig.ApiDatabase, clientId, clientSecret);
                    if (user == null)
                    {
                        context.SetError(Resources.ApiInvalidGrant, Resources.ApiInvalidCredential);
                        context.Rejected();
                        return;
                    }
                    context.OwinContext.Set<ApiUser>(Resources.ApiUserInfoStorage, user);
                    context.OwinContext.Set<string>(Resources.ApiAuthClientId, clientId);
                    context.Validated(clientId);
                    return;
                }
                else
                {
                    context.SetError(Resources.ApiInvalidGrant, Resources.ApiInvalidCredential);
                    context.Rejected();
                }
            }
            catch (Exception ex)
            {
                await Task.FromResult(ApiErrorHandler.ProcessError(ex));
            }
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            try
            {
                var user = context.OwinContext.Get<ApiUser>(Resources.ApiUserInfoStorage);
                context.OwinContext.Response.Headers.Add(Resources.ApiOriginMsg, new[] { "*" });

                await Task.Run(() =>
                {
                    if (user == null ||
                        !user.EmailAddress.Equals(context.UserName, StringComparison.OrdinalIgnoreCase) ||
                        !user.Password.Equals(DataSecurity.EncryptData(context.Password), StringComparison.Ordinal))
                    {
                        context.SetError(Resources.ApiInvalidGrant, Resources.ApiInvalidCredential);
                        context.Rejected();
                        return;
                    }

                    if (user.UserStatus != ApiUserStatus.Active)
                    {
                        context.SetError(Resources.ApiInvalidGrant, Resources.ApiInvalidStatus);
                        context.Rejected();
                        return;
                    }
                });

                var identity = new ClaimsIdentity(user,
                    new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Name ?? user.EmailAddress),
                        new Claim(ClaimTypes.Role, user.RoleType.ToString()),
                        new Claim(ClaimTypes.Sid, user.Id.ToString())
                    },
                    context.Options.AuthenticationType,
                    context.Options.AuthenticationType,
                    context.Options.AuthenticationType);

                var authProperties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    { Resources.ApiAuthClientId, context.ClientId }
                });

                var ticket = new AuthenticationTicket(identity, authProperties);

                context.Validated(ticket);
            }
            catch (Exception ex)
            {
                await Task.FromResult(ApiErrorHandler.ProcessError(ex));
            }
        }

        public override async Task GrantClientCredentials(OAuthGrantClientCredentialsContext context)
        {
            try
            {
                var user = context.OwinContext.Get<ApiUser>(Resources.ApiUserInfoStorage);
                context.OwinContext.Response.Headers.Add(Resources.ApiOriginMsg, new[] { "*" });

                var identity = new ClaimsIdentity(user,
                    new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Name ?? user.EmailAddress),
                        new Claim(ClaimTypes.Role, user.RoleType.ToString()),
                        new Claim(ClaimTypes.Sid, user.Id.ToString())
                    },
                    context.Options.AuthenticationType,
                    context.Options.AuthenticationType,
                    context.Options.AuthenticationType);

                var authProperties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    { Resources.ApiAuthClientId, context.ClientId }
                });

                await Task.Run(() =>
                {
                    var ticket = new AuthenticationTicket(identity, authProperties);
                    context.Validated(ticket);
                });
            }
            catch (Exception ex)
            {
                await Task.FromResult(ApiErrorHandler.ProcessError(ex));
            }
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            try
            {
                foreach (var property in context.Properties.Dictionary)
                    context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }
            catch (Exception ex)
            {
                Task.FromResult(ApiErrorHandler.ProcessError(ex));
            }

            return Task.FromResult<object>(null);
        }

        public override Task GrantRefreshToken(OAuthGrantRefreshTokenContext context)
        {
            try
            {
                string clientId;
                context.Ticket.Properties.Dictionary.TryGetValue(Resources.ApiAuthClientId, out clientId);

                if (clientId != context.ClientId)
                {
                    context.SetError(Resources.ApiInvalidRefresh, "Client ID is not valid.");
                    return Task.FromResult<object>(null);
                }

                var identity = new ClaimsIdentity(context.Ticket.Identity);
                var ticket = new AuthenticationTicket(identity, context.Ticket.Properties);

                context.Validated(ticket);
            }
            catch (Exception ex)
            {
                Task.FromResult(ApiErrorHandler.ProcessError(ex));
            }

            return Task.FromResult<object>(null);
        }
        #endregion Overrides
    }
}