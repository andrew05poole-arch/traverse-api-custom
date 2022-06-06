#region Using Directives
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TraverseApi
{
    public sealed class ApiJsonConverter : JsonConverter
    {
        #region Overrides
        public override bool CanConvert(Type objectType)
        {
            //Traverse API will treat all types of object, dynamic and ApiEntityModel as the same - a custom dynamic object
            return (objectType == typeof(object) || objectType == typeof(ApiEntityModel));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //Read in the object for processing
            JToken token = JObject.ReadFrom(reader);

            //Process the object into a custom dynamic object for ease of use by the implementer
            return ProcessToken(token);
        }

        //Not used; required for valid implementation of JsonConverter
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        { }
        #endregion Overrides

        #region Private Methods
        private dynamic ProcessToken(JToken token)
        {
            //return null if no data provided
            if (token == null)
                return null;

            //Create dynamic model
            dynamic model = new ApiEntityModel();

            //when this is an array object, process is different including a recursive call to this procedure
            if (token is JArray)
            {
                //Item is an array so create an array of dynamic models for the size of the list
                int arrayCount = ((JArray)token).Count;
                object[] array = new object[arrayCount];
                for (int i = 0; i < arrayCount; i++)
                {
                    //recursively create the dynamic model
                    array[i] = ProcessToken(((JArray)token)[i]);
                }
                return array;
            }

            //Normal dynamic model creation
            foreach (JProperty property in token)
            {
                //check if we are processing custom fields
                if (property.Name == ApiUtility.ApiCustomFieldName)
                {
                    ((ApiEntityModel)model).InitializeCustomFieldList();
                    //Build the custom field dynamic model entry
                    ((ApiEntityModel)model).CustomFieldList = new Dictionary<string, object>(RetrievePropertyValue(property) as IDictionary<string, object>);
                    continue;
                }

                //Set object value on the dynamic model
                ((ApiEntityModel)model)[property.Name] = RetrievePropertyValue(property);
            }

            return model;
        }

        private object RetrievePropertyValue(JProperty property)
        {
            //Check to see if the value of the property is an actual value or another object like an array
            if (!(property.Value is JValue))
                return ProcessToken(property.Value);  //Process child object

            return property.Value.ToObject<object>();  //Return property value
        }
        #endregion Private Methods

        #region Properties
        public override bool CanRead => true;

        public override bool CanWrite => false;
        #endregion Properties
    }
}