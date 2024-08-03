using System.Net;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;

namespace PrDeploy.Api.Filters
{
    public class SanitizedHttpResponseFormatter : DefaultHttpResponseFormatter
    {
        protected override HttpStatusCode OnDetermineStatusCode(IQueryResult result, FormatInfo format, HttpStatusCode? proposedStatusCode)
        {
            var statusCode = base.OnDetermineStatusCode(result, format, proposedStatusCode);
            if (result.Errors?.Count > 0)
            {
                if (result.Errors.Any(e => e.Code == "FORBIDDEN"))
                {
                    statusCode = HttpStatusCode.Forbidden;
                }
            }

            return statusCode;
        }
    }
}
