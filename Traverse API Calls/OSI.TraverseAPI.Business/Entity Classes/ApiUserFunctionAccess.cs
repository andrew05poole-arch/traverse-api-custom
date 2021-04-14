namespace OSI.TraverseApi.Business
{
    public partial class ApiUserFunctionAccess
    {
        #region Overrides
        public override string CompId { get => Parent != null ? Parent.CompId : base.CompId; set => base.CompId = value; }

        public override long UserFunctionId { get => Parent != null ? Parent.Id : base.UserFunctionId; set => base.UserFunctionId = value; }

        public override long? AccessCount { get => base.AccessCount.GetValueOrDefault(); set => base.AccessCount = value; }
        #endregion Overrides

        #region Properties
        public ApiUserFunction Parent
        {
            get => _parent;
            set => _parent = value;
        }
        #endregion Properties

        #region Fields
        private ApiUserFunction _parent;
        #endregion Fields
    }
}
