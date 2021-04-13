using System.Drawing;
using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public class ApiBuildMaintScriptPlugin : PluginBase
    {
        public override void Initialize()
        {
            this.MainInterface = new ApiBuildMaintScriptControl(this);
        }

        public override Size PopupSize => new Size(1024, 768);

        public override bool Popup => true;
    }
}
