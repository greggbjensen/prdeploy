using StrawberryShake;

namespace PrDeploy.Api.Tests.Framework.Client
{
    /// <summary>
    /// Defines the contract for client Error Extensions operations.
    /// </summary>
    public static class IClientErrorExtensions
    {
        /// <summary>
        /// Retrieves the exception detail.
        /// </summary>
        /// <param name="error">Object containing the error data used by the operation.</param>
        /// <returns>Requested value, or its default when no value is available.</returns>
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
