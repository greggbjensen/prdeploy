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
            _logger.LogError(error.Exception, error.Message, error);

            if (!_hostEnvironment.IsDevelopment())
            {
                // TODO GBJ: Add additional message sanitization for security.
                error.RemoveException();
                error.RemoveLocations();
            }

            return error;
        }
    }
}
