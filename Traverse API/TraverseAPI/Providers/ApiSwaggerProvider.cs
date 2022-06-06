using Swagger.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using TRAVERSE.Business.API;
using TraverseApi.Properties;

namespace TraverseApi
{
    public sealed class ApiSwaggerProvider : ISwaggerProvider
    {
        #region Constructors
        public ApiSwaggerProvider(ISwaggerProvider generator)
        {
            this._generator = generator;
        }
        #endregion Constructors

        #region Methods
        private Dictionary<string, ApiUserFunctionComp> GenerateApiDescriptions(IApiExplorer explorer)
        {
            ApiUserFunction function = null;
            Guid? functionId = null;

            Dictionary<string, ApiUserFunctionComp> exceptionList = new Dictionary<string, ApiUserFunctionComp>();
            ApiUser user = HttpContext.Current.User as ApiUser;

            foreach (var apiDescription in explorer.ApiDescriptions)
            {
                ApiUserFunctionComp functionInfo = null;
                string path = string.Format("/{0}", apiDescription.Route.RouteTemplate);
                string name = apiDescription.ControllerName();
                string method = apiDescription.HttpMethod.Method.ToLower();

                if (path.EndsWith("/"))
                    path = path.Substring(0, path.Length - 1);

                if (apiDescription.Route.DataTokens != null && apiDescription.Route.DataTokens.ContainsKey(Resources.ApiFunctionDataToken))
                    functionId = ((ApiRouteAttribute)apiDescription.Route.DataTokens[Resources.ApiFunctionDataToken]).FunctionId;

                if (functionId.HasValue && functionId != Guid.Empty)
                {
                    if (!TagsToRemove.Contains(name))
                        TagsToRemove.Add(name);

                    if (user != null)
                    {
                        foreach (ApiUserFunction func in user.FunctionList)
                        {
                            if (func.FunctionId != functionId || func.CompanyList.Count == 0)
                                continue;

                            foreach (ApiUserFunctionComp comp in func.CompanyList)
                            {
                                if (!TRAVERSE.Core.ApplicationContext.IsFeatureValid(func.FunctionInfo.AppId, comp.CompanyId))
                                    continue;

                                functionInfo = comp;
                                break;
                            }

                            if (functionInfo == null)
                                continue;

                            function = func;
                            break;
                        }
                    }
                }

                if (function == null ||   //Function not found
                        functionInfo == null ||  //No access to the company for this function
                        !(  //Check that the user has access and the function supports that access
                            (functionInfo.AllowNew && function.FunctionInfo.AllowNew && method == "post") ||
                            (functionInfo.AllowRead && function.FunctionInfo.AllowRead && method == "get") ||
                            (functionInfo.AllowEdit && function.FunctionInfo.AllowEdit && method == "put") ||
                            (functionInfo.AllowDelete && function.FunctionInfo.AllowDelete && method == "delete")
                        ) ||
                        functionInfo.Parent.AccessExpireDate.GetValueOrDefault(DateTime.Today.AddDays(1)) < DateTime.Today  //Access is expired
                    )
                {
                    if (!exceptionList.ContainsKey(path))
                        exceptionList.Add(path, functionInfo);
                }
            }

            return exceptionList;
        }
        #endregion Methods

        #region Overrides
        public SwaggerDocument GetSwagger(string rootUrl, string apiVersion)
        {
            IApiExplorer explorer = WebApiApplication.Configuration.Services.GetApiExplorer();
            var items = this.GenerateApiDescriptions(explorer);

            SwaggerDocument doc = _generator.GetSwagger(rootUrl, apiVersion);

            foreach (string tag in TagsToRemove)
            {
                var tagItem = doc.tags.FirstOrDefault(t => t.name == tag);
                if (tagItem != null)
                    doc.tags.Remove(tagItem);
            }

            foreach (KeyValuePair<string, ApiUserFunctionComp> pair in items)
            {
                foreach (var path in doc.paths.Where(p => p.Key.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!string.IsNullOrEmpty(path.Key))
                    {
                        if (path.Value.get != null && (pair.Value == null || !pair.Value.AllowRead))
                            path.Value.get = null;
                        if (path.Value.put != null && (pair.Value == null || !pair.Value.AllowEdit))
                            path.Value.put = null;
                        if (path.Value.post != null && (pair.Value == null || !pair.Value.AllowNew))
                            path.Value.post = null;
                        if (path.Value.delete != null && (pair.Value == null || !pair.Value.AllowDelete))
                            path.Value.delete = null;
                    }
                }
            }

            //clear title and version information
            doc.info.title = string.Empty;
            doc.info.version = string.Empty;

            //Sort paths to display properly
            doc.paths = doc.paths.OrderBy(e => e.Key).ToList().ToDictionary(e => e.Key, e => e.Value);

            return doc;
        }
        #endregion Overrides

        #region Properties
        private List<string> TagsToRemove { get; } = new List<string>();
        #endregion Properties

        #region Fields
        private ISwaggerProvider _generator;
        #endregion Fields
    }
}