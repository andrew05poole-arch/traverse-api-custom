#region Using Directives
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Business.Validation;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public partial class ApiUserFunctionComp
    {
        #region Methods
        protected virtual bool ValidateUserFunction(string propertyName, ref string errorDescription)
        {
            if (propertyName == Columns.UserFunctionId.ToString())
            {
                if (!SkipLookupValidation && (this.Parent == null || this.Parent.FunctionInfo == null))
                {
                    errorDescription = "Invalid function.";
                    return false;
                }
                return true;
            }
            return false;
        }

        protected virtual List<ApiEntitySchema> LoadEntitySchema()
        {
            if (_entitySchema == null)
            {
                _entitySchema = ApiEntitySchema.GetSchema(this);
            }
            return _entitySchema;
        }

        public ApiUserFunctionComp GetChildFunctionDef(Guid childFunctionId)
        {
            ApiUserFunctionComp function = null;
            ApiUserFunction userFunction = this.Parent?.Parent.FunctionList.Find(ApiUserFunctionBase.Columns.FunctionId, childFunctionId);
            if (userFunction != null)
                function = userFunction.CompanyList.Find(ApiUserFunctionCompBase.Columns.CompanyId, this.CompanyId);

            if (function == null)
            {
                if (userFunction == null)
                {
                    userFunction = new ApiUserFunction()
                    {
                        Parent = this.Parent.Parent,
                        FunctionId = childFunctionId
                    };
                }
                function = new ApiUserFunctionComp()
                {
                    CompanyId = this.CompanyId,
                    Parent = userFunction,
                    Scope = userFunction.FunctionInfo.Scope
                };
            }

            return function;
        }

        public async Task<bool> FilterEntityList(IList entityList)
        {
            bool valid = true;

            await Task.Run(() =>
            {
                //Only process this if we have a filter and a non-empty list
                if (!string.IsNullOrWhiteSpace(this.Filter) && entityList != null && entityList.Count > 0)
                {
                    var changeItems = entityList.GetType().GetProperty("ChangedItems");
                    IList list = (changeItems != null && changeItems.GetValue(entityList) is IList && ((IList)changeItems.GetValue(entityList)).Count > 0) ? ((IList)changeItems.GetValue(entityList)) : entityList;

                    //If this is an EntityList, reset the RaiseListChangedEvents to false as to not trigger any unintended Traverse business layer effects
                    var eventItem = entityList.GetType().GetProperty("RaiseListChangedEvents");
                    if (eventItem != null)
                    {
                        if ((bool)eventItem.GetValue(entityList))
                            eventItem.SetValue(entityList, false);
                        else
                            eventItem = null;
                    }

                    try
                    {
                        //Build our criteria and an evaluator to apply to entities later
                        CriteriaOperator criteria = CriteriaOperator.Parse(this.Filter, new object[0]);
                        var evaluator = new ExpressionEvaluator(
                                new EvaluatorContextDescriptorDefault(entityList[0].GetType()), criteria);

                        //If this is an EntityList, load the DeletedItems list so that we can remove our record and not accidentally delete it if it does get persisted
                        var delItem = entityList.GetType().GetProperty("DeletedItems");
                        IList delList = (delItem != null && delItem.GetValue(entityList) is IList) ? ((IList)delItem.GetValue(entityList)) : null;

                        //Peruse the list and see if we have anything that did not match the filter. If so, remove that item from the list and also the deleted items, if present
                        for (int i = 0; i < entityList.Count; i++)
                        {
                            //Let's grab the next object to work with
                            var entity = entityList[i];

                            //Let's see if the object passes validation
                            bool success = (bool)evaluator.Evaluate(entity);
                            if (!success)
                            {
                                //check to see if this item is in the changed items list on an entity list
                                bool changedItem = list.Contains(entity);
                                entityList.Remove(entity);

                                if (delList != null)
                                    delList.Remove(entity);
                                i--;
                                valid &= !changedItem;  //if this item was in the changed items list, it means the user tried to update it so validation fails
                            }
                        }
                    }
                    finally
                    {
                        //If this is an EntityList, restore the RaiseListChangedEvents setting
                        if (eventItem != null)
                            eventItem.SetValue(entityList, true);
                    }
                }
            });

            return valid;
        }

        public string BuildSqlTableCommand()
        {
            if (string.IsNullOrWhiteSpace(this.Parent.FunctionInfo.QueryTableName))
                throw new Exception(string.Format("{0} does not support data query", this.Parent.FunctionInfo.Name));

            string tableName = this.Parent.FunctionInfo.QueryTableName.Trim();
            string command = "(SELECT ";
            bool addComma = false;

            var tblCols = GetTableColumns(tableName);
            foreach (var field in this.EntitySchemaList)
            {
                if (field.Hidden || (field.FieldAccess & ApiFieldSetting.Read) != ApiFieldSetting.Read || (field.ChildFunction != null && field.ChildFunction != Guid.Empty) || string.IsNullOrWhiteSpace(field.QueryColumnName))
                    continue;

                string sourceColumn = field.QueryColumnName;
                if (!sourceColumn.Contains(" "))
                {
                    if (!sourceColumn.StartsWith("[") && !sourceColumn.EndsWith("]"))
                    {
                        sourceColumn = string.Format("[{0}]", sourceColumn.Replace("[", "[[").Replace("]", "]]"));
                    }
                    if (!tblCols.Exists(c => string.Equals(c, sourceColumn, StringComparison.OrdinalIgnoreCase)) && field.FieldType == AccessFieldType.Entity)
                        continue;
                }

                if (addComma)
                    command += ", ";
                else
                    addComma = true;

                command += string.Format("{0} As [{1}]", sourceColumn, field.ApiFieldName.Replace("[", "[[").Replace("]", "]]"));
            }

            foreach (string sourceColumn in tblCols.FindAll(c => c.StartsWith("[cf_") && this.EntitySchemaList.Find(e => c.Equals(e.QueryColumnName, StringComparison.OrdinalIgnoreCase)) == null))
            {
                if (addComma)
                    command += ", ";
                else
                    addComma = true;

                command += string.Format("{0} As {0}", sourceColumn);
            }

            if (tableName.StartsWith("["))
                tableName = tableName.Substring(1);

            if (tableName.EndsWith("]"))
                tableName = tableName.Substring(0, tableName.IndexOf(']'));

            if (tableName.IndexOf(' ') > 0 && tableName.IndexOf(' ') < tableName.Length)
                tableName = string.Format("({0}) x", tableName);
            else
                tableName = string.Format("[{0}]", tableName.Replace("[", "[[").Replace("]", "]]"));

            command += string.Format(" FROM {0} {1})", tableName, string.IsNullOrEmpty(this.Filter) ? string.Empty : string.Format("WHERE {0}", this.Filter));

            return command;
        }

        private List<string> GetTableColumns(string tableName)
        {
            List<string> columns = new List<string>();

            try
            {
                string validQuery = tableName.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase) ? tableName : string.Format("SELECT * FROM {0}", tableName);
                string query = string.Format("SELECT DISTINCT [name] FROM sys.dm_exec_describe_first_result_set('{0}', NULL, 0) ;", validQuery);
                var set = EntityProvider.ExecuteCommand(query, this.CompanyId, null);
                if (set != null && set.Tables.Count == 1 && set.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in set.Tables[0].Rows)
                    {
                        string colName = row.Field<string>(0);
                        if (string.IsNullOrWhiteSpace(colName))
                            continue;

                        columns.Add(string.Format("[{0}]", row.Field<string>(0).Replace("[", "[[").Replace("]", "]]")));
                    }
                }
            }
            catch
            { }
            return columns;
        }
        #endregion Methods

        #region Overrides
        protected override void AddValidationRules()
        {
            base.AddValidationRules();
            base.ValidationRules.AddRule(CommonRules.NotNull, Columns.UserFunctionId.ToString());
            base.ValidationRules.AddRule(CommonRules.StringRequired, Columns.CompanyId.ToString());
            base.ValidationRules.AddRule(EntityRules.ValidEntityRule, new EntityRulesArgs(Columns.UserFunctionId.ToString(), ValidateUserFunction));
            base.ValidationRules.AddRule(CommonRules.InRange<byte>, new CommonRules.RangeRuleArgs<byte>(Columns.Scope.ToString(), new CommonRules.Range<byte>(1, 15)));
        }
        #endregion Overrides

        #region Properties
        public List<ApiEntitySchema> EntitySchemaList { get => LoadEntitySchema(); }

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
                    if (Parent == null || Parent.FunctionInfo == null || this.Parent.FunctionInfo.AllowRead)
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
                    if (Parent == null || Parent.FunctionInfo == null || this.Parent.FunctionInfo.AllowEdit)
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
                    if (Parent == null || Parent.FunctionInfo == null || this.Parent.FunctionInfo.AllowNew)
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
                    if (Parent == null || Parent.FunctionInfo == null || this.Parent.FunctionInfo.AllowDelete)
                        Scope = (byte)(OptionsScope | ApiScope.AllowDelete);
                }
                else
                {
                    Scope = (byte)(Scope.Value & 7);
                }
            }
        }

        public ApiUserFunction Parent
        {
            get => _parent;
            set => ParentEntity = _parent = value;
        }

        public override string CompId { get => Parent != null ? Parent.CompId : base.CompId; set => base.CompId = value; }

        public override long? UserFunctionId { get => Parent?.Id ?? base.UserFunctionId; set => base.UserFunctionId = value; }

        public override TransactionManager TransMan { get => Parent != null ? Parent.TransMan : base.TransMan; set => base.TransMan = value; }

        public override byte? Scope
        {
            get => base.Scope.GetValueOrDefault();
            set
            {
                base.Scope = value;
                RaisePropertyChanged("AllowRead");
                RaisePropertyChanged("AllowEdit");
                RaisePropertyChanged("AllowNew");
                RaisePropertyChanged("AllowDelete");
            }
        }

        internal bool SkipLookupValidation { get; set; }
        #endregion Properties

        #region Fields
        private ApiUserFunction _parent;
        private List<ApiEntitySchema> _entitySchema;
        #endregion Fields
    }
}
