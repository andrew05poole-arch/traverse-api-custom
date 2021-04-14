#region Using Directives
using System;
using System.Data;
using TRAVERSE.Business;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    internal partial class ApiUserFunctionCompProvider
    {
        #region Constructors
        private ApiUserFunctionCompProvider()
        { }

        internal ApiUserFunctionCompProvider(ApiUserFunction parent)
            : this()
        {
            _parent = parent;
        }
        #endregion Constructors

        #region Methods
        public static EntityList<ApiUserFunctionComp> GetFunctionCompanyList(ApiUserFunction function)
        {
            if (function == null)
                return new EntityList<ApiUserFunctionComp>();

            ApiUserFunctionCompProvider provider = new ApiUserFunctionCompProvider(function);
            SqlFilterBuilder<ApiUserFunctionCompBase.Columns> builder = new SqlFilterBuilder<ApiUserFunctionCompBase.Columns>();

            builder.AppendEquals(ApiUserFunctionCompBase.Columns.UserFunctionId, function.Id.ToString());
            provider.TransMan = function.TransMan;

            return provider.Load(function.CompId, new FilterCriteria(builder.ToString(), ""));
        }

        public static void UpdateAccessCompList(ApiUserFunction function)
        {
            ApiUserFunctionCompProvider provider = new ApiUserFunctionCompProvider(function);

            if (function.IsDeleted)
                function.CompanyList.RemoveAll();

            provider.Items.AddRange(function.CompanyList.ChangedItems);
            provider.Items.DeletedItems.AddRange(function.CompanyList.DeletedItems);

            provider.TransMan = function.TransMan;
            provider.Update(function.CompId, true);

            if (!provider.Items.IsDirty)
                function.CompanyList.DeletedItems.Clear();
        }
        #endregion Methods

        #region Overrides
        protected override void FillCustom(IDataReader reader, int index)
        {
            base.FillCustom(reader, index);
            Items[index].Parent = _parent;
        }

        protected override void Update(ApiUserFunctionComp entity, bool throwException)
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
            entity.SkipLookupValidation = false;
        }
        #endregion Overrides

        #region Fields
        private ApiUserFunction _parent;
        #endregion Fields
    }
}
