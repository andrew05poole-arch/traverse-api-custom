#region Using Directives
using System;
using System.Data;
using TRAVERSE.Business;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    internal partial class ApiUserFunctionSchemaProvider
    {
        #region Constructors
        internal ApiUserFunctionSchemaProvider()
        { }

        internal ApiUserFunctionSchemaProvider(ApiUserFunction parent)
            : this()
        {
            _parent = parent;
        }
        #endregion Constructors

        #region Methods
        public static EntityList<ApiUserFunctionSchema> GetFunctionSchemaList(ApiUserFunction function)
        {
            if (function == null)
                return new EntityList<ApiUserFunctionSchema>();

            ApiUserFunctionSchemaProvider provider = new ApiUserFunctionSchemaProvider(function);
            SqlFilterBuilder<ApiUserFunctionSchemaBase.Columns> builder = new SqlFilterBuilder<ApiUserFunctionSchemaBase.Columns>();

            builder.AppendEquals(ApiUserFunctionSchemaBase.Columns.UserFunctionId, function.Id.ToString());
            provider.TransMan = function.TransMan;

            return provider.Load(function.CompId, new FilterCriteria(builder.ToString(), ""));
        }

        public static void UpdateAccessSchemaList(ApiUserFunction function)
        {
            ApiUserFunctionSchemaProvider provider = new ApiUserFunctionSchemaProvider(function);

            if (function.IsDeleted)
                function.SchemaList.RemoveAll();

            provider.Items.AddRange(function.SchemaList.ChangedItems);
            provider.Items.DeletedItems.AddRange(function.SchemaList.DeletedItems);

            provider.TransMan = function.TransMan;
            provider.Update(function.CompId, true);

            if (!provider.Items.IsDirty)
                function.SchemaList.DeletedItems.Clear();
        }
        #endregion Methods

        #region Overrides
        protected override void FillCustom(IDataReader reader, int index)
        {
            base.FillCustom(reader, index);
            Items[index].Parent = _parent;
        }

        protected override void Update(ApiUserFunctionSchema entity, bool throwException)
        {
            if (entity.EntityState != EntityState.Deleted)
            {
                if (entity.IsDirty)
                {
                    entity.DateModified = DateTime.Now;
                    entity.ModifiedBy = ApplicationContext.CurrentUser;
                }

                if (entity.IsNew)
                    entity.DateCreated = entity.DateModified;
            }

            base.Update(entity, throwException);
        }
        #endregion Overrides

        #region Fields
        private ApiUserFunction _parent;
        #endregion Fields
    }
}
