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
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="next">Object containing the next data used by the operation.</param>
        /// <param name="logger">Logger used to report operational information and errors.</param>
        /// <param name="hostEnvironment">Object containing the host environment data used by the operation.</param>
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
            _logger.LogDebug("Request Accept: {RequestAccept}", requestAccept);

            context.Response.OnStarting(OnResponseStarting, context);
            await _next(context);
            string responseContentType = context.Response.Headers.ContentType!;
            _logger.LogDebug("Response Content-Type: {ResponseContentType}", responseContentType);

        }

        /// <summary>
        /// Performs the on response starting operation.
        /// </summary>
        /// <param name="arg">Object containing the arg data used by the operation.</param>
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
