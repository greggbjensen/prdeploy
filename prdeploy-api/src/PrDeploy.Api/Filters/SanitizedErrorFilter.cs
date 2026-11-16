using System.Net;
using FluentValidation;
using HotChocolate.Execution;

namespace PrDeploy.Api.Filters
{
    /// <summary>
    /// Represents the sanitized Error Filter used by the application.
    /// </summary>
    public class SanitizedErrorFilter : IErrorFilter
    {
        private readonly ILogger<SanitizedErrorFilter> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// Initializes a sanitized Error Filter with the supplied dependencies.
        /// </summary>
        /// <param name="logger">Logger used to report operational information and errors.</param>
        /// <param name="hostEnvironment">Value supplied for host Environment.</param>
        public SanitizedErrorFilter(ILogger<SanitizedErrorFilter> logger, IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Performs the on Error operation and returns its result.
        /// </summary>
        /// <param name="error">Value supplied for error.</param>
        /// <returns>The value produced by this operation.</returns>
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
                    else if (requestException.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        result = ErrorBuilder.FromError(error)
                            .SetCode("UNAUTHORIZED")
                            .SetMessage("Authentication required.")
                            .SetException(
                                new HttpRequestException("Authentication required.",
                                    null, HttpStatusCode.Unauthorized))
                            .Build();
                    }
                    break;

                case ValidationException validationException:
                    var validationMessages = string.Join(", ", validationException.Errors.Select(e => e.ErrorMessage));
                    result = ErrorBuilder.FromError(error)
                        .SetCode("BAD_REQUEST")
                        .SetMessage(validationMessages)
                        .SetException(
                            new HttpRequestException("Bad request.",
                                null, HttpStatusCode.BadRequest))
                        .Build();
                    break;
            }

            _logger.LogError(error.Exception, error.Message, error);

            if (!_hostEnvironment.IsDevelopment())
            {
                // TODO GBJ: Add additional message sanitization for security.
                result.WithException(null);
                result.WithLocations([]);
            }

            return result;
        }
    }
}
