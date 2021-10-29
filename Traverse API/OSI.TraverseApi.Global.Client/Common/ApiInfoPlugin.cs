using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public class ApiInfoPlugin : PluginBase
    {
        public override void Initialize()
        {
            this.MainInterface = new ApiInfoControl(this);
        }
    }
}
