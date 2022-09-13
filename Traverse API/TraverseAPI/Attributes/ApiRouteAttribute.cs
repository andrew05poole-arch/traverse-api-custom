#region Using Directives
using System;
using System.Collections.Generic;
using System.Web.Http.Routing;
using System.Web.Routing;
using TRAVERSE.Business.API;
using TRAVERSE.Web.API.Properties;
#endregion Using Directives

namespace TRAVERSE.Web.API
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class ApiRouteAttribute : Attribute, IDirectRouteFactory
    {
        #region Fields
        private ApiFunctionHeader _functionInfo;
        #endregion Fields

        #region Constructors
        //Do not allow user to create attribute with default constructor
        private ApiRouteAttribute()
        { }

        /// <summary>
        /// Route Template handler for Traverse API
        /// </summary>
        /// <param name="functionId">Guid for the function. This will be used during the authorization process to see if the consumer has access to this function</param>
        /// <param name="revision">The revision is used to specify version information. This number should be greater than zero. Major versions or whole numbers are provided by OSAS development. For any custom projects this should be a decimal value with the included major version. For example, 2.19087 which would represent major version 2 and custom version 19087.</param>
        /// <param name="template">The typical route template as used by the standard Route Attribute. It is not necessary to supply /api. The /api will be added by default along with the application id.</param>
        /// <param name="typeForDocumentation">This would be the object type to use for documentation purposes. When this is an IEntity object or an object whose properties have a DataObject tag on them, field sizes will be extracted for the documentation as well</param>
        public ApiRouteAttribute(string functionId, float revision, string template, Type typeForDocumentation)
        {
            Guid funcId = Guid.Empty;
            Guid.TryParse(functionId, out funcId);
            FunctionId = funcId;
            Template = template;
            Revision = (revision < 0 ? -1 : 1) * revision;

            DocumentType = typeForDocumentation;
        }

        /// <summary>
        /// Route Template handler for Traverse API
        /// </summary>
        /// <param name="functionId">Guid for the function. This will be used during the authorization process to see if the consumer has access to this function</param>
        /// <param name="revision">The revision is used to specify version information. This number should be greater than zero. Major versions or whole numbers are provided by OSAS development. For any custom projects this should be a decimal value with the included major version. For example, 2.19087 which would represent major version 2 and custom version 19087.</param>
        /// <param name="template">The typical route template as used by the standard Route Attribute. It is not necessary to supply /api. The /api will be added by default along with the application id.</param>
        /// <param name="typeForDocumentation">This would be the object type to use for documentation purposes. When this is an IEntity object or an object whose properties have a DataObject tag on them, field sizes will be extracted for the documentation as well</param>
        /// <param name="undefinedObjectList">Due to constraints in documentation, there may be some child members that are IBindable or other generic types that cannot be determined. Use this to include a method of processing those cases. This requires a pair consisting of the FunctionID as a string and the type to use for processing the documentation.</param>
        public ApiRouteAttribute(string functionId, float revision, string template, Type typeForDocumentation, object[] undefinedObjectList)
            : this(functionId, revision, template, typeForDocumentation)
        {
            if (undefinedObjectList != null)
            {
                if (undefinedObjectList.Length % 2 != 0)
                    throw new ApplicationException("ApiRoute has incorrectly defined Undefined Objects List.");

                for (int i = 0; i < undefinedObjectList.Length; i += 2)
                {
                    var key = Guid.Parse((string)undefinedObjectList[i]);
                    if (!UndefinedTypeList.ContainsKey(key))
                        this.UndefinedTypeList.Add(key, (Type)undefinedObjectList[i + 1]);
                }
            }
        }

        /// <summary>
        /// Route Template handler for Traverse API. This implementation is for internal purposes only for allowing usage on unsecure routes
        /// </summary>
        /// <param name="template">The typical route template as used by the standard Route Attribute. It is not necessary to supply /api. The /api will be added by default along with the application id.</param>
        /// <param name="typeForDocumentation">This would be the object type to use for documentation purposes. When this is an IEntity object or an object whose properties have a DataObject tag on them, field sizes will be extracted for the documentation as well</param>
        internal ApiRouteAttribute(string template, Type typeForDocumentation)
            : this(null, 2f, template, typeForDocumentation)
        {
            SkipUserAccessValidation = true;
        }

        /// <summary>
        /// Route Template handler for Traverse API. This implementation is for internal purposes only for allowing usage on unsecure routes
        /// </summary>
        /// <param name="template">The typical route template as used by the standard Route Attribute. It is not necessary to supply /api. The /api will be added by default along with the application id.</param>
        /// <param name="adminOnly">Option to make this a system/support call only that would require higher permissions than normal</param>
        /// <param name="typeForDocumentation">This would be the object type to use for documentation purposes. When this is an IEntity object or an object whose properties have a DataObject tag on them, field sizes will be extracted for the documentation as well</param>
        internal ApiRouteAttribute(string template, bool adminOnly, Type typeForDocumentation)
            : this(null, 2f, template, typeForDocumentation)
        {
            SkipUserAccessValidation = true;
            AdminFunction = adminOnly;
        }
        #endregion Constructors

        #region Public Methods
        public RouteEntry CreateRoute(DirectRouteFactoryContext context)
        {
            //Calculate the revision info. We will be using v1 for the legacy calls that existed prior and v2 for our new calls. It is possible that v3 could happen at some point as well
            string revision = string.Format("/v{0}", Revision);

            //Calculate the string needed for the application ID; Application ID can be blank
            string applicationId = string.IsNullOrEmpty(FunctionInfo?.AppId) ? revision :
                string.Format("{1}/{0}",
                    FunctionInfo.AppId.Equals("jc", StringComparison.OrdinalIgnoreCase) ? "pc" : FunctionInfo.AppId.ToLower(), revision);

            string prefix = string.IsNullOrEmpty(context.Prefix) ? string.Empty : string.Format("/{0}", context.Prefix);

            //Calculate the actual template in the form of /api/v{Revision}{/appId}/template
            string revisedTemplate = string.Format(Resources.ApiTemplateFormat, applicationId, prefix, Template);

            //Use standard process to build route; this will include the user prefixes
            var builder = context.CreateBuilder(revisedTemplate.ToLower());

            //Check that we have a collection for DataTokens
            if (builder.DataTokens == null)
                builder.DataTokens = new RouteValueDictionary();

            //Update our function id on this entry
            if (builder.DataTokens.ContainsKey(Resources.ApiFunctionDataToken))
                builder.DataTokens[Resources.ApiFunctionDataToken] = this;
            else
                builder.DataTokens.Add(Resources.ApiFunctionDataToken, this);

            //Generate route object
            return builder.Build();
        }
        #endregion Public Methods

        #region Private Methods
        private ApiFunctionHeader LoadFunctionInfo()
        {
            if (_functionInfo == null)
            {
                _functionInfo = ApiUtility.GetApiFunction(TravApiConfig.ApiDatabase, FunctionId);
            }

            return _functionInfo;
        }
        #endregion Private Methods

        #region Properties
        public string Template { get; private set; }

        public float Revision { get; private set; }

        public Guid FunctionId { get; private set; }

        internal bool SkipUserAccessValidation { get; private set; }

        internal bool AdminFunction { get; private set; }

        internal Type DocumentType { get; private set; }

        internal Dictionary<Guid, Type> UndefinedTypeList { get; } = new Dictionary<Guid, Type>();

        private ApiFunctionHeader FunctionInfo { get => LoadFunctionInfo(); }
        #endregion Properties
    }
}