using StrawberryShake;

namespace PrDeploy.Api.Tests.Framework.Client
{
    /// <summary>
    /// Defines the contract for client Error Extensions operations.
    /// </summary>
    public static class IClientErrorExtensions
    {
        /// <summary>
        /// Retrieves the Exception Detail.
        /// </summary>
        /// <param name="error">Value supplied for error.</param>
        /// <returns>The value produced by this operation.</returns>
        public static ExceptionDetail GetExceptionDetail(this IClientError error)
        {
            var detail = new ExceptionDetail();
            if (error.Extensions != null 
                && error.Extensions.TryGetValue("message", out object? message))
            {
                detail.Message = message as string;

                if (error.Extensions.TryGetValue("stackTrace", out object? stackTrace))
                {
                    detail.StackTrace = stackTrace as string;
                }
            }
            else
            {
                detail.Message = error.Message;
            }

            return detail;
        }
    }
}
