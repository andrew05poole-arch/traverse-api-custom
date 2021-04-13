using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public class ApiFunctionPlugin : PluginBase
    {
        public override void Initialize()
        {
            this.MainInterface = new ApiFunctionControl(this);
        }
    }
}
