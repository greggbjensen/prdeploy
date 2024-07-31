using StrawberryShake;

namespace PrDeploy.Api.Tests.Framework.Client
{
    public static class OperationResultExtensions
    {
        /// <summary>
        /// Ensures that the operation result has no errors and throws a
        /// detailed <see cref="GraphQLClientException"/> when the operation result
        /// has errors.
        /// </summary>
        /// <param name="result">The operation result.</param>
        /// <exception cref="ArgumentNullException">
        /// The operation result is null.
        /// </exception>
        /// <exception cref="GraphQLClientException">
        /// The operation result has errors.
        /// </exception>
        public static void ValidateNoErrors(this IOperationResult result)
        {
            // TODO GBJ: Add IErrorFilter for test and release mode.
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.Errors.Count > 0)
            {
                throw new ApiClientException(result.Errors);
            }
        }
    }
}
