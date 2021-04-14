using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Core;

namespace OSI.TraverseApi.Business
{
    public partial class ApiUserProvider
    {
        #region Constructors
        public ApiUserProvider()
            : base()
        { }
        #endregion Constructors

        #region Methods
        protected virtual void UpdateUserDetail(ApiUser user)
        {
            ApiUserFunctionProvider.Update(user);
        }

        public static ApiUser ValidateClient(string companyId, string clientId, string clientSecret)
        {
            ApiUserProvider provider = new ApiUserProvider();
            SqlFilterBuilder<ApiUserBase.Columns> builder = new SqlFilterBuilder<ApiUserBase.Columns>();
            builder.AppendEquals(ApiUserBase.Columns.ClientId, clientId);
            builder.AppendEquals(ApiUserBase.Columns.ClientSecret, clientSecret);

            provider.Load(companyId, new FilterCriteria(builder.ToString(), ""));
            if (provider.Items.Count > 0 && (provider.Items[0].ExpirationDate.GetValueOrDefault(DateTime.Now) - DateTime.Now).Days >= 0)
                return provider.Items[0];

            return null;
        }

        public static ApiUser GetUser(string companyId, string username, string password)
        {
            ApiUser user = null;
            ApiUserProvider provider = new ApiUserProvider();
            SqlFilterBuilder<ApiUserBase.Columns> builder = new SqlFilterBuilder<ApiUserBase.Columns>();
            builder.AppendEquals(ApiUserBase.Columns.EmailAddress, username);
            builder.AppendEquals(ApiUserBase.Columns.Password, DataSecurity.EncryptData(password));

            provider.Load(companyId, new FilterCriteria(builder.ToString(), ""));
            if (provider.Items.Count > 0)
                user = provider.Items[0];

            return user;
        }

        public static ApiUser GetUser(string companyId, string id)
        {
            ApiUser user = null;
            ApiUserProvider provider = new ApiUserProvider();
            SqlFilterBuilder<ApiUserBase.Columns> builder = new SqlFilterBuilder<ApiUserBase.Columns>();
            builder.AppendEquals(ApiUserBase.Columns.Id, id);

            provider.Load(companyId, new FilterCriteria(builder.ToString(), ""));
            if (provider.Items.Count > 0)
                user = provider.Items[0];

            return user;
        }

        public static void UpdateUser(ApiUser user)
        {
            if (user == null)
                return;

            lock (_lockObject)
            {
                ApiUserProvider provider = new ApiUserProvider();
                provider.TransMan = user.TransMan;
                provider.Items.Add(user);
                provider.Update(user.CompId);
            }
        }

        public static async Task<ApiUser> ValidateClientAsync(string companyId, string clientId, string clientSecret)
        {
            return await Task.Run(() => ValidateClient(companyId, clientId, clientSecret));
        }

        public static async Task<List<ApiUserInfo>> GetUserListForWeb(string compId)
        {
            var list = new List<ApiUserInfo>();

            await Task.Run(() =>
            {
                var provider = new ApiUserProvider();
                provider.Load(compId);

                foreach (var user in provider.Items)
                {
                    var info = new ApiUserInfo();

                    info.email_address = user.EmailAddress;
                    info.expiration_date = user.ExpirationDate;
                    info.name = user.Name;
                    info.status = user.UserStatus.ToString();
                    info.last_access = user.LastAccess;
                    info.last_function = user.LastFunction;
                    info.num_assigned_functions = user.FunctionList.Count;

                    list.Add(info);
                }
            });

            return list;
        }
        #endregion Methods

        #region Overrides
        protected override void Update(ApiUser entity, bool throwException)
        {
            UpdateUserDetail(entity);
            base.Update(entity, throwException);
        }
        #endregion Overrides

        #region Fields
        private static object _lockObject = new object();
        #endregion Fields
    }
}
