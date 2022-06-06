using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using System;
using System.Threading.Tasks;
using TRAVERSE.Business.API;
using TraverseApi.Properties;

namespace TraverseApi
{
    public sealed class TraverseApiRefreshTokenProvider : IAuthenticationTokenProvider
    {
        #region Methods
        public void Create(AuthenticationTokenCreateContext context)
        {
            throw new NotImplementedException();
        }

        public async Task CreateAsync(AuthenticationTokenCreateContext context)
        {
            var user = context.OwinContext.Get<ApiUser>(Resources.ApiUserInfoStorage);
            ApiUserToken token = user.UpdateRefreshToken();
            context.Ticket.Properties.IssuedUtc = token.IssueTime;
            context.Ticket.Properties.ExpiresUtc = token.ExpireTime;
            token.RefreshToken = context.SerializeTicket();
            await token.SaveAsync();

            context.SetToken(token.TokenId);
        }

        public void Receive(AuthenticationTokenReceiveContext context)
        {
            throw new NotImplementedException();
        }

        public async Task ReceiveAsync(AuthenticationTokenReceiveContext context)
        {
            context.OwinContext.Response.Headers.Add(Resources.ApiOriginMsg, new[] { "*" });
            var user = context.OwinContext.Get<ApiUser>(Resources.ApiUserInfoStorage);
            await Task.Run(() =>
            {
                if (user.TokenInfo.RefreshToken.Equals(context.Token, StringComparison.Ordinal))
                {
                    var token = user.TokenInfo;
                    var properties = new AuthenticationProperties(context.Ticket.Properties.Dictionary)
                    {
                        IssuedUtc = token.IssueTime,
                        ExpiresUtc = token.ExpireTime
                    };
                    var ticket = new AuthenticationTicket(context.Ticket.Identity, properties);
                    context.SetTicket(ticket);
                }
            });
        }
        #endregion Methods
    }
}