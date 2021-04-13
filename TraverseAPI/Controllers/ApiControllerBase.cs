#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TRAVERSE.Business;
using TraverseApi.Properties;
#endregion Using Directives

namespace TraverseApi
{
    [ApiAuthorize, ApiCorsPolicy]
    public abstract class ApiControllerBase : ApiController
    {
        #region Constructors
        public ApiControllerBase()
        {
            AddPropertyDelegates();
        }
        #endregion Constructors

        #region Protected  
        protected abstract void AddPropertyDelegates();

        protected IHttpActionResult Ok(object content)
        {
            KeyValuePair<int, List<dynamic>> response = ApiEntityModel.BuildModelList(null, content, ProcessCustomResponse, PageNumber, PageSize);
            return new ApiOkResult(response, this);
        }

        protected void AddWarnings(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || WarningList.Contains(message))
                return;

            WarningList.Add(message);
        }

        protected string GetClientIpAddress() => GetClientIpAddress(null);

        protected string GetClientIpAddress(HttpRequestMessage message)
        {
            var request = message ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)this.Request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }

        protected virtual void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        { }

        protected bool FilterEntityList(IList entityList)
        {
            return Task.Run(() => FilterEntityListAsync(entityList)).Result;
        }

        protected async Task<bool> FilterEntityListAsync(IList entityList)
        {
            var access = Request.GetOwinContext().Get<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage);
            return await FilterList(entityList, access);
        }

        protected bool FilterEntityList(IList entityList, string functionId)
        {
            return Task.Run(() => FilterEntityListAsync(entityList, Guid.Parse(functionId))).Result;
        }

        protected async Task<bool> FilterEntityListAsync(IList entityList, string functionId)
        {
            return await FilterEntityListAsync(entityList, Guid.Parse(functionId));
        }

        protected bool FilterEntityList(IList entityList, Guid functionId)
        {
            var access = Request.GetOwinContext().Get<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage);
            if (access == null)
                return true;

            return Task.Run(() => FilterList(entityList, access.GetChildFunctionDef(functionId))).Result;
        }

        protected async Task<bool> FilterEntityListAsync(IList entityList, Guid functionId)
        {
            var access = Request.GetOwinContext().Get<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage);
            if (access == null)
                return true;

            return await FilterList(entityList, access.GetChildFunctionDef(functionId));
        }

        protected void ValidateEntity(IEntity entity)
        {
            if (entity == null)
                return;

            ValidateEntityList(new List<object>(new[] { entity }));
        }

        protected async Task ValidateEntityAsync(IEntity entity)
        {
            if (entity == null)
                return;

            await ValidateEntityListAsync(new List<object>(new[] { entity }));
        }

        protected void ValidateEntity(IEntity entity, Guid functionId)
        {
            if (entity == null)
                return;

            ValidateEntityList(new List<object>(new[] { entity }), functionId);
        }

        protected async Task ValidateEntityAsync(IEntity entity, Guid functionId)
        {
            if (entity == null)
                return;

            await ValidateEntityListAsync(new List<object>(new[] { entity }), functionId);
        }

        protected void ValidateEntityList(IList entityList)
        {
            var access = Request.GetOwinContext().Get<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage);
            Task.FromResult(ValidateEntityList(entityList, access));
        }

        protected async Task ValidateEntityListAsync(IList entityList)
        {
            var access = Request.GetOwinContext().Get<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage);
            await ValidateEntityList(entityList, access);
        }

        protected void ValidateEntityList(IList entityList, Guid functionId)
        {
            var access = Request.GetOwinContext().Get<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage);
            Task.FromResult(ValidateEntityList(entityList, access?.GetChildFunctionDef(functionId)));
        }

        protected async Task ValidateEntityListAsync(IList entityList, Guid functionId)
        {
            var access = Request.GetOwinContext().Get<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage);
            await ValidateEntityList(entityList, access?.GetChildFunctionDef(functionId));
        }
        #endregion Protected

        #region Private
        private async Task<bool> FilterList(IList entityList, ApiUserFunctionComp access)
        {
            if (access == null)
                return true;

            bool filterApplied = await access.FilterEntityList(entityList);
            foreach (ApiEntitySchema schema in access.EntitySchemaList)
            {
                if (!schema.ChildFunction.HasValue || schema.ChildFunction == Guid.Empty)
                    continue;

                var childAccess = access.GetChildFunctionDef(schema.ChildFunction.Value);
                if (string.IsNullOrWhiteSpace(childAccess.Filter))
                    continue;

                foreach (object entity in entityList)
                {
                    object value = entity.GetType().GetProperty(schema.EntityFieldName)?.GetValue(entity);
                    IList enumerable = value as IList;
                    if (enumerable == null)
                    {
                        if (value == null)
                            enumerable = new List<object>();
                        else
                            enumerable = new List<object> { value };
                    }

                    filterApplied |= await childAccess.FilterEntityList(enumerable);
                }
            }

            return filterApplied;
        }

        private async Task ValidateEntityList(IList entityList, ApiUserFunctionComp access)
        {
            if (entityList == null || entityList.Count == 0)
                return;

            //Validate user's filter is met first
            await ValidateFilterList(entityList, access);

            if (!(typeof(EntityBase).IsAssignableFrom(entityList[0].GetType())))
                return;

            //Validate entity and build error
            StringBuilder builder = new StringBuilder();
            foreach (EntityBase entity in entityList)
            {
                if (!entity.ValidateAll(true))
                {
                    //Entity does not validate but for some reason, no rules are listed as broken; this should be an exception and then we can show more detail
                    if (entity.BrokenRulesList.Count == 0)
                        builder.Append(string.Format("({2}) | {0} {1}", Resources.ApiBrokenRuleMsg, entity.Error, entityList.IndexOf(entity)));
                    else
                    {
                        foreach (var rule in entity.BrokenRulesList)
                        {
                            var schema = access?.EntitySchemaList.Find(s => s.EntityFieldName == rule.Property);
                            if (schema != null)
                                builder.Append(string.Format("{1} : {2} | {0} {3} ", Resources.ApiBrokenRuleMsg, schema.ApiFieldName, entity.GetType().GetProperty(rule.Property).GetValue(entity, null), rule.Description));
                            else
                                builder.Append(string.Format("{1} : {2} | {0} {3} ", Resources.ApiBrokenRuleMsg, rule.Property, entity.GetType().GetProperty(rule.Property).GetValue(entity, null), rule.Description));
                        }
                    }
                }
            }

            //When error found, return information
            if (builder.Length > 0)
                throw new InvalidValueException(builder.ToString());
        }

        private async Task ValidateFilterList(IList entityList, ApiUserFunctionComp access)
        {
            if (!await access.FilterEntityList(entityList))
                throw new InvalidValueException(Resources.ApiEntityValidationError);

            foreach (ApiEntitySchema schema in access.EntitySchemaList)
            {
                if (!schema.ChildFunction.HasValue || schema.ChildFunction == Guid.Empty)
                    continue;

                foreach (object entity in entityList)
                {
                    object value = entity.GetType().GetProperty(schema.EntityFieldName)?.GetValue(entity);
                    IList enumerable = value as IList;
                    if (enumerable == null)
                    {
                        if (value == null)
                            enumerable = new List<object>();
                        else
                            enumerable = new List<object> { value };
                    }

                    var childAccess = access.GetChildFunctionDef(schema.ChildFunction.Value);
                    await ValidateFilterList(enumerable, childAccess);
                }
            }
        }
        #endregion Private

        #region Properties
        protected int PageNumber
        {
            get => Request.GetOwinContext().Get<int>(Resources.ApiStoragePage);
        }

        protected int PageSize
        {
            get => Request.GetOwinContext().Get<int>(Resources.ApiStoragePageSize);
        }

        protected string CompId
        {
            get => Request.GetOwinContext().Get<string>(Resources.ApiStorageCompany);
        }

        internal List<string> WarningList { get; } = new List<string>();
        #endregion Properties
    }
}