namespace PrDeploy.Api.Middleware
{
    public class GraphQlStandardsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GraphQlStandardsMiddleware> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public GraphQlStandardsMiddleware(RequestDelegate next, ILogger<GraphQlStandardsMiddleware> logger,
            IHostEnvironment hostEnvironment)
        {
            _next = next;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Allow request to be read multiple times in development to support GlobalRoleAuthorizationHandler.
            if (_hostEnvironment.IsDevelopment())
            {
                context.Request.EnableBuffering();
            }

            string requestAccept = context.Request.Headers.Accept!;
            _logger.LogInformation($"Request Accept: {requestAccept}");

            context.Response.OnStarting(OnResponseStarting, context);
            await _next(context);
            string responseContentType = context.Response.Headers.ContentType!;
            _logger.LogInformation($"Response Content-Type: {responseContentType}");

        }

        private Task OnResponseStarting(object arg)
        {
            HttpContext context = (HttpContext)arg;
            //ApolloGW compatibility
            if (context.Response.ContentType == "application/graphql-response+json; charset=utf-8")
                context.Response.ContentType = "application/json; charset=utf-8";
            return Task.CompletedTask;
        }
    }
}
