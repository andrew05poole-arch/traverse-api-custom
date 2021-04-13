using System.Data;
using TRAVERSE.Business;
using TRAVERSE.Core;

namespace OSI.TraverseApi.Business
{
    internal partial class ApiUserTokenProvider
    {
        #region Constructors
        public ApiUserTokenProvider()
        { }

        public ApiUserTokenProvider(ApiUser parent)
            : this()
        {
            _parent = parent;
        }
        #endregion Constructors

        #region Methods
        public static ApiUserToken RetrieveUserToken(ApiUser user)
        {
            lock (_lockObject)
            {
                ApiUserTokenProvider provider = new ApiUserTokenProvider(user);
                SqlFilterBuilder<ApiUserTokenBase.Columns> builder = new SqlFilterBuilder<ApiUserTokenBase.Columns>();
                builder.AppendEquals(ApiUserTokenBase.Columns.UserId, user.Id.ToString());

                provider.Load(user.CompId, new FilterCriteria(builder.ToString(), ""));
                if (provider.Items.Count > 0)
                    return provider.Items[0];

                return new ApiUserToken(user.CompId) { Parent = user };
            }
        }

        protected override void FillCustom(IDataReader reader, int index)
        {
            base.FillCustom(reader, index);
            Items[index].Parent = _parent;
        }
        #endregion Methods

        #region Fields
        private ApiUser _parent;
        private static object _lockObject = new object();
        #endregion Fields
    }
}
