using System.Net;

namespace PrDeploy.Api.Filters
{
    public class SanitizedErrorFilter : IErrorFilter
    {
        private readonly ILogger<SanitizedErrorFilter> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public SanitizedErrorFilter(ILogger<SanitizedErrorFilter> logger, IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        public IError OnError(IError error)
        {
            var result = error;
            switch (error.Exception)
            {
                // GitHub not found is a forbidden.
                case Octokit.NotFoundException:
                    result = ErrorBuilder.FromError(error)
                        .SetCode("FORBIDDEN")
                        .SetMessage("Access denied.")
                        .SetException(
                            new HttpRequestException("Access denied.",
                                null, HttpStatusCode.Forbidden))
                        .Build();
                    break;

                case HttpRequestException requestException:
                    if (requestException.StatusCode == HttpStatusCode.Forbidden)
                    {
                        result = ErrorBuilder.FromError(error)
                            .SetCode("FORBIDDEN")
                            .SetMessage("Access denied.")
                            .SetException(
                                new HttpRequestException("Access denied.",
                                    null, HttpStatusCode.Forbidden))
                            .Build();
                    }
                    break;

            }

            _logger.LogError(error.Exception, error.Message, error);

            if (!_hostEnvironment.IsDevelopment())
            {
                // TODO GBJ: Add additional message sanitization for security.
                result.RemoveException();
                result.RemoveLocations();
            }

            return result;
        }
    }
}
