using System.Net;
using System.Text.Json;

namespace PrDeploy.Api.Middleware
{
    public class GlobalErrorHandlingMiddleware
    {
        // SourceRef: https://dev.to/moe23/net-6-web-api-global-exceptions-handling-1a46
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _hostEnvironment;

        public GlobalErrorHandlingMiddleware(RequestDelegate next, IHostEnvironment hostEnvironment)
        {
            _next = next;
            _hostEnvironment = hostEnvironment;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode status;
            var stackTrace = string.Empty;
            string? message = null;

            switch (exception)
            {
                case NotImplementedException:
                    status = HttpStatusCode.NotImplemented;
                    break;

                case UnauthorizedAccessException:
                    message = exception.Message;
                    status = HttpStatusCode.Unauthorized;
                    break;

                case KeyNotFoundException:
                    status = HttpStatusCode.NotFound;
                    break;

                default:
                    status = HttpStatusCode.InternalServerError;
                    break;
            }

            if (string.IsNullOrEmpty(message))
            {
                message = exception.Message;
            }

            stackTrace = exception.StackTrace;

            string exceptionResult;
            if (context.Request.Headers.Accept.Any(a => a!.Contains("text/html")))
            {
                exceptionResult = _hostEnvironment.IsDevelopment()
                    ? message
                    : $"{message}: \n${stackTrace}";
                context.Response.ContentType = "text/html";
            }
            else if (context.Request.ContentType?.Contains("graphql") == true)
            {
                // TODO GBJ: Add better GraphQL error serialization.
                exceptionResult = _hostEnvironment.IsDevelopment()
                    ? JsonSerializer.Serialize(new { error = message, stackTrace })
                    : JsonSerializer.Serialize(new { error = message });
                context.Response.ContentType = "application/graphql-response+json; charset=utf-8";
            }
            else
            {
                exceptionResult = _hostEnvironment.IsDevelopment()
                    ? JsonSerializer.Serialize(new { error = message, stackTrace })
                    : JsonSerializer.Serialize(new { error = message });
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)status;
            }

            return context.Response.WriteAsync(exceptionResult);
        }
    }
}
