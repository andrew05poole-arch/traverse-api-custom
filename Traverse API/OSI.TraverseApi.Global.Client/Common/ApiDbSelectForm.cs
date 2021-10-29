using OSI.TraverseApi.Business;
using System;
using TRAVERSE.Client;

namespace OSI.TraverseApi.Client
{
    public partial class ApiDbSelectForm : HostForm
    {
        private ApiDbSelectForm()
            : base()
        {
            InitializeComponent();
        }

        public static void SelectCompany()
        {
            if (ApiUtility.ApiDbList.Count > 1)
            {
                using (ApiDbSelectForm form = new ApiDbSelectForm())
                {
                    form.lstDatabase.Items.Clear();
                    foreach (string company in ApiUtility.ApiDbList)
                    {
                        form.lstDatabase.Items.Add(company);
                    }

                    form.lstDatabase.SelectedItem = ApiUtility.CurrentApiDb;

                    form.ShowDialog(ClientContext.HostShell as System.Windows.Forms.IWin32Window);
                }
            }
        }

        private void OKButtonClicked(object sender, EventArgs e)
        {
            ApiUtility.CurrentApiDb = lstDatabase.SelectedItem as string;
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
