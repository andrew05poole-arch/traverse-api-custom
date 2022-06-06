#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Xml.Serialization;
using TRAVERSE.Business.API;
using TRAVERSE.Core;
using TraverseApi.Properties;
#endregion Using Directives

namespace TraverseApi
{
    public sealed class ApiErrorHandler : ExceptionHandler
    {
        #region Fields
        private static object _lockObject = new object();
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Private constructor so that only single instance per application
        /// </summary>
        private ApiErrorHandler()
        { }
        #endregion Constructors

        #region Private Methods
        private async Task<ErrorResponse> ParseException(HttpRequestMessage request, Exception ex)
        {
            var code = HttpStatusCode.InternalServerError;
            Type type = ex.GetType();

            switch (type.Name)
            {
                case "EntityValidationException":
                case "PeriodClosedException":
                case "OutofBalanceException":
                case "PeriodInvalidException":
                case "ProviderException":
                case "DeleteValidationException":
                    code = HttpStatusCode.Conflict;
                    break;
                case "NothingToProcessException":
                case "InvalidValueException":
                case "ArgumentNullException":
                case "ApiRequestException":
                case "MissingFieldException":
                    code = HttpStatusCode.BadRequest;
                    break;
                case "PermissionDeniedException":
                    code = HttpStatusCode.Unauthorized;
                    break;
                case "BusinessRuleException":
                    code = HttpStatusCode.Forbidden;
                    break;
                case "ApiRouteNotFoundException":
                    code = HttpStatusCode.NotFound;
                    break;
                default:
                    code = HttpStatusCode.InternalServerError;
                    break;
            }

            return await Task.FromResult<ErrorResponse>(new ErrorResponse(request.CreateResponse<ApiError[]>(code, ApiError.GenerateError(ex))));
        }
        #endregion Private Methods

        #region Overrides
        public override async Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            var ex = context.Exception;

            if (ex != null)
            {
                try
                {
                    await ProcessError(ex);
                }
                catch
                { }

                context.Result = await this.ParseException(context.Request, ex);
            }

            await base.HandleAsync(context, cancellationToken);
        }
        #endregion Overrides

        #region Static Methods
        public static async Task ProcessError(Exception exception)
        {
            //Grab our date information now and use the same date-time for the remainder of the code
            //This accounts for the rare instance that the error might happen and the time rolls to the next day before being fully processed
            DateTime now = DateTime.Now;
            DateTime today = now.Date;

            //Calculate our file path
            string path = PrepareErrorFilePath(today);

            //Generate our log object to serialize
            var entry = BuildLogEntry(now, exception);
            await Task.Run(() => WriteToFile(path, entry));
        }

        private static ApiErrorLogEntry BuildLogEntry(DateTime date, Exception ex)
        {
            ApiErrorLogEntry entry = new ApiErrorLogEntry() { Date = date, ErrorMessage = ex.Message };

            // If this error was thrown while processing a request, capture the request details for debugging
            if (!(ex is ApiRouteNotFoundException) && HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                var request = HttpContext.Current.Request;
                ApiUser user = null;
                try
                {
                    var context = request.GetOwinContext();
                    //Check to see if we have a user
                    user = context.Get<ApiUser>(Resources.ApiUserInfoStorage);

                    //Load any request body text
                    entry.RequestBody = context.Get<string>(Resources.ApiBodyContent);
                }
                catch
                { }

                if (user != null)
                    entry.UserEmail = user.EmailAddress;

                entry.HttpMethod = request.HttpMethod;
                entry.Endpoint = request.Url.PathAndQuery;
            }

            while (ex != null && !(ex is ApiRouteNotFoundException))
            {
                int? lineNum = null;
                string errorNumber = !(ex is LoginException) ? string.Empty : string.Format(" ({0})", ((LoginException)ex).ErrorNumber);

                //Try to get the line number for the error
                try
                {
                    var trace = new StackTrace(ex, true);
                    lineNum = trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber();
                }
                catch { }

                entry.EntryDetail.Add(
                    new ApiErrorLogEntry.ApiErrorLogEntryDetail()
                    {
                        ErrorMessage = string.Format("{0}{1}", ex.Message, errorNumber),
                        ErrorType = ex.GetType().Name,
                        TargetSite = ex.TargetSite == null ? string.Empty : ex.TargetSite.Name,
                        StackTrace = ex.StackTrace,
                        LineNumber = lineNum
                    });

                ex = ex.InnerException;
            }

            return entry;
        }

        private static string PrepareErrorFilePath(DateTime date)
        {
            //Check that error log directory exists
            string errorDir = Path.Combine(HttpRuntime.AppDomainAppPath, "ErrorLog");
            if (!Directory.Exists(errorDir))
                Directory.CreateDirectory(errorDir);

            //Calculate the number of the current week regardless of our localization
            int weekNum = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date,
                    CultureInfo.InvariantCulture.DateTimeFormat.CalendarWeekRule,
                    CultureInfo.InvariantCulture.DateTimeFormat.FirstDayOfWeek);

            //Calculate filename by using year and number of week
            string fileName = string.Format("ApiErrors{0}_{1:00}.log", date.Year, weekNum);

            return Path.Combine(errorDir, fileName);
        }

        private static void WriteToFile(string path, ApiErrorLogEntry entry)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof(List<ApiErrorLogEntry>));

            lock (_lockObject)
            {
                List<ApiErrorLogEntry> list = ReadErrorFile(path);
                list.Add(entry);
                using (StringWriter writer = new StringWriter(builder))
                {
                    serializer.Serialize(writer, list);
                }

                File.WriteAllText(path, builder.ToString());
            }
        }

        internal static List<ApiErrorLogEntry> ReadErrorFile(string path)
        {
            if (!File.Exists(path))
                return new List<ApiErrorLogEntry>();

            string text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text))
                return new List<ApiErrorLogEntry>();

            XmlSerializer serializer = new XmlSerializer(typeof(List<ApiErrorLogEntry>));
            using (StringReader reader = new StringReader(text ?? string.Empty))
            {
                return serializer.Deserialize(reader) as List<ApiErrorLogEntry>;
            }
        }
        #endregion Static Methods

        #region Properties
        internal static ApiErrorHandler ErrorHandler { get; } = new ApiErrorHandler();
        #endregion Properties

        #region Response Class
        private class ErrorResponse : IHttpActionResult
        {
            #region Public Methods
            public ErrorResponse(HttpResponseMessage message)
            {
                ResponseMessage = message;
            }

            public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                return await Task.FromResult<HttpResponseMessage>(ResponseMessage);
            }
            #endregion Public Methods

            #region Properties
            private HttpResponseMessage ResponseMessage { get; set; }
            #endregion Properties
        }
        #endregion Response Class
    }
}