using System.Collections.Generic;

namespace TRAVERSE.Web.API
{
    internal sealed class ApiDocModel
    {
        public ApiDocModel()
        { }

        public string FieldName { get; set; }

        public int MaxLength { get; set; }

        public string Type { get; set; }

        public Dictionary<string, string> Enumeration { get; set; }

        public string DefaultValue { get; set; }

        public List<ApiDocModel> ChildObject { get; set; }
    }
}