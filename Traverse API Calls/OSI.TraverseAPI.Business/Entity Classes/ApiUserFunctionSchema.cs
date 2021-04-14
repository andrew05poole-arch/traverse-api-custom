#region Using Directives
using TRAVERSE.Business.Validation;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public partial class ApiUserFunctionSchema
    {
        #region Methods
        protected virtual bool ValidateUserFunction(string propertyName, ref string errorDescription)
        {
            if (propertyName == Columns.UserFunctionId.ToString())
            {
                if (this.Parent == null || this.Parent.FunctionInfo == null)
                {
                    errorDescription = "Invalid function.";
                    return false;
                }
                return true;
            }
            return false;
        }

        protected virtual bool ValidateFieldName(string propertyName, ref string errorDescription)
        {
            if (propertyName == Columns.FunctionSchemaId.ToString())
            {
                if (this.FunctionSchemaId != 0 && this.AccessFieldType == AccessFieldType.Entity && this.Parent.SchemaList.Exists(s => s.AccessFieldType == AccessFieldType.Entity && s.Id != this.Id && s.FunctionSchemaId == this.FunctionSchemaId))
                {
                    errorDescription = "Entity Field is a duplicate and will create ambiguity";
                    return false;
                }
                return true;
            }

            if (propertyName == Columns.CustomFieldName.ToString())
            {
                if (!string.IsNullOrEmpty(this.CustomFieldName) && this.AccessFieldType == AccessFieldType.Custom && this.Parent.SchemaList.Exists(s => s.AccessFieldType == AccessFieldType.Custom && s.Id != this.Id && s.CustomFieldName == this.CustomFieldName))
                {
                    errorDescription = "Custom Field is a duplicate and will create ambiguity";
                    return false;
                }
                return true;
            }

            if (propertyName == Columns.ApiFieldName.ToString())
            {
                if (string.IsNullOrEmpty(this.ApiFieldName))
                    return true;

                foreach (ApiUserFunctionSchema schema in this.Parent.SchemaList)
                {
                    if (schema.Id == this.Id)
                        continue;

                    if (this.AccessFieldType != schema.AccessFieldType)
                        continue;

                    ApiFunctionSchema item = this.AccessFieldType == AccessFieldType.Custom ? null : this.Parent.FunctionInfo.SchemaList.Find(s => s.Id == this.FunctionSchemaId);
                    if ((item != null && item.ApiFieldName == this.ApiFieldName) || (item == null && schema.ApiFieldName == this.ApiFieldName))
                    {
                        errorDescription = "Api Field Name is ambiguous and refers to more than one field";
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        protected virtual bool ValidateSchema(string propertyName, ref string errorDescription)
        {
            if (propertyName == Columns.FunctionSchemaId.ToString())
            {
                if (this.FunctionSchemaId != 0 && this.AccessFieldType == AccessFieldType.Entity)
                {
                    if (this.Parent.SchemaList.Exists(s => s.AccessFieldType == AccessFieldType.Entity && s.Id != this.Id && s.FunctionSchemaId == this.FunctionSchemaId))
                    {
                        errorDescription = "Field is a duplicate and will create ambiguity";
                        return false;
                    }

                    if (this.Parent == null || this.Parent.FunctionInfo == null || !this.Parent.FunctionInfo.SchemaList.Exists(s => s.Id == this.FunctionSchemaId.GetValueOrDefault()))
                    {
                        errorDescription = "Field is not a valid option for this function";
                        return false;
                    }
                }

                return true;
            }
            return false;
        }

        protected virtual bool ValidateApiFieldRequired(string propertyName)
        {
            if (propertyName == Columns.ApiFieldName.ToString() && !this.Hidden.GetValueOrDefault())
            {
                return true;
            }
            return false;
        }

        protected virtual bool ValidateCustomFieldRequired(string propertyName)
        {
            if (propertyName == Columns.CustomFieldName.ToString() && this.AccessFieldType == AccessFieldType.Custom)
            {
                return true;
            }
            return false;
        }

        protected virtual bool ValidateTraverseFieldRequired(string propertyName)
        {
            if (propertyName == Columns.FunctionSchemaId.ToString() && this.AccessFieldType == AccessFieldType.Entity)
            {
                return true;
            }
            return false;
        }
        #endregion Methods

        #region Overrides
        protected override void AddValidationRules()
        {
            base.AddValidationRules();
            base.ValidationRules.AddRule(CommonRules.NotNull, Columns.UserFunctionId.ToString());
            base.ValidationRules.AddRule(EntityRules.ValidEntityRule, new EntityRulesArgs(Columns.UserFunctionId.ToString(), ValidateUserFunction));

            base.ValidationRules.AddRule(CommonRules.NotNull, new ValidationRuleArgs(Columns.ApiFieldName.ToString(), new ConditionalRuleHandler(ValidateApiFieldRequired)));
            base.ValidationRules.AddRule(EntityRules.ValidEntityRule, new EntityRulesArgs(Columns.ApiFieldName.ToString(), new EntityRuleHandler(ValidateFieldName)));

            base.ValidationRules.AddRule(CommonRules.NotNull, new ValidationRuleArgs(Columns.CustomFieldName.ToString(), new ConditionalRuleHandler(ValidateCustomFieldRequired)));
            base.ValidationRules.AddRule(EntityRules.ValidEntityRule, new EntityRulesArgs(Columns.CustomFieldName.ToString(), new EntityRuleHandler(ValidateFieldName)));

            base.ValidationRules.AddRule(CommonRules.NotNull, new ValidationRuleArgs(Columns.FunctionSchemaId.ToString(), new ConditionalRuleHandler(ValidateTraverseFieldRequired)));
            base.ValidationRules.AddRule(EntityRules.ValidEntityRule, new EntityRulesArgs(Columns.FunctionSchemaId.ToString(), new EntityRuleHandler(ValidateSchema)));
        }
        #endregion Overrides

        #region Properties
        public ApiUserFunction Parent
        {
            get => _parent;
            set => ParentEntity = _parent = value;
        }

        public AccessFieldType AccessFieldType
        {
            get => (AccessFieldType)this.FieldType.GetValueOrDefault();
            set => this.FieldType = (byte)value;
        }

        public override string CompId { get => Parent != null ? Parent.CompId : base.CompId; set => base.CompId = value; }

        public override bool? Hidden { get => base.Hidden.GetValueOrDefault(); set => base.Hidden = value; }

        public override long? UserFunctionId { get => Parent?.Id ?? base.UserFunctionId; set => base.UserFunctionId = value; }

        public override byte? FieldType { get => base.FieldType.GetValueOrDefault(); set => base.FieldType = value; }

        public override long? FunctionSchemaId
        {
            get
            {
                if (AccessFieldType == AccessFieldType.Entity)
                    return base.FunctionSchemaId;
                return null;
            }
            set
            {
                if (AccessFieldType == AccessFieldType.Entity)
                    base.FunctionSchemaId = value;
            }
        }

        public override string CustomFieldName
        {
            get
            {
                if (AccessFieldType == AccessFieldType.Custom)
                    return base.CustomFieldName;
                return null;
            }
            set
            {
                if (AccessFieldType == AccessFieldType.Custom)
                    base.CustomFieldName = value;
            }
        }

        public override TransactionManager TransMan { get => Parent != null ? Parent.TransMan : base.TransMan; set => base.TransMan = value; }

        internal bool SkipLookupValidation { get; set; }
        #endregion Properties

        #region Fields
        private ApiUserFunction _parent;
        #endregion Fields
    }
}
