using System.Net;
using FluentValidation;
using HotChocolate.Execution;

namespace PrDeploy.Api.Filters
{
    /// <summary>
    /// Represents sanitized error filter data used by the application.
    /// </summary>
    public class SanitizedErrorFilter : IErrorFilter
    {
        private readonly ILogger<SanitizedErrorFilter> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="logger">Logger used to report operational information and errors.</param>
        /// <param name="hostEnvironment">Object containing the host environment data used by the operation.</param>
        public SanitizedErrorFilter(ILogger<SanitizedErrorFilter> logger, IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Performs the on error operation and returns its result.
        /// </summary>
        /// <param name="error">Object containing the error data used by the operation.</param>
        /// <returns>The i error produced by the operation.</returns>
        public IError OnError(IError error)
        {
            var result = error;
            switch (error.Exception)
            {
                // GitHub not found or forbidden is a forbidden.
                case Octokit.NotFoundException:
                case Octokit.ForbiddenException:
                    result = ErrorBuilder.FromError(error)
                        .SetCode("FORBIDDEN")
                        .SetMessage("Access denied.")
                        .SetException(
                            new HttpRequestException("Access denied.",
                                null, HttpStatusCode.Forbidden))
                        .Build();
                    break;

                case Octokit.AuthorizationException:
                    result = ErrorBuilder.FromError(error)
                        .SetCode("UNAUTHORIZED")
                        .SetMessage("Authentication required.")
                        .SetException(
                            new HttpRequestException("Authentication required.",
                                null, HttpStatusCode.Unauthorized))
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

            // Expected client errors are warnings; anything else is an unexpected server error.
            var level = error.Exception is null or Octokit.NotFoundException or Octokit.ForbiddenException
                        or Octokit.AuthorizationException or ValidationException
                        or HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized }
                ? LogLevel.Warning
                : LogLevel.Error;
            _logger.Log(level, error.Exception, "GraphQL error {ErrorCode} at {ErrorPath}: {ErrorMessage}",
                result.Code, error.Path?.Print(), error.Message);

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
