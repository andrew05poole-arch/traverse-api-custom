namespace TraverseApi
{
    public sealed class ApiEntityPropertyChangingArgs
    {
        #region Constructors
        private ApiEntityPropertyChangingArgs()
        { }

        internal ApiEntityPropertyChangingArgs(object entity, string propertyName, object propertyValue)
        {
            Entity = entity;
            FieldName = propertyName;
            ActualValue = propertyValue;
        }
        #endregion Constructors

        #region Properties
        public object Entity { get; private set; }

        public string FieldName { get; private set; }

        public object ActualValue { get; set; }

        public bool Handled { get; set; }
        #endregion Properties
    }
}