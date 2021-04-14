using System.Data;

namespace OSI.TraverseApi.Business
{
    internal partial class ApiFunctionSchemaProvider
    {
        #region Constructors
        public ApiFunctionSchemaProvider(ApiFunctionHeader parent)
            : base()
        {
            _parent = parent;
        }
        #endregion Constructors

        #region Methods
        public static void UpdateSchemaList(ApiFunctionHeader header)
        {
            ApiFunctionSchemaProvider provider = new ApiFunctionSchemaProvider(header);

            if (header.IsDeleted)
                header.SchemaList.RemoveAll();

            provider.Items.AddRange(header.SchemaList.ChangedItems);
            provider.Items.DeletedItems.AddRange(header.SchemaList.DeletedItems);

            provider.TransMan = header.TransMan;
            provider.Update(header.CompId, true);

            if (!provider.Items.IsDirty)
                header.SchemaList.DeletedItems.Clear();
        }
        #endregion Methods

        #region Overrides
        protected override void FillCustom(IDataReader reader, int index)
        {
            base.FillCustom(reader, index);
            Items[index].Parent = _parent;
        }
        #endregion Overrides

        #region Fields
        private ApiFunctionHeader _parent;
        #endregion Fields
    }
}
