using System.Net;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Execution;

namespace PrDeploy.Api.Filters
{
    /// <summary>
    /// Represents sanitized HTTP response formatter data used by the application.
    /// </summary>
    public class SanitizedHttpResponseFormatter : DefaultHttpResponseFormatter
    {
        /// <summary>
        /// Performs the on determine status code operation and returns its result.
        /// </summary>
        /// <param name="result">Object containing the result data used by the operation.</param>
        /// <param name="format">Object containing the format data used by the operation.</param>
        /// <param name="proposedStatusCode">Object containing the proposed status code data used by the operation.</param>
        /// <returns>The HTTP status code produced by the operation.</returns>
        protected override HttpStatusCode OnDetermineStatusCode(OperationResult result, FormatInfo format, HttpStatusCode? proposedStatusCode)
        {
            var statusCode = base.OnDetermineStatusCode(result, format, proposedStatusCode);
            if (result.Errors?.Count > 0)
            {
                if (result.Errors.Any(e => e.Code == "UNAUTHORIZED"))
                {
                    return HttpStatusCode.Unauthorized;
                }

                if (result.Errors.Any(e => e.Code == "FORBIDDEN"))
                {
                    return HttpStatusCode.Forbidden;
                }
            }

            return statusCode;
        }
    }
}
