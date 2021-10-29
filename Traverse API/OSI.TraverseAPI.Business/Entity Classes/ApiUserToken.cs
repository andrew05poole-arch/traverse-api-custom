using System.Threading.Tasks;

namespace OSI.TraverseApi.Business
{
    public partial class ApiUserToken
    {
        #region Methods
        public async Task SaveAsync()
        {
            ApiUserTokenProvider provider = new ApiUserTokenProvider();
            provider.Items.Add(this);
            await Task.Run(() => provider.Update(CompId));
        }
        #endregion Methods

        #region Properties
        public override long UserId { get => Parent != null ? Parent.Id : base.UserId; set => base.UserId = value; }

        public string TokenId { get; set; }

        public ApiUser Parent { get; set; }
        #endregion Properties
    }
}
