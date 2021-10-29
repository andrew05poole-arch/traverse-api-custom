using OSI.TraverseApi.Business.Properties;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TRAVERSE.Business;

namespace OSI.TraverseApi.Business
{
    public class ApiGenerateScriptProcess : ProcessBase
    {
        #region Constructors
        public ApiGenerateScriptProcess()
            : this(string.Empty)
        { }

        public ApiGenerateScriptProcess(string compId)
            : this(compId, ProcessBase.GenerateProcessId())
        { }

        public ApiGenerateScriptProcess(string compId, string processId)
            : base(compId, processId)
        { }
        #endregion Constructors

        #region Methods
        protected virtual void BuildScriptContents()
        {
            double currentIndex = 0;
            double count = FunctionList.Count;
            foreach (ApiFunctionHeader function in FunctionList)
            {
                currentIndex++;
                this.RaiseStatus(string.Format("{0} ({1:0.00%})", function.Name, currentIndex / count));
                
                HeaderBuilder.AppendLine(
                    string.Format(Resources.FunctionHeaderScript,
                        SqlUtil.Encode(function.Id.ToString(), true),
                        SqlUtil.Encode(function.Name, true),
                        function.AppId == null ? SqlUtil.NULL : SqlUtil.Encode(function.AppId, true),
                        function.Type,
                        function.Notes == null ? SqlUtil.NULL : SqlUtil.Encode(function.Notes, true),
                        function.Scope,
                        function.OverrideId == null ? SqlUtil.NULL : SqlUtil.Encode(function.OverrideId.ToString())).Replace("'", "''")
                    );

                SchemaBuilder.AppendLine(
                    string.Format(Resources.FunctionSchemaDeleteScript, 
                        SqlUtil.Encode(function.Id.ToString(), true)).Replace("'", "''")
                    );

                foreach (ApiFunctionSchema schema in function.SchemaList)
                {
                    SchemaBuilder.AppendLine(
                        string.Format(Resources.FunctionSchemaScript,
                            schema.SeqNum,
                            SqlUtil.Encode(schema.FunctionId.ToString(), true),
                            SqlUtil.Encode(schema.TravFieldName, true),
                            SqlUtil.Encode(schema.ApiFieldName, true),
                            schema.ValueTranslation == null ? SqlUtil.NULL : SqlUtil.Encode(schema.ValueTranslation, true),
                            schema.Notes == null ? SqlUtil.NULL : SqlUtil.Encode(schema.Notes, true),
                            schema.FieldSetting,
                            schema.ChildFunctionId == null ? SqlUtil.NULL : SqlUtil.Encode(schema.ChildFunctionId.ToString(), true)).Replace("'", "''")
                        );
                }
            }
        }

        protected virtual void WriteHeaderFile()
        {
            string path = Path.Combine(FilePath, "ApiFunctionHeader.sql");
            var tableBuilder = new StringBuilder();

            tableBuilder.AppendLine(Resources.ApiFunctionHeaderTable);
            tableBuilder.AppendLine(string.Format(Resources.GenerateScript, HeaderBuilder.ToString()));
            File.WriteAllText(path, tableBuilder.ToString());
        }

        protected virtual void WriteSchemaFile()
        {
            string path = Path.Combine(FilePath, "ApiFunctionSchema.sql");
            var tableBuilder = new StringBuilder();

            tableBuilder.AppendLine(Resources.ApiFunctionSchemaTable);
            tableBuilder.AppendLine(string.Format(Resources.GenerateScript, SchemaBuilder.ToString()));
            File.WriteAllText(path, tableBuilder.ToString());
        }
        #endregion Methods

        #region Overrides
        public override void Execute(Status status)
        {
            try
            {
                this.ProcessStatus = status;
                this.RaiseStatus("Initializing");
                this.BuildScriptContents();
                this.WriteHeaderFile();
                this.WriteSchemaFile();
            }
            catch
            {
                throw;
            }
        }
        #endregion Overrides

        #region Properties
        public virtual string FilePath { get; set; }

        public virtual List<ApiFunctionHeader> FunctionList { get; } = new List<ApiFunctionHeader>();

        protected virtual StringBuilder HeaderBuilder
        {
            get
            {
                if (_headerBuilder == null)
                    _headerBuilder = new StringBuilder();
                return _headerBuilder;
            }
        }

        protected virtual StringBuilder SchemaBuilder
        {
            get
            {
                if (_schemaBuilder == null)
                    _schemaBuilder = new StringBuilder();
                return _schemaBuilder;
            }
        }
        #endregion Properties

        #region Fields
        private StringBuilder _headerBuilder;
        private StringBuilder _schemaBuilder;
        #endregion Fields
    }
}
