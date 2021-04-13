using OSI.TraverseApi.Business;
using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public partial class ApiUserMultiFunctionForm : BaseForm
    {
        public ApiUserMultiFunctionForm(ApiUser user)
            : base()
        {
            InitializeComponent();
            this.FunctionControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FunctionControl.CurrentUser = user;
            this.Controls.Add(FunctionControl);
        }

        public virtual ApiUserMultiFunctionControl FunctionControl { get; } = new ApiUserMultiFunctionControl();
    }
}
