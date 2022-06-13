#region Using Directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TRAVERSE.Business;
using TRAVERSE.Business.API;
using TRAVERSE.Web.API.Properties;
#endregion Using Directives

namespace TRAVERSE.Web.API
{
    public sealed class ApiEntityModel : DynamicObject, IDictionary<string, object>, IDisposable
    {
        #region Fields
        private Dictionary<string, object> _valueDictionary = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        private ApiUserFunctionComp _apiUserAccessInfo;
        #endregion Fields

        #region Public Methods
        public void PopulateEntity(object entity)
        {
            PopulateEntity(entity, null);
        }

        public void PopulateEntity(object entity, ChildRecordHandler childHandler)
        {
            Task.FromResult(PopulateEntityData(entity, childHandler));
            if (FieldUpdateIsComplete != null)
                FieldUpdateIsComplete.Invoke(entity);
        }

        public async Task PopulateEntityAsync(object entity)
        {
            await this.PopulateEntityAsync(entity, null);
        }

        public async Task PopulateEntityAsync(object entity, ChildRecordHandler childHandler)
        {
            await PopulateEntityData(entity, childHandler);
            if (FieldUpdateIsComplete != null)
                FieldUpdateIsComplete.Invoke(entity);
        }
        #endregion Public Methods

        #region Internal Methods
        internal void InitializeCustomFieldList()
        {
            if (CustomFieldList == null)
                CustomFieldList = new Dictionary<string, object>();
        }

        internal void Serialize(object item, HandleResponseFields customResponse)
        {
            _valueDictionary.Clear();  //We need to clear the item in case this is being reused

            if (item == null)
                return;  //Returns if there is nothing to populate

            SerializeObjectField(item, item.GetType().GetObjectProperties(), customResponse);
        }
        #endregion Internal Methods

        #region Private Methods
        private ApiUserFunctionComp LoadApiUserAccessInfo()
        {
            if (_apiUserAccessInfo == null)
            {
                _apiUserAccessInfo = HttpContext.Current.Request.GetOwinContext().Get<ApiUserFunctionComp>(Resources.ApiUserRequestActionStorage);
            }

            return _apiUserAccessInfo;
        }

        private void SerializeObjectField(object obj, PropertyDescriptorCollection collection, HandleResponseFields customResponse)
        {
            EntityBase entity = obj as EntityBase;

            //Assume if we have made it this far, this user has been verified and this is a system process
            if (ApiUserAccessInfo == null)
            {
                foreach (PropertyDescriptor property in collection)
                {
                    this[property.Name] = property.GetValue(obj);
                }
                return;
            }

            foreach (ApiEntitySchema fieldItem in ApiUserAccessInfo.EntitySchemaList)
            {
                try
                {
                    if (fieldItem.FieldType == AccessFieldType.Custom ||   //Field is a custom field; these are handled later
                        fieldItem.Hidden ||   //Field is hidden from user and there is no defined default value
                        (fieldItem.FieldAccess & ApiFieldSetting.Read) != ApiFieldSetting.Read)
                        continue;

                    PropertyDescriptor property = collection.Find(fieldItem.EntityFieldName, false);

                    object value = property?.GetValue(obj);

                    var args = new ApiEntityPropertyChangingArgs(obj, fieldItem.EntityFieldName, value);
                    customResponse.Invoke(args);
                    if (args.Handled)  //allows implementer to set custom value to a field or skip field
                        continue;

                    value = args.ActualValue;
                    if (fieldItem.ChildFunction.HasValue)  //We are setup to use another Function to serialize this item
                    {
                        value = ProcessChildData(value, fieldItem.ChildFunction.Value, customResponse);
                    }
                    else if (fieldItem.TranslateList.Count > 0) //if we have enumerated field values, this is an enumerated field
                    {
                        //Attempt to find the enumerated value using the object's value as the key
                        object tempValue = fieldItem.TranslateList.FirstOrDefault(t => t.Key == ApiUtility.ConvertToType<string>(value, null))?.Value;
                        value = tempValue ?? value;
                    }

                    this[fieldItem.ApiFieldName] = value;
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(string.Format("{0}: {1}", fieldItem.ApiFieldName, ex.Message), ex);
                }
            }

            //if an actual entity; attempt to process custom fields
            if (entity != null && entity.CustomFieldList != null && entity.CustomFieldList.Count > 0)
            {
                InitializeCustomFieldList();

                foreach (CustomField field in entity.CustomFieldList)
                {
                    ApiEntitySchema schemaField = ApiUserAccessInfo.EntitySchemaList.Find(itm => itm.FieldType == AccessFieldType.Custom && itm.EntityFieldName == field.Name);
                    if (schemaField != null &&
                        (schemaField.Hidden ||
                        (schemaField.FieldAccess & ApiFieldSetting.Read) != ApiFieldSetting.Read))
                        continue;

                    try
                    {
                        object cfValue = entity.FindCustomValue<string>(field, null);
                        if (!string.IsNullOrWhiteSpace(cfValue as string))
                            cfValue = Convert.ChangeType(cfValue as string, field.SystemType);

                        CustomFieldList[schemaField?.ApiFieldName ?? field.Name] = cfValue;
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException(string.Format("cf_{0}: {1}", schemaField?.ApiFieldName ?? field.Name, ex.Message), ex);
                    }
                }

                this[ApiUtility.ApiCustomFieldName] = CustomFieldList;
            }
        }

        private object ProcessChildData(object childObject, Guid childFunction, HandleResponseFields customResponse)
        {
            ApiUserFunctionComp function = ApiUserAccessInfo.GetChildFunctionDef(childFunction);
            if (function == null)
                return null;

            return BuildModelList(function, childObject, customResponse);
        }

        private async Task PopulateEntityData(object entity, ChildRecordHandler childHandler)
        {
            //Creating object if this is a post or if the object is actually a new Traverse entity object
            bool isCreate = (HttpContext.Current.Request.HttpMethod.ToLower() == "post") || ((entity as EntityBase)?.IsNew ?? false);

            //Editing object if this is a put or if the object is actually an existing Traverse entity object
            bool isEdit = (HttpContext.Current.Request.HttpMethod.ToLower() == "put") || !(((entity as EntityBase)?.IsNew ?? true));
            ApiUserAccessInfo.EntitySchemaList.Sort(SortEntitySchema);

            PropertyDescriptorCollection collection = entity.GetType().GetObjectProperties();
            foreach (ApiEntitySchema schema in ApiUserAccessInfo.EntitySchemaList)
            {
                try
                {
                    //Did the user provide an entry for this field
                    bool fieldInDictionary = _valueDictionary.ContainsKey(schema.ApiFieldName);

                    //Adding individual if statements to simplify readability
                    //Check 1: If this field is not editable, skip  (not editable = no access[0]; read only access[1]; delete only[8]; read/delete only access[9])
                    if (schema.FieldAccess == ApiFieldSetting.None || (byte)schema.FieldAccess == 1 || (byte)schema.FieldAccess == 8 || (byte)schema.FieldAccess == 9)
                        continue;

                    //Check 2: Validate creation requirements
                    if (isCreate)
                    {
                        //Create Validation 1: Check if field is read-only on create. If so, skip
                        if ((schema.FieldAccess & ApiFieldSetting.Create) != ApiFieldSetting.Create &&
                            (schema.FieldAccess & ApiFieldSetting.Required_Create) != ApiFieldSetting.Required_Create)
                            continue;

                        //Create Validation 2: Check if field is required for create, if so and not provided, error
                        if (!fieldInDictionary && (schema.FieldAccess & ApiFieldSetting.Required_Create) == ApiFieldSetting.Required_Create)
                            throw new InvalidValueException(string.Format(Resources.ApiMissingRequiredField, schema.ApiFieldName, HttpContext.Current.Request.HttpMethod.ToLower()));
                    }

                    //Check 3: Validate edit requirements
                    if (isEdit)
                    {
                        //Edit Validation 1: Check if field is read-only on edit. If so, skip
                        if ((schema.FieldAccess & ApiFieldSetting.Edit) != ApiFieldSetting.Edit &&
                            (schema.FieldAccess & ApiFieldSetting.Required_Edit) != ApiFieldSetting.Required_Edit)
                            continue;

                        //Edit Validation 2: Check if field is required for create, if so and not provided, error
                        if (!fieldInDictionary && (schema.FieldAccess & ApiFieldSetting.Required_Edit) == ApiFieldSetting.Required_Edit)
                            throw new InvalidValueException(string.Format(Resources.ApiMissingRequiredField, schema.ApiFieldName, HttpContext.Current.Request.HttpMethod.ToLower()));
                    }

                    //Check 4: If the user did not provide a value and there is no default value, skip
                    if (!fieldInDictionary && schema.DefaultValue == null)
                        continue;

                    //Check 5: Check if this is a child function. If so, handle individually
                    if (schema.ChildFunction != null)
                    {
                        //If the implementer is not handling child objects, skip
                        if (childHandler == null)
                            continue;

                        object child = null;
                        _valueDictionary.TryGetValue(schema.ApiFieldName, out child);

                        //if there is no child object provided, skip
                        if (child == null)
                            continue;

                        //Load the child function defintion. If not found, continue. Should we error?
                        ApiUserFunctionComp childFunction = ApiUserAccessInfo.GetChildFunctionDef(schema.ChildFunction.Value);
                        if (childFunction == null)
                            continue;

                        //If the child is not an enumerable object (Array of ApiEntityModel), create and enumerable object with child as the only record
                        IEnumerable enumerable = child as IEnumerable;
                        if (enumerable == null)
                        {
                            enumerable = new List<object> { child };
                        }

                        //Loop through each item in the enumerable object
                        foreach (ApiEntityModel item in enumerable)
                        {
                            //Set function schema for user in order to process child object appropriately
                            item._apiUserAccessInfo = childFunction;

                            //Create args and submit to implementer's handler method
                            ApiChildRecordArgs args = new ApiChildRecordArgs(entity, schema.EntityFieldName, item);
                            object childObject = childHandler.Invoke(args);

                            if (args.Ignore)
                                continue;

                            if (childObject == null)
                                throw new InvalidValueException(string.Format(Resources.ApiChildObjectError, schema.ApiFieldName));

                            //Populate the data using the object returned from the handler
                            await item.PopulateEntityAsync(childObject, childHandler);
                        }

                        continue;
                    }

                    //Set default value for field
                    object value = schema.DefaultValue;

                    //If the property does not exist on the entity, skip. Should we error? As of 20200110, no.
                    var property = collection.Find(schema.EntityFieldName, false);

                    //If the field is not a hidden field and the user provided a value, use the user's provided value
                    if (fieldInDictionary && !schema.Hidden)
                        _valueDictionary.TryGetValue(schema.ApiFieldName, out value);

                    //If the field has an enumerated value list, process the list
                    if (schema.TranslateList.Count > 0)
                    {
                        //Attempt to find the enumerated key using the object's value
                        object tempValue = schema.TranslateList.FirstOrDefault(t => t.Value.Equals(ApiUtility.ConvertToType<string>(value, null), StringComparison.OrdinalIgnoreCase))?.Key;
                        value = tempValue ?? value;
                    }

                    //If we have a property, get the value in the correct type
                    if (property != null)
                        value = ApiUtility.ConvertToType(value, property.PropertyType, property.GetValue(entity));

                    //Call any applied event handlers to manage the value or setting of the field
                    if (SkipPropertyAssignment(entity, schema.EntityFieldName, ref value) || property == null || property.IsReadOnly)
                        continue;

                    //Confirm value is the correct type
                    value = ApiUtility.ConvertToType(value, property.PropertyType, property.GetValue(entity));

                    //Set the value, convert the value to the proper type first
                    property.SetValue(entity, value);
                }
                catch (Exception ex)
                {
                    throw new InvalidValueException(string.Format("{0}: {1}", schema.ApiFieldName, ex.Message), ex);
                }
            }

            EntityBase obj = entity as EntityBase;
            //if an actual entity; attempt to process custom fields
            if (obj != null && obj.CustomFieldList != null && CustomFieldList != null && CustomFieldList.Count > 0)
            {
                foreach (KeyValuePair<string, object> pair in CustomFieldList)
                {
                    ApiEntitySchema schemaField = ApiUserAccessInfo.EntitySchemaList.Find(itm => itm.FieldType == AccessFieldType.Custom && itm.ApiFieldName == pair.Key);
                    if (schemaField != null && schemaField.Hidden)
                        continue;

                    try
                    {
                        CustomField field = obj.CustomFieldList.FirstOrDefault(c => c.Name.Equals(schemaField?.EntityFieldName ?? pair.Key, StringComparison.OrdinalIgnoreCase));
                        if (field == null)
                            continue;

                        obj.AssignCustomValue(field, Convert.ToString(ApiUtility.ConvertToType(pair.Value, field.SystemType, null)));
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException(string.Format("cf_{0}: {1}", pair.Key, ex.Message), ex);
                    }
                }
            }
        }

        private int SortEntitySchema(ApiEntitySchema schema1, ApiEntitySchema schema2)
        {
            if (schema1 == null && schema2 == null)
                return 0;

            if (schema1 == null)
                return 1;

            if (schema2 == null)
                return -1;

            if (schema1.SequenceNumber == schema2.SequenceNumber)
                return string.Compare(schema1.EntityFieldName, schema2.EntityFieldName, StringComparison.OrdinalIgnoreCase);
   
            return CaseInsensitiveComparer.Default.Compare(schema1.SequenceNumber, schema2.SequenceNumber);
        }

        private bool SkipPropertyAssignment(object entity, string propertyName, ref object value)
        {
            if (EntityPropertyChanging != null)
            {
                ApiEntityPropertyChangingArgs args = new ApiEntityPropertyChangingArgs(entity, propertyName, value);
                EntityPropertyChanging.Invoke(this, args);

                value = args.ActualValue;
                return args.Handled;
            }

            return false;
        }
        #endregion Private Methods

        #region Overrides
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            //Load if we require user access; this could be no if we are processing an internal request
            bool? skipCheck = HttpContext.Current.Request.GetOwinContext().Get<bool>(Resources.ApiIgnoreUserAccessValidation);

            //Check to see if this is a POST or creation request
            bool isNewObject = (HttpContext.Current.Request.HttpMethod.ToLower() == "post");

            if (!skipCheck.GetValueOrDefault() && ApiUserAccessInfo == null)
                throw new ApplicationException("Api model requires an authorized user record to process.");

            if (binder.Name == "CustomFields")
            {
                result = CustomFieldList;
                return true;
            }

            //Find the field being queried by the controller code
            ApiEntitySchema schema = ApiUserAccessInfo?.EntitySchemaList.Find(fld => fld.EntityFieldName == binder.Name);

            if (schema != null)
            {
                //If the field is hidden and we do not have a default value, error because the API caller does not have access to this field
                if (schema.DefaultValue == null && schema.Hidden)
                    throw new MissingFieldException(string.Format(Resources.ApiInvalidFieldName, schema.ApiFieldName));

                object value = isNewObject ? schema.DefaultValue : null;  //Set the default value when present and if we are creating a new object; no need for default on updates
                if (!_valueDictionary.TryGetValue(schema.ApiFieldName, out value))  //Try to pull the actual value and replace the default; if this fails, no value provided by the user, return our Missing constant
                {
                    result = ApiUserSkipped.Value;  //User did not provide field in the body of the request and there is no default value
                    return true;
                }

                result = value;  //Set the value as the result

                //if we have enumerated field values, this is an enumerated field
                if (schema.TranslateList.Count > 0)
                {
                    //Attempt to find the enumerated value using the result value as the key
                    object tempValue = result;
                    tempValue = schema.TranslateList.FirstOrDefault(t => t.Value == ApiUtility.ConvertToType<string>(tempValue, null))?.Key;
                    result = tempValue ?? result;  //if an enumerated value was found, use that value, otherwise, use the original value
                }

                return true;  //We successfully converted the result value
            }

            return _valueDictionary.TryGetValue(binder.Name, out result);  //If there is no schema, do not error at this time; attempt to process like normal...Is this ok? should we always require a schema record?
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (ApiUserAccessInfo == null)
                throw new ApplicationException("Api model requires an authorized user record to process.");

            if (binder.Name != "CustomFields")  //Custom fields are read only
            {
                object apiValue = value;
                ApiEntitySchema schema = ApiUserAccessInfo.EntitySchemaList.Find(fld => fld.EntityFieldName.Equals(binder.Name, StringComparison.OrdinalIgnoreCase));
                if (schema != null && !schema.Hidden)
                {
                    if (schema.TranslateList.Count > 0)
                    {
                        //Attempt to find the enumerated value using the object's value as the key
                        object tempValue = value;
                        tempValue = schema.TranslateList.FirstOrDefault(t => t.Key == ApiUtility.ConvertToType<string>(value, null))?.Value;
                        apiValue = tempValue ?? apiValue;
                    }
                    this[schema.ApiFieldName] = apiValue;
                    return true;
                }

                this[binder.Name] = apiValue;
                return true;
            }

            return false;
        }

        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            if (_valueDictionary.ContainsKey(binder.Name))
            {
                _valueDictionary.Remove(binder.Name);
                return true;
            }
            return false;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _valueDictionary.Keys.ToList();
        }
        #endregion Overrides

        #region Static Methods
        internal static List<dynamic> BuildModelList(ApiUserFunctionComp function, object content, HandleResponseFields customResponse)
        {
            List<dynamic> list = new List<dynamic>();

            IEnumerable enumerable = content as IEnumerable;
            if (enumerable == null)
            {
                if (content == null)
                    enumerable = new List<object>();
                else
                    enumerable = new List<object> { content };
            }

            foreach (object item in enumerable)
            {
                list.Add(BuildResponseObject(function, item, customResponse));
            }

            return list;
        }

        internal static KeyValuePair<int, List<dynamic>> BuildModelList(ApiUserFunctionComp function, object content, HandleResponseFields customResponse, int page, int pageSize)
        {
            int totalCount = 0;
            List<dynamic> list = new List<dynamic>();

            IEnumerable enumerable = content as IEnumerable;
            if (enumerable == null)
            {
                if (content == null)
                    enumerable = new List<object>();
                else
                    enumerable = new List<object> { content };
            }

            
            List<object> pageList = new List<object>();

            int count = pageSize;
            int start = (page - 1) * pageSize;
            foreach (object item in enumerable)
            {
                totalCount++;
                if (page <= 0 || pageSize <= 0)
                    continue;

                if (totalCount > start && totalCount <= (start + count))
                    pageList.Add(item);
            }

            if (page > 0 && pageSize > 0)
                enumerable = pageList; 

            foreach (object item in enumerable)
            {
                list.Add(BuildResponseObject(function, item, customResponse));
            }

            return new KeyValuePair<int, List<dynamic>>(totalCount, list);
        }

        private static dynamic BuildResponseObject(ApiUserFunctionComp function, object item, HandleResponseFields customResponse)
        {
            dynamic model = new ApiEntityModel() { _apiUserAccessInfo = function };
            model.Serialize(item, customResponse);
            return model;
        }
        #endregion Static Methods

        #region Properties
        public Dictionary<string, object> CustomFieldList { get; internal set; }

        public event EventHandler<ApiEntityPropertyChangingArgs> EntityPropertyChanging;

        public delegate void HandleResponseFields(ApiEntityPropertyChangingArgs args);

        public Action<object> FieldUpdateIsComplete { get; set; }

        internal object this[string key]
        {
            get
            {
                object value;
                _valueDictionary.TryGetValue(key, out value);
                return value;
            }
            set
            {
                if (_valueDictionary.ContainsKey(key))
                    _valueDictionary[key] = value;
                else
                    _valueDictionary.Add(key, value);
            }
        }

        private ApiUserFunctionComp ApiUserAccessInfo => LoadApiUserAccessInfo();
        #endregion Properties

        #region Interface 
        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return _valueDictionary.ContainsKey(key);
        }

        void IDictionary<string, object>.Add(string key, object value)
        {
            _valueDictionary.Add(key, value);
        }

        bool IDictionary<string, object>.Remove(string key)
        {
            return _valueDictionary.Remove(key);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return _valueDictionary.TryGetValue(key, out value);
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            _valueDictionary.Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            _valueDictionary.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_valueDictionary).Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)_valueDictionary).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_valueDictionary).Remove(item);
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _valueDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _valueDictionary.GetEnumerator();
        }

        ICollection<string> IDictionary<string, object>.Keys => _valueDictionary.Keys;

        ICollection<object> IDictionary<string, object>.Values => _valueDictionary.Values;

        int ICollection<KeyValuePair<string, object>>.Count => _valueDictionary.Count;

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => ((ICollection<KeyValuePair<string, object>>)_valueDictionary).IsReadOnly;

        object IDictionary<string, object>.this[string key] { get => this[key]; set => this[key] = value; }

        void IDisposable.Dispose()
        {
            this.EntityPropertyChanging = null;
            this.FieldUpdateIsComplete = null;
            this._valueDictionary = null;
            this.CustomFieldList = null;
        }
        #endregion Interface 
    }
}