using System.Net;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Execution;

namespace PrDeploy.Api.Filters
{
    /// <summary>
    /// Represents the sanitized Http Response Formatter used by the application.
    /// </summary>
    public class SanitizedHttpResponseFormatter : DefaultHttpResponseFormatter
    {
        /// <summary>
        /// Performs the on Determine Status Code operation and returns its result.
        /// </summary>
        /// <param name="result">Value supplied for result.</param>
        /// <param name="format">Value supplied for format.</param>
        /// <param name="proposedStatusCode">Value supplied for proposed Status Code.</param>
        /// <returns>The value produced by this operation.</returns>
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
