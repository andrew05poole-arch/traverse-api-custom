using System.Windows.Forms;
using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public partial class ApiFunctionSchemaTranslateForm : BaseForm
    {
        #region Constructors
        public ApiFunctionSchemaTranslateForm()
        {
            InitializeComponent();
            this.BindingSource = this.bindTranslate;
        }
        #endregion Constructors

        #region Overrides
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.gvTranslate.PostEditor();
            base.OnFormClosing(e);
        }
        #endregion Overrides
    }
}
