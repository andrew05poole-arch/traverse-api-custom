#region Using Directives
using System;
using System.Data;
using TRAVERSE.Business;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    internal partial class ApiUserFunctionProvider
    {
        #region Constructors
        public ApiUserFunctionProvider()
        { }

        public ApiUserFunctionProvider(ApiUser user)
            : this()
        {
            _parent = user;
        }
        #endregion Constructors

        #region Methods
        protected virtual void UpdateFunction(ApiUserFunction function)
        {
            ApiUserFunctionCompProvider.UpdateAccessCompList(function);
            ApiUserFunctionSchemaProvider.UpdateAccessSchemaList(function);
        }

        public static EntityList<ApiUserFunction> RetreiveFunctionList(ApiUser user)
        {
            if (user == null)
                return new EntityList<ApiUserFunction>();

            ApiUserFunctionProvider provider = new ApiUserFunctionProvider(user);
            SqlFilterBuilder<ApiUserFunctionBase.Columns> builder = new SqlFilterBuilder<ApiUserFunctionBase.Columns>();

            builder.AppendEquals(ApiUserFunctionBase.Columns.UserId, user.Id.ToString());
            provider.TransMan = user.TransMan;

            return provider.Load(user.CompId, new FilterCriteria(builder.ToString(), ""));
        }

        public static void Update(ApiUser user)
        {
            ApiUserFunctionProvider provider = new ApiUserFunctionProvider(user);

            if (user.IsDeleted)
                user.FunctionList.RemoveAll();

            provider.Items.AddRange(user.FunctionList.ChangedItems);
            provider.Items.DeletedItems.AddRange(user.FunctionList.DeletedItems);

            provider.TransMan = user.TransMan;
            provider.Update(user.CompId, true);

            if (!provider.Items.IsDirty)
                user.FunctionList.DeletedItems.Clear();
        }
        #endregion Methods

        #region Overrides
        protected override void FillCustom(IDataReader reader, int index)
        {
            base.FillCustom(reader, index);
            Items[index].Parent = _parent;
        }

        protected override void Update(ApiUserFunction entity, bool throwException)
        {
            if (entity.EntityState != EntityState.Deleted)
            {
                entity.SuppressEntityEvents = true;
                if (entity.IsDirty)
                {
                    entity.DateModified = DateTime.Now;
                    entity.ModifiedBy = ApplicationContext.CurrentUser;
                }

                if (entity.IsNew)
                    entity.DateCreated = entity.DateModified;
                entity.SuppressEntityEvents = false;
            }

            entity.SkipLookupValidation = true;
            base.Update(entity, throwException);
            UpdateFunction(entity);
            entity.SkipLookupValidation = false;

            if (entity.EntityState != EntityState.Deleted && entity.IsDirty)
                base.Update(entity, throwException);
        }
        #endregion Overrides

        #region Fields
        private ApiUser _parent;
        #endregion Fields
    }
}
