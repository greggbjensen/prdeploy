using StrawberryShake;

namespace PrDeploy.Api.Tests.Framework.Client
{
    public static class IClientErrorExtensions
    {
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
