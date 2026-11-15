using System.Net;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Execution;

namespace PrDeploy.Api.Filters
{
    public class SanitizedHttpResponseFormatter : DefaultHttpResponseFormatter
    {
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
