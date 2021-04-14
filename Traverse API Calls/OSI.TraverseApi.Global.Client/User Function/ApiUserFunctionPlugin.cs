using System.Drawing;
using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public class ApiUserFunctionPlugin : PluginBase
    {
        public override void Initialize()
        {
            MainInterface = new ApiUserFunctionControl(this);
        }

        public override Size PopupSize => new Size(1024, 768);

        public override bool Popup => true;
    }
}
