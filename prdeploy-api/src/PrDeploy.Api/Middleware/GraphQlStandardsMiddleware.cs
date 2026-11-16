namespace PrDeploy.Api.Middleware
{
    /// <summary>
    /// Applies the graph Ql Standards Middleware behavior to requests as they pass through the pipeline.
    /// </summary>
    public class GraphQlStandardsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GraphQlStandardsMiddleware> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// Initializes a graph Ql Standards Middleware with the supplied dependencies.
        /// </summary>
        /// <param name="next">Value supplied for next.</param>
        /// <param name="logger">Logger used to report operational information and errors.</param>
        /// <param name="hostEnvironment">Value supplied for host Environment.</param>
        public GraphQlStandardsMiddleware(RequestDelegate next, ILogger<GraphQlStandardsMiddleware> logger,
            IHostEnvironment hostEnvironment)
        {
            _next = next;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Performs the invoke operation.
        /// </summary>
        /// <param name="context">Request or application context used by the operation.</param>
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

        /// <summary>
        /// Performs the on Response Starting operation.
        /// </summary>
        /// <param name="arg">Value supplied for arg.</param>
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
