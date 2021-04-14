#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TRAVERSE.Business;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public static class ApiTypeExtension
    {
        #region Static Methods
        public static PropertyDescriptorCollection GetObjectProperties(this Type type)
        {
            List<PropertyDescriptor> collection = new List<PropertyDescriptor>();
            if (type != null)
            {
                //If this is not an entitybase type, return all public properties
                if (!typeof(EntityBase).IsAssignableFrom(type))
                    return TypeDescriptor.GetProperties(type);

                //otherwise, filter properties that are bindable or enumerable
                var attr = new BindableAttribute(true);

                foreach (PropertyDescriptor desc in TypeDescriptor.GetProperties(type))
                {
                    if (!desc.Attributes.Contains(attr) &&
                        !typeof(EntityBase).IsAssignableFrom(desc.PropertyType) &&
                        desc.PropertyType.GetInterface(typeof(System.Collections.IEnumerable).Name) == null)
                        continue;

                    collection.Add(desc);
                }
            }
            return new PropertyDescriptorCollection(collection.ToArray());
        }

        public static Type GetEntityTypeColumn(this Type type)
        {
            if (type == null || !typeof(EntityBase).IsAssignableFrom(type))
                return null;

            Type colType = type.GetNestedType("Columns");
            if (colType != null || type.BaseType == null)
                return colType;

            return type.BaseType.GetEntityTypeColumn();
        }

        public static int GetMaxStringLength(this Type type, string compId, string propertyName)
        {
            PropertyDescriptor property = null;
            try
            {
                if (!string.IsNullOrEmpty(propertyName))
                {
                    var props = GetObjectProperties(type);

                    if (props == null || (property = props.Find(propertyName, false)) == null || !typeof(string).IsAssignableFrom(property.PropertyType))
                        return -2;  //property is not a string

                    if (typeof(EntityBase).IsAssignableFrom(type))
                    {
                        Type columns = type.GetEntityTypeColumn();
                        if (columns != null)
                            return EntityHelper.GetColumnLength(Enum.Parse(columns, property.Name) as Enum, compId);
                    }
                    else
                    {
                        var attribute = property.Attributes[typeof(DataObjectFieldAttribute)];
                        if (attribute != null)
                            return ((DataObjectFieldAttribute)attribute).Length;
                    }
                }
            }
            catch
            {
                return -3;  //is a string property but does not have a compatible length defined
            }

            return -1;  //this should really only happen if the property is not defined
        }

        public static Type GetPropertySchemaInfo(this Type type, string propertyName)
        {
            PropertyDescriptorCollection props = null;
            PropertyDescriptor property = null;

            //confirm that we have a type; attempt to retrieve the property list; then, attempt to find the property
            if (type != null && (props = GetObjectProperties(type)) != null && (property = props.Find(propertyName, false)) != null)
            {
                return Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            }

            return typeof(object);
        }

        public static Tuple<Type, int> GetCustomFieldInfo(this Type type, string compId, string customFieldName)
        {
            if (type != null && typeof(EntityBase).IsAssignableFrom(type))
            {
                var list = CustomFieldsList.RetrieveAssignedFieldsList(Utils.GetStaticPropertyValue(type, "TableName").ToString(), compId);
                if (list != null)
                {
                    CustomField field = list.FirstOrDefault(c => c.Name == customFieldName);
                    if (field != null)
                    {
                        return new Tuple<Type, int>(field.SystemType, field.MaxLength);
                    }
                }
            }

            return null;
        }

        public static CustomFieldsList GetCustomFieldList(this Type type, string compId)
        {
            if (type != null && typeof(EntityBase).IsAssignableFrom(type))
                return CustomFieldsList.RetrieveAssignedFieldsList(Utils.GetStaticPropertyValue(type, "TableName").ToString(), compId);

            return null;
        }

        public static List<ApiEntitySchema> GetEntitySchema(this Type type, ApiUserFunctionComp function)
        {
            return ApiEntitySchema.GetSchema(type, function);
        }
        #endregion Static Methods
    }
}
