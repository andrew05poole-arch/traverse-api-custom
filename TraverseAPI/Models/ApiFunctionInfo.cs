using System;

namespace TraverseApi
{
    public class ApiFunctionInfo
    {
        public string function_name { get; set; }
        public DateTime first_access_time { get; set; }

        public DateTime last_access_time { get; set; }

        public string last_access_method { get; set; }

        public long num_times_accessed { get; set; }
    }
}