#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using TRAVERSE.Business;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public sealed class ApiEntitySchema
    {
        #region Constructors
        private ApiEntitySchema()
        { }
        #endregion Constructors

        #region Static Methods
        public static List<ApiEntitySchema> GetSchema(ApiUserFunctionComp function)
        {
            return GetSchema(null, function);
        }

        internal static List<ApiEntitySchema> GetSchema(Type type, ApiUserFunctionComp function)
        {
            List<ApiEntitySchema> schemaList = new List<ApiEntitySchema>();

            //make sure that we have a valid parameter and the function exists
            if (function != null && function.Parent?.FunctionInfo != null)
            {
                //Loop through each default schema object and create schema item
                foreach (ApiFunctionSchema item in function.Parent?.FunctionInfo.SchemaList)
                {
                    schemaList.Add(PopulateFields(item, function, type));
                }

                //Loop through user custom field list
                CustomFieldsList list = type.GetCustomFieldList(function.CompanyId);
                var cfList = function.Parent.SchemaList.FindAll(itm => itm.FieldType == (byte)AccessFieldType.Custom);
                
                if (list == null || list.Count == 0)
                {
                    foreach (ApiUserFunctionSchema cfItem in cfList)
                    {
                        schemaList.Add(PopulateCustomSchema(null, cfItem));
                    }
                }
                else
                {
                    foreach (CustomField field in list)
                    {
                        var cfItem = cfList.Find(i => field.Name.Equals(i.EntityName, StringComparison.OrdinalIgnoreCase));
                        schemaList.Add(PopulateCustomSchema(field, cfItem));
                    }
                }
            }

            return schemaList;
        }

        private static ApiEntitySchema PopulateFields(ApiFunctionSchema item, ApiUserFunctionComp function, Type type)
        {
            ApiEntitySchema schema = new ApiEntitySchema();
            //Check to see if we have a custom schema on the user setup but only for Physical Fields
            var userOverride = function.Parent?.SchemaList.Find(fn => fn.FieldType == 0 && fn.FunctionSchemaId == item.Id);

            //Build schema object
            schema.SequenceNumber = item.SeqNum.GetValueOrDefault();
            schema.FieldType = AccessFieldType.Entity;
            schema.EntityFieldName = item.TravFieldName;
            schema.QueryColumnName = item.QueryColumnName ?? item.TravFieldName;
            schema.ApiFieldName = userOverride?.ApiFieldName ?? item.ApiFieldName;
            schema.Hidden = userOverride?.Hidden ?? false;
            schema.DefaultValue = userOverride?.DefaultValue;
            schema.Notes = ((item.Notes + Environment.NewLine) ?? string.Empty) + (userOverride != null && !string.IsNullOrWhiteSpace(userOverride.Notes) ? userOverride.Notes : string.Empty);
            schema.FieldAccess = (ApiFieldSetting)item.FieldSetting.GetValueOrDefault();
            schema.TranslateList = new List<ApiValueTranslate>(item.ValueList);
            schema.ChildFunction = item.ChildFunctionId;

            if (type != null)
            {
                schema.ObjectType = type.GetPropertySchemaInfo(item.TravFieldName);
                int response = type.GetMaxStringLength(function.CompanyId, item.TravFieldName);
                if (response > 0 || response == -3)
                {
                    schema.MaxLength = (response == -3) ? int.MaxValue : response;
                }
            }

            return schema;
        }

        private static ApiEntitySchema PopulateCustomSchema(CustomField field, ApiUserFunctionSchema cfItem)
        {
            return new ApiEntitySchema()
            {
                FieldType = AccessFieldType.Custom,
                EntityFieldName = field?.Name ?? cfItem?.EntityName,
                ApiFieldName = cfItem?.ApiFieldName ?? field.Name,
                Hidden = (cfItem?.Hidden).GetValueOrDefault(),
                DefaultValue = cfItem?.DefaultValue,
                Notes = cfItem?.Notes ?? string.Empty,
                FieldAccess = ApiFieldSetting.Create | ApiFieldSetting.Delete | ApiFieldSetting.Edit | ApiFieldSetting.Read,
                ObjectType = field.SystemType,
                MaxLength = field.MaxLength,
                QueryColumnName = string.Format("cf_{0}", field?.Name ?? cfItem?.EntityName)
            };
        }
        #endregion Static Methods

        #region Properties
        public int SequenceNumber { get; private set; }

        public string EntityFieldName { get; private set; }

        public string QueryColumnName { get; private set; }

        public string ApiFieldName { get; private set; }

        public bool Hidden { get; private set; }

        public string DefaultValue { get; private set; }

        public string Notes { get; private set; }

        public string Filter { get; private set; }

        public Guid? ChildFunction { get; private set; }

        public Type ObjectType { get; private set; }

        public List<ApiValueTranslate> TranslateList { get; private set; }

        public ApiFieldSetting FieldAccess { get; private set; }

        public AccessFieldType FieldType { get; private set; }

        public int MaxLength { get; private set; }

        private EntityBase EntityItem { get; set; }
        #endregion Properties
    }
}
