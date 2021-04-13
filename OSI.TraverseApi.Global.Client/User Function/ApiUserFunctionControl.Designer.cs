namespace OSI.TraverseApi.Client
{
    partial class ApiUserFunctionControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _accessControl != null && _accessControl.BindingSource != null)
            {
                _accessControl.BindingSource.CurrentChanged -= AccessBindingSourceChanged;
            }
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitMain = new DevExpress.XtraEditors.SplitContainerControl();
            this.tabFunctionDetail = new TRAVERSE.Controls.TravTabControl();
            this.tabCompany = new System.Windows.Forms.TabPage();
            this.tabSchema = new System.Windows.Forms.TabPage();
            this.bindUser = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).BeginInit();
            this.splitContainerBase.Panel1.SuspendLayout();
            this.splitContainerBase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.SuspendLayout();
            this.tabFunctionDetail.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindUser)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainerBase
            // 
            // 
            // splitContainerBase.Panel1
            // 
            this.splitContainerBase.Panel1.Controls.Add(this.splitMain);
            // 
            // errorProviderBase
            // 
            this.errorProviderBase.DataSource = this.bindUser;
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Horizontal = false;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Name = "splitMain";
            this.splitMain.Panel1.Text = "Panel1";
            this.splitMain.Panel2.Controls.Add(this.tabFunctionDetail);
            this.splitMain.Panel2.Text = "Panel2";
            this.splitMain.Size = new System.Drawing.Size(800, 600);
            this.splitMain.SplitterPosition = 248;
            this.splitMain.TabIndex = 0;
            this.splitMain.Text = "splitContainerControl1";
            // 
            // tabFunctionDetail
            // 
            this.tabFunctionDetail.Controls.Add(this.tabCompany);
            this.tabFunctionDetail.Controls.Add(this.tabSchema);
            this.tabFunctionDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabFunctionDetail.Location = new System.Drawing.Point(0, 0);
            this.tabFunctionDetail.Name = "tabFunctionDetail";
            this.tabFunctionDetail.SelectedIndex = 0;
            this.tabFunctionDetail.Size = new System.Drawing.Size(800, 347);
            this.tabFunctionDetail.TabIndex = 0;
            // 
            // tabCompany
            // 
            this.tabCompany.Location = new System.Drawing.Point(4, 22);
            this.tabCompany.Name = "tabCompany";
            this.tabCompany.Padding = new System.Windows.Forms.Padding(3);
            this.tabCompany.Size = new System.Drawing.Size(792, 321);
            this.tabCompany.TabIndex = 0;
            this.tabCompany.Text = "Company Assignment";
            this.tabCompany.UseVisualStyleBackColor = true;
            // 
            // tabSchema
            // 
            this.tabSchema.Location = new System.Drawing.Point(4, 22);
            this.tabSchema.Name = "tabSchema";
            this.tabSchema.Padding = new System.Windows.Forms.Padding(3);
            this.tabSchema.Size = new System.Drawing.Size(792, 321);
            this.tabSchema.TabIndex = 1;
            this.tabSchema.Text = "Field Management";
            this.tabSchema.UseVisualStyleBackColor = true;
            // 
            // bindUser
            // 
            this.bindUser.AllowNew = false;
            this.bindUser.DataSource = typeof(OSI.TraverseApi.Business.ApiUser);
            // 
            // ApiUserFunctionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BindingSource = this.bindUser;
            this.HandleListChangedEvent = true;
            this.Name = "ApiUserFunctionControl";
            this.splitContainerBase.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBase)).EndInit();
            this.splitContainerBase.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.tabFunctionDetail.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.bindUser)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.SplitContainerControl splitMain;
        private TRAVERSE.Controls.TravTabControl tabFunctionDetail;
        private System.Windows.Forms.TabPage tabCompany;
        private System.Windows.Forms.TabPage tabSchema;
        private System.Windows.Forms.BindingSource bindUser;
    }
}
