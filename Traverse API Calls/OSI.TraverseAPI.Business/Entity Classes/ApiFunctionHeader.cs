#region Using Directives
using System;
using System.ComponentModel;
using System.Linq;
using TRAVERSE.Business;
using TRAVERSE.Business.Validation;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public partial class ApiFunctionHeader
    {
        #region Methods
        protected virtual EntityList<ApiFunctionSchema> GetSchemaList()
        {
            if (_schemaList == null)
            {
                ApiFunctionSchemaProvider provider = new ApiFunctionSchemaProvider(this);
                SqlFilterBuilder<ApiFunctionSchemaBase.Columns> builder = new SqlFilterBuilder<ApiFunctionSchemaBase.Columns>();
                builder.AppendEquals(ApiFunctionSchemaBase.Columns.FunctionId, Id.ToString());

                SQLSortBuilder sorter = new SQLSortBuilder();
                sorter.Append(ApiFunctionSchemaBase.Columns.SeqNum);
                sorter.Append(ApiFunctionSchemaBase.Columns.Id);

                _schemaList = provider.Load(CompId, new FilterCriteria(builder.ToString(), sorter.ToString()));
                _schemaList.ListChanged += SchemaListChanged;
            }
            return _schemaList;
        }

        private void SchemaListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                SchemaList[e.NewIndex].Parent = this;
                SchemaList[e.NewIndex].SeqNum = NextSeqNumber;
            }
        }

        protected virtual bool ValidateIfNotNull(string propertyName)
        {
            switch (propertyName)
            {
                case "OverrideId":
                    return OverrideId != null && OverrideId != Guid.Empty;
                default:
                    return false;
            }
        }

        private int GetNextSeqNumber()
        {
            int maxNum = 0;
            if (SchemaList != null)
            {
                maxNum = SchemaList.Max(s => s.SeqNum.GetValueOrDefault());
            }

            return (maxNum / 10) * 10 + 10; //Get next number in intervals of 10; using math so that if user entered 22, next number is 30
        }
        #endregion Methods

        #region Overrides
        protected override void AddValidationRules()
        {
            base.AddValidationRules();
            base.ValidationRules.AddRule(CommonRules.StringRequired, Columns.Name.ToString());
            base.ValidationRules.AddRule(EntityRules.ForeignKeyRule<ApiFunctionHeader, ApiFunctionHeaderProvider>, new EntityRulesArgs(Columns.OverrideId.ToString(), CompId, Columns.Id, ValidateIfNotNull));
            base.ValidationRules.AddRule(CommonRules.InRange<byte>, new CommonRules.RangeRuleArgs<byte>(Columns.Scope.ToString(), new CommonRules.Range<byte>(0, 15)));
        }

        public override byte? Scope { get => base.Scope.GetValueOrDefault(); set => base.Scope = value; }
        #endregion Overrides

        #region Properties
        public virtual EntityList<ApiFunctionSchema> SchemaList { get => GetSchemaList(); }

        public int NextSeqNumber { get => GetNextSeqNumber(); }

        public ApiFunctionType FunctionType
        {
            get => (ApiFunctionType)Type;
            set => Type = (byte)value;
        }

        private ApiScope OptionsScope
        {
            get => (ApiScope)Scope;
        }

        [Bindable(true), Description]
        public bool AllowRead
        {
            get
            {
                return ((OptionsScope & ApiScope.AllowRead) == ApiScope.AllowRead);
            }
            set
            {
                if (value)
                {
                    Scope = (byte)(OptionsScope | ApiScope.AllowRead);
                }
                else
                {
                    Scope = (byte)(Scope.Value & 14);
                }
            }
        }

        [Bindable(true), Description]
        public bool AllowEdit
        {
            get
            {
                return ((OptionsScope & ApiScope.AllowEdit) == ApiScope.AllowEdit);
            }
            set
            {
                if (value)
                {
                    Scope = (byte)(OptionsScope | ApiScope.AllowEdit);
                }
                else
                {
                    Scope = (byte)(Scope.Value & 13);
                }
            }
        }

        [Bindable(true), Description]
        public bool AllowNew
        {
            get
            {
                return ((OptionsScope & ApiScope.AllowNew) == ApiScope.AllowNew);
            }
            set
            {
                if (value)
                {
                    Scope = (byte)(OptionsScope | ApiScope.AllowNew);
                }
                else
                {
                    Scope = (byte)(Scope.Value & 11);
                }
            }
        }

        [Bindable(true), Description]
        public bool AllowDelete
        {
            get
            {
                return ((OptionsScope & ApiScope.AllowDelete) == ApiScope.AllowDelete);
            }
            set
            {
                if (value)
                {
                    Scope = (byte)(OptionsScope | ApiScope.AllowDelete);
                }
                else
                {
                    Scope = (byte)(Scope.Value & 7);
                }
            }
        }

        public override byte? Type { get => base.Type.GetValueOrDefault(1); set => base.Type = value; }
        #endregion Properties

        #region Fields
        private EntityList<ApiFunctionSchema> _schemaList;
        #endregion Fields
    }
}
