namespace TRAVERSE.Web.API
{
    public delegate object ChildRecordHandler(ApiChildRecordArgs args);

    public sealed class ApiChildRecordArgs
    {
        private ApiChildRecordArgs() { }

        internal ApiChildRecordArgs(object parent, string propertyName, ApiEntityModel model)
        {
            ParentObject = parent;
            PropertyName = propertyName;
            ItemModel = model;
        }

        /// <summary>
        /// This is the parent of the child property being requested. This is helpful in the event that any items are needed from the parent in the creation of the child. This property is readonly.
        /// </summary>
        public object ParentObject { get; private set; }

        /// <summary>
        /// This property is the name of the child property being requested from the Function List. This property is readonly.
        /// </summary>
        public string PropertyName { get; private set; }

        /// <summary>
        /// This is the section of data in the original request that corresponds with the child record being processed. This property is readonly.
        /// </summary>
        public dynamic ItemModel { get; private set; }

        /// <summary>
        /// Use this field when skipping the child request is necessary. Set value to true and return null or any value and the process will skip and proceed with the next property in the list.
        /// </summary>
        public bool Ignore { get; set; }
    }
}