using System;
using System.Runtime.Serialization;

namespace TRAVERSE.Web.API
{
    public class ApiRequestException : Exception
    {
        #region Constructors
        public ApiRequestException()
            : base()
        { }

        public ApiRequestException(string message)
            : base(message)
        { }

        public ApiRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ApiRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
        #endregion Constructors
    }
}