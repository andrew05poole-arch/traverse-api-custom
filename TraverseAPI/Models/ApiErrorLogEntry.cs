#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics;
#endregion Using Directives

namespace TraverseApi
{
    [Serializable]
    public sealed class ApiErrorLogEntry
    {
        #region Properties
        public string UserEmail { get; set; }

        public DateTime Date { get; set; }

        public string HttpMethod { get; set; }

        public string Endpoint { get; set; }

        public string ErrorMessage { get; set; }

        public string RequestBody { get; set; }

        public List<ApiErrorLogEntryDetail> EntryDetail { get; } = new List<ApiErrorLogEntryDetail>();
        #endregion Properties

        public sealed class ApiErrorLogEntryDetail
        {
            #region Properties
            public string ErrorMessage { get; set; }

            public string ErrorType { get; set; }

            public string TargetSite { get; set; }

            public string StackTrace { get; set; }

            public int? LineNumber { get; set; }
            #endregion Properties
        }
    }
}