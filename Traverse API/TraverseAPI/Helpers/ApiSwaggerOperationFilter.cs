using OSI.TraverseApi.Business;
using Swagger.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using TRAVERSE.Business;
using TraverseApi.Properties;

namespace TraverseApi
{
    public class ApiSwaggerOperationFilter : IOperationFilter
    {
        #region Methods
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            try
            {
                Guid? functionId = null;
                Type type = null;
                Dictionary<Guid, Type> undefinedList = null;

                string method = apiDescription.HttpMethod.Method.ToLower();
                var binding = apiDescription.ActionDescriptor.ActionBinding;
                ApiUser user = HttpContext.Current.User as ApiUser;
                ApiUserFunctionComp functionInfo = null;

                var securityDictionary = new Dictionary<string, IEnumerable<string>>();
                securityDictionary.Add("oauth2", new string[] { });
                operation.security = new List<IDictionary<string, IEnumerable<string>>>() { securityDictionary };

                if (apiDescription.Route.DataTokens != null && apiDescription.Route.DataTokens.ContainsKey(Resources.ApiFunctionDataToken))
                {
                    functionId = ((ApiRouteAttribute)apiDescription.Route.DataTokens[Resources.ApiFunctionDataToken]).FunctionId;
                    type = ((ApiRouteAttribute)apiDescription.Route.DataTokens[Resources.ApiFunctionDataToken]).DocumentType;
                    undefinedList = ((ApiRouteAttribute)apiDescription.Route.DataTokens[Resources.ApiFunctionDataToken]).UndefinedTypeList;
                }

                if (functionId.HasValue && functionId != Guid.Empty)
                {
                    var function = user?.FunctionList.Find(f => f.FunctionId == functionId);
                    if (function != null)
                        functionInfo = function.CompanyList.FirstOrDefault();

                    if (function == null || functionInfo == null)
                        return;

                    operation.tags = new List<string>() { function.FunctionName };
                    operation.description = function.FunctionInfo?.Notes;
                }

                this.AppendQueryParameters(operation, method);
                this.AppendMessageSecurityHeader(operation);
                this.AppendXmlFormats(operation);
                this.AddActionParameters(operation, schemaRegistry, method, binding.ParameterBindings, functionInfo, type, undefinedList);
                this.AddSuccessResponse(operation, schemaRegistry, "get", binding, functionInfo, type, undefinedList);
                this.AppendErrorResponse(operation, schemaRegistry);
            }
            catch (Exception ex)
            {
                System.Threading.Tasks.Task.FromResult(ApiErrorHandler.ProcessError(new Exception(string.Format("Path ({0}): {1}||{2}", apiDescription.RelativePath, apiDescription.HttpMethod.Method, ex.Message), ex)));
                operation.description = string.Format("{0}{1}", Resources.ApiOperationSchemaIncomplete, operation.description);
            }
        }

        private void AppendQueryParameters(Operation operation, string method)
        {
            //Add Query parameter for Company ID
            operation.parameters.Add(new Parameter()
            {
                name = Resources.ApiRequestCompany,
                @in = "query",
                required = true,
                @type = "string",
                format = "string",
                description = Resources.ApiDocumentCompanyParam
            });

            //These parameters are only allowed on a GET call
            if (method == "get")
            {
                //Add optional query parameter for Page
                operation.parameters.Add(new Parameter()
                {
                    name = Resources.ApiRequestPage,
                    @in = "query",
                    required = false,
                    @type = "integer",
                    format = "int32"
                });

                //Add optional query parameter for Page Size
                operation.parameters.Add(new Parameter()
                {
                    name = Resources.ApiRequestPageSize,
                    @in = "query",
                    required = false,
                    @type = "integer",
                    format = "int32"
                });
            }
        }

        private void AppendMessageSecurityHeader(Operation operation)
        {
            if (!ApiUtility.ApiConfig.UseMsgSecurity.GetValueOrDefault())
                return;

            operation.parameters.Add(new Parameter()
            {
                name = Resources.ApiSignatureHeaderKey,
                @in = "header",
                required = true,
                @type = "string",
                format = "string",
                description = string.Format(Resources.ApiSwaggerMsgSecurity,
                    ApiUtility.ApiConfig.MsgEncryptionType.ToString(),
                    string.IsNullOrWhiteSpace(ApiUtility.ApiConfig.MsgSharedKey) ? Resources.ApiMsgSecClientId : Resources.ApiMsgSecContact)
            });
        }

        private void AppendXmlFormats(Operation operation)
        {
            if (!operation.produces.Contains("application/xml"))
                operation.produces.Add("application/xml");
            if (!operation.produces.Contains("text/xml"))
                operation.produces.Add("text/xml");

            if (!operation.consumes.Contains("application/xml"))
                operation.consumes.Add("application/xml");
            if (!operation.consumes.Contains("text/xml"))
                operation.consumes.Add("text/xml");
        }

        private void AddActionParameters(Operation operation, SchemaRegistry schemaRegistry, string httpMethod, HttpParameterBinding[] parameterBindingList, ApiUserFunctionComp function, Type type, Dictionary<Guid, Type> undefinedTypes)
        {
            if (operation == null || parameterBindingList == null || parameterBindingList.Length == 0 || function == null)
                return;

            foreach (Parameter parameter in operation.parameters)
            {
                var binding = parameterBindingList.FirstOrDefault(p => parameter.name == p.Descriptor.ParameterName);
                if (binding == null)
                    continue;

                parameter.required = !binding.Descriptor.IsOptional;

                if (binding.Descriptor.GetCustomAttributes<System.Web.Http.FromBodyAttribute>().FirstOrDefault() == null)
                    continue;

                parameter.schema = CalculateSchema(schemaRegistry, function, type, httpMethod, undefinedTypes);
            }
        }

        private void AddSuccessResponse(Operation operation, SchemaRegistry schemaRegistry, string httpMethod, HttpActionBinding actionBinding, ApiUserFunctionComp function, Type type, Dictionary<Guid, Type> undefinedTypes)
        {
            if (!operation.responses.ContainsKey("200") || actionBinding == null || function == null)
                return;

            operation.responses["200"].schema = CalculateSchema(schemaRegistry, function, type, httpMethod, undefinedTypes);
        }

        private void AppendErrorResponse(Operation operation, SchemaRegistry schemaRegistry)
        {
            BuildErrorResponse(operation, schemaRegistry, "400", "One or more expected values are missing", new NothingToProcessException());
            BuildErrorResponse(operation, schemaRegistry, "401", "You do not have access to the requested resource", new PermissionDeniedException());
            BuildErrorResponse(operation, schemaRegistry, "403", "A major server issue such as missing business rule exists. Application pool needs to be refreshed", new BusinessRuleException());
            BuildErrorResponse(operation, schemaRegistry, "409", "An entity validation error such as an invalid entity property or closed gl period", new PeriodClosedException());
            BuildErrorResponse(operation, schemaRegistry, "500", "General server processing error such as divide by zero", new DivideByZeroException());
        }

        private void BuildErrorResponse(Operation operation, SchemaRegistry schemaRegistry, string responseCode, string description, Exception example)
        {
            if (!operation.responses.ContainsKey(responseCode))
            {
                var response = new Response()
                {
                    schema = schemaRegistry.GetOrRegister(typeof(ApiError)),
                    description = description,
                };

                //response.schema.example = ApiError.GenerateError(example);

                operation.responses.Add(responseCode, response);
            }
        }

        private Schema CalculateSchema(SchemaRegistry schemaRegistry, ApiUserFunctionComp function, Type type, string method, Dictionary<Guid, Type> undefinedTypes, bool excludeArrayWrapper = false)
        {
            Dictionary<string, Schema> methodDef = null;
            Schema schema = null;

            if (!FunctionSchema.TryGetValue(function.Parent.FunctionId.Value, out methodDef) || !methodDef.TryGetValue(method, out schema) || schema == null)
            {
                if (excludeArrayWrapper)
                {
                    schema = GenerateSchema(schemaRegistry, type, function, method, undefinedTypes);
                }
                else
                {
                    schema = new Schema()
                    {
                        type = "array",
                        items = GenerateSchema(schemaRegistry, type, function, method, undefinedTypes)
                    };
                }

                if (methodDef == null)
                {
                    methodDef = new Dictionary<string, Schema>();
                    FunctionSchema.Add(function.Parent.FunctionId.Value, methodDef);
                }

                methodDef.Add(method, schema);
            }
            return schema;
        }

        private Schema GenerateSchema(SchemaRegistry schemaRegistry, Type type, ApiUserFunctionComp function, string method, Dictionary<Guid, Type> undefinedTypes)
        {
            if (function == null)
            {
                return schemaRegistry.GetOrRegister(type);
            }
            else if (type != null)
            {
                Schema cfSchema = null;
                Schema entitySchema = new Schema();
                entitySchema.type = "object";
                entitySchema.properties = new Dictionary<string, Schema>();
                entitySchema.required = new List<string>();

                foreach (ApiEntitySchema item in type.GetEntitySchema(function))
                {
                    //Skip if field is hidden
                    if (item.Hidden)
                        continue;

                    //Skip if this is a GET call and user does not have read access
                    if (method == "get" && (item.FieldAccess & ApiFieldSetting.Read) != ApiFieldSetting.Read)
                        continue;

                    //Ship if this is a POST call and user does not have write access to field, including required entry
                    if (method == "post" && (item.FieldAccess & ApiFieldSetting.Create) != ApiFieldSetting.Create &&
                        (item.FieldAccess & ApiFieldSetting.Required_Create) != ApiFieldSetting.Required_Create)
                        continue;

                    //Ship if this is a PUT call and user does not have edit access to field, including required entry
                    if (method == "put" && (item.FieldAccess & ApiFieldSetting.Edit) != ApiFieldSetting.Edit &&
                        (item.FieldAccess & ApiFieldSetting.Required_Edit) != ApiFieldSetting.Required_Edit)
                        continue;

                    //Ship if this is a PUT call and user does not have edit access to field, including required entry
                    if (method == "delete" && (item.FieldAccess & ApiFieldSetting.Delete) != ApiFieldSetting.Delete &&
                        (item.FieldAccess & ApiFieldSetting.Required_Delete) != ApiFieldSetting.Required_Delete)
                        continue;

                    var schema = new Schema();

                    if (item.ObjectType != null && (item.ObjectType.IsPrimitive || item.ObjectType.IsValueType || item.ObjectType == typeof(string)))
                        schema.type = item.ObjectType.Name.ToLower();
                    else
                        schema.type = "object";

                    if (item.ChildFunction.HasValue)
                    {
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(item.ObjectType))
                        {
                            Type childType = null;
                            schema.type = "array";
                            childType = item.ObjectType.GetGenericArguments().FirstOrDefault();

                            if (childType == null && undefinedTypes.ContainsKey(item.ChildFunction.Value))
                            {
                                childType = undefinedTypes[item.ChildFunction.Value];
                            }

                            schema.items = CalculateSchema(schemaRegistry, function.GetChildFunctionDef(item.ChildFunction.Value), childType, method, undefinedTypes, true);
                        }
                        else
                            schema = CalculateSchema(schemaRegistry, function.GetChildFunctionDef(item.ChildFunction.Value), item.ObjectType, method, undefinedTypes, true);
                    }

                    schema.description = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim();
                    schema.maxLength = (item.MaxLength == 0 || item.MaxLength == int.MaxValue) ? null : new int?(item.MaxLength);
                    if (item.TranslateList != null && item.TranslateList.Count > 0)
                    {
                        schema.type = "string";
                        List<object> enumList = new List<object>();
                        item.TranslateList.ForEach(t => enumList.Add(t.Value));
                        schema.@enum = enumList;
                    }
                    if ((method == "post" && (item.FieldAccess & ApiFieldSetting.Required_Create) == ApiFieldSetting.Required_Create) ||
                        (method == "put" && (item.FieldAccess & ApiFieldSetting.Required_Edit) == ApiFieldSetting.Required_Edit) ||
                        (method == "delete" && (item.FieldAccess & ApiFieldSetting.Required_Delete) == ApiFieldSetting.Required_Delete))
                    {
                        entitySchema.required.Add(item.ApiFieldName);
                    }

                    if (item.FieldType == AccessFieldType.Custom)
                    {
                        if (cfSchema == null)
                            cfSchema = new Schema() { type = "object", properties = new Dictionary<string, Schema>() };

                        cfSchema.properties.Add(item.ApiFieldName, schema);
                        continue;
                    }
                    entitySchema.properties.Add(item.ApiFieldName, schema);
                }

                if (cfSchema != null)
                    entitySchema.properties.Add("custom_fields", cfSchema);

                return entitySchema;
            }

            return new Schema();
        }
        #endregion Methods

        #region Properties
        private Dictionary<Guid, Dictionary<string, Schema>> FunctionSchema { get; } = new Dictionary<Guid, Dictionary<string, Schema>>();
        #endregion Properties
    }
}