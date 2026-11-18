using System.IdentityModel.Tokens.Jwt;

namespace PrDeploy.Api.Middleware
{
    /// <summary>
    /// Opens a logging scope carrying the authenticated GitHub login, so every log written while handling
    /// the request (services, stores, GraphQL) is tagged with the user.
    /// </summary>
    public class UserLogScopeMiddleware
    {
        /// <summary>
        /// Name of the structured property that holds the authenticated login.
        /// </summary>
        public const string UserProperty = "User";

        private readonly RequestDelegate _next;
        private readonly ILogger<UserLogScopeMiddleware> _logger;

        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="next">Next middleware in the pipeline.</param>
        /// <param name="logger">Logger used to open the user scope.</param>
        public UserLogScopeMiddleware(RequestDelegate next, ILogger<UserLogScopeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Runs the rest of the pipeline inside a user scope when the request is authenticated.
        /// </summary>
        /// <param name="context">Current request. Only the login claim is read; the token is never logged.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var login = GetLogin(context);
            if (login == null)
            {
                await _next(context);
                return;
            }

            using (_logger.BeginScope(new Dictionary<string, object> { [UserProperty] = login }))
            {
                await _next(context);
            }
        }

        /// <summary>
        /// Reads the GitHub login from the authenticated principal.
        /// </summary>
        /// <param name="context">Current request.</param>
        /// <returns>The login, or null when the request is anonymous.</returns>
        public static string? GetLogin(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var login = context.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
            return string.IsNullOrWhiteSpace(login) ? null : login;
        }
    }
}
