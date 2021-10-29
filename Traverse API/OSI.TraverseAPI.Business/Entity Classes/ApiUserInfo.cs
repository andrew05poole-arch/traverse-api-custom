using System;

namespace OSI.TraverseApi.Business
{
    public class ApiUserInfo
    {
        public string email_address { get; set; }

        public string name { get; set; }

        public DateTime? expiration_date { get; set; }

        public string status { get; set; }

        public DateTime? last_access { get; set; }

        public string last_function { get; set; }

        public long num_assigned_functions { get; set; }
    }
}
