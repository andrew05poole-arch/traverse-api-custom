using System.ComponentModel;

namespace OSI.TraverseApi.Business
{
    public partial class ApiInfo
    {
        #region Properties
        [Bindable(true), Description]
        public string DatabaseName { get; internal set; }

        public ApiMsgEncryption MsgEncryptionType
        {
            get => (ApiMsgEncryption)this.MsgEncryption;
            set => this.MsgEncryption = (byte)value;
        }
        #endregion Properties

        #region Overrides
        public override int Id { get => 1; set { } }

        public override int? AuthorizeTimeout { get => base.AuthorizeTimeout.GetValueOrDefault(15); set => base.AuthorizeTimeout = value; }

        public override int? AccessExpireHours { get => base.AccessExpireHours.GetValueOrDefault(12); set => base.AccessExpireHours = value; }

        public override int? RefreshExpireDays { get => base.RefreshExpireDays.GetValueOrDefault(30); set => base.RefreshExpireDays = value; }

        public override bool? IsDebugLocalEnv { get => base.IsDebugLocalEnv.GetValueOrDefault(false); set => base.IsDebugLocalEnv = value; }

        public override bool? IgnoreInvalidField { get => base.IgnoreInvalidField.GetValueOrDefault(false); set => base.IgnoreInvalidField = value; }

        public override bool? UseMsgSecurity { get => base.UseMsgSecurity.GetValueOrDefault(false); set => base.UseMsgSecurity = value; }

        public override byte? MsgEncryption { get => base.MsgEncryption.GetValueOrDefault(0); set => base.MsgEncryption = value; }
        #endregion Overrides
    }
}
