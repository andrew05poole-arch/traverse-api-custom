using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public class ApiUserAccountPlugin : PluginBase
    {
        public override void Initialize()
        {
            this.MainInterface = new ApiUserAccountControl(this);
        }

        public override string Description { get => "User Account"; }
    }
}
