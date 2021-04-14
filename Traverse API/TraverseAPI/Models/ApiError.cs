using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TraverseApi
{
    internal sealed class ApiError
    {
        #region Constructors
        private ApiError()
            : base()
        { }
        #endregion Constructors

        #region Methods
        internal static ApiError[] GenerateError(Exception exception)
        {
            List<ApiError> errorList = new List<ApiError>();
            errorList.Add(BuildError(exception));
            return errorList.ToArray();
        }

        private static ApiError BuildError(Exception exception)
        {
            if (exception != null)
            {
                ApiError error = new ApiError();
                error.ErrorMessage = exception.Message;
                error.ErrorCode = exception.HResult;

                if (!(exception is ApiRouteNotFoundException || exception is ApiRequestException) && exception.TargetSite != null)
                    error.ErrorMethod = exception.TargetSite.Name;

                if (!(exception is ApiRouteNotFoundException || exception is ApiRequestException) && exception.InnerException != null)
                    error.NextError = BuildError(exception.InnerException);

                return error;
            }
            return null;
        }
        #endregion Methods

        #region Properties
        [Bindable(true)]
        [JsonProperty("error_message")]
        public string ErrorMessage { get; private set; }

        [Bindable(true)]
        [JsonProperty("error_method")]
        public string ErrorMethod { get; private set; }

        [Bindable(true)]
        [JsonProperty("error_number")]
        public int ErrorCode { get; private set; }

        [Bindable(true)]
        [JsonProperty("internal_error")]
        public ApiError NextError { get; private set; }
        #endregion Properties
    }
}