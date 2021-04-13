using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http.Cors;

namespace TraverseApi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ApiCorsPolicyAttribute : Attribute, ICorsPolicyProvider
    {
        #region Constructors
        public ApiCorsPolicyAttribute()
        { }
        #endregion Constructors

        #region Methods
        public async Task<CorsPolicy> GetCorsPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //Check if we have a policy that is already defined
            if (_policy == null)
            {
                //run asynchronously to make the compiler happy
                await Task.Run(() =>
                {
                    //Create the general policy
                    _policy = new CorsPolicy()
                    {
                        AllowAnyHeader = true,
                        AllowAnyMethod = true
                    };

                    //If we do not have any origin urls defined in the config, allow all; otherwise limit to the user-specified list
                    if (TravApiConfig.CorsOriginList?.Count == 0)
                        _policy.Origins.Add("*");
                    else
                        TravApiConfig.CorsOriginList.ForEach(item => _policy.Origins.Add(item.Trim()));
                });
            }
            return _policy;
        }
        #endregion Methods

        #region Fields
        private CorsPolicy _policy;
        #endregion Fields
    }
}