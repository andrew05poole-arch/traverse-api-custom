using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using TRAVERSE.Business.Validation;
using TRAVERSE.Core;

namespace OSI.TraverseApi.Business
{
    public partial class ApiFunctionSchema
    {
        #region Event Handlers
        private void ValueListChanged(object sender, ListChangedEventArgs e)
        {
            OnColumnChanging(Columns.ValueTranslation.ToString());
            _valueTranslation = null;
            OnColumnChanged(Columns.ValueTranslation.ToString());
            OnPropertyChanged(Columns.ValueTranslation.ToString());
        }
        #endregion Event Handlers

        #region Methods
        protected virtual bool ValidateIfNotNull(string propertyName)
        {
            switch (propertyName)
            {
                case "ChildFunctionId":
                    return ChildFunctionId != null && ChildFunctionId != Guid.Empty;
                default:
                    return false;
            }
        }

        private string SerializeValueList()
        {
            if (ValueList?.Count == 0)
                return null;

            XmlSerializer serializer = new XmlSerializer(typeof(List<ApiValueTranslate>));

            using (StringWriter writer = new StringWriter())
            {
                List<ApiValueTranslate> list = new List<ApiValueTranslate>(ValueList);
                serializer.Serialize(writer, list);
                return writer.ToString();
            }
        }

        private void DeserializeValueList(string translation)
        {
            if (string.IsNullOrWhiteSpace(translation))
                return;

            XmlSerializer serializer = new XmlSerializer(typeof(List<ApiValueTranslate>));

            using (StringReader reader = new StringReader(translation))
            {
                List<ApiValueTranslate> list = serializer.Deserialize(reader) as List<ApiValueTranslate>;
                _valueList = new BindingList<ApiValueTranslate>(list);
                _valueList.ListChanged += ValueListChanged;
            }
        }
        #endregion Methods

        #region Overrides
        protected override void AddValidationRules()
        {
            base.AddValidationRules();
            base.ValidationRules.AddRule(EntityRules.ForeignKeyRule<ApiFunctionHeader, ApiFunctionHeaderProvider>, new EntityRulesArgs(Columns.ChildFunctionId.ToString(), CompId, Columns.Id, ValidateIfNotNull));
            base.ValidationRules.AddRule(CommonRules.StringRequired, Columns.TravFieldName.ToString());
            base.ValidationRules.AddRule(CommonRules.StringRequired, Columns.ApiFieldName.ToString());
            base.ValidationRules.AddRule(CommonRules.InRange<byte>, new CommonRules.RangeRuleArgs<byte>(Columns.FieldSetting.ToString(), new CommonRules.Range<byte>(0, 127)));
        }

        public override string TravFieldName
        {
            get => base.TravFieldName;
            set
            {
                if (this.IsNew)
                    base.TravFieldName = value;
            }
        }

        public override string CompId { get => Parent != null ? Parent.CompId : base.CompId; set => base.CompId = value; }

        public override Guid? FunctionId { get => Parent != null ? Parent.Id : base.FunctionId; set => base.FunctionId = value; }

        public override string ValueTranslation { get => _valueTranslation ?? (_valueTranslation = this.SerializeValueList()); set => this.DeserializeValueList(value); }

        public override byte? FieldSetting { get => base.FieldSetting.GetValueOrDefault(); set => base.FieldSetting = value; }

        public override TransactionManager TransMan { get => Parent != null ? Parent.TransMan : base.TransMan; set => base.TransMan = value; }
        #endregion Overrides

        #region Properties
        [Bindable(true), Description]
        public BindingList<ApiValueTranslate> ValueList { get => _valueList ?? (_valueList = new BindingList<ApiValueTranslate>()); }

        [Bindable(true), Description]
        public ApiFieldSetting FieldScope
        {
            get => (ApiFieldSetting)FieldSetting.GetValueOrDefault();
            set => FieldSetting = (byte)value;
        }

        public ApiFunctionHeader Parent
        {
            get => _parent;
            set => ParentEntity = _parent = value;
        }
        #endregion Properties

        #region Fields
        private BindingList<ApiValueTranslate> _valueList;
        private string _valueTranslation;
        private ApiFunctionHeader _parent;
        #endregion Fields
    }
}
