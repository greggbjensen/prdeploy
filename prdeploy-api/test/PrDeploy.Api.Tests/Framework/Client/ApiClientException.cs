using StrawberryShake;
using System.Text;

namespace PrDeploy.Api.Tests.Framework.Client
{
    /// <summary>
    /// Represents an error encountered while processing the api Client Exception.
    /// </summary>
    internal class ApiClientException : Exception
    {
        private readonly string? _stackTrace = null;

        /// <summary>
        /// Initializes a api Client Exception with the supplied dependencies.
        /// </summary>
        /// <param name="message">Value supplied for message.</param>
        public ApiClientException(string message)
            : this(new ClientError(message))
        {
        }

        /// <summary>
        /// Initializes a api Client Exception with the supplied dependencies.
        /// </summary>
        /// <param name="error">Value supplied for error.</param>
        public ApiClientException(IClientError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            Message = error.Message;
            Errors = new[] { error };
        }

        /// <summary>
        /// Initializes a api Client Exception with the supplied dependencies.
        /// </summary>
        /// <param name="errors">Value supplied for errors.</param>
        public ApiClientException(params IClientError[] errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            if (errors.Length == 0)
            {
                Message = "Unknown error occurred";
            }
            else if (errors.Length == 1)
            {
                var detail = errors[0].GetExceptionDetail();
                Message = detail.Message!;
                _stackTrace = detail.StackTrace;
            }
            else
            {
                var message = new StringBuilder("Multiple errors occurred:");

                foreach (var error in errors)
                {
                    var detail = error.GetExceptionDetail();
                    message.AppendLine();
                    message.Append("- ");
                    message.Append(detail.Message);

                    if (string.IsNullOrEmpty(_stackTrace))
                    {
                        _stackTrace = detail.StackTrace;
                    }
                }

                Message = message.ToString();
            }

            Errors = errors;
        }

        /// <summary>
        /// Initializes a api Client Exception with the supplied dependencies.
        /// </summary>
        /// <param name="errors">Value supplied for errors.</param>
        public ApiClientException(IEnumerable<IClientError> errors)
            // We pass this null safe to the constructor using arrays and let it throw there
            // with a proper ArgumentNullException.
            : this(errors?.ToArray()!)
        {
        }

        /// <summary>
        /// The aggregated error message.
        /// </summary>
        public sealed override string Message { get; }

        /// <summary>
        /// Gets the Stack Trace.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public override string? StackTrace => _stackTrace;

        /// <summary>
        /// The underlying client errors.
        /// </summary>
        public IReadOnlyList<IClientError> Errors { get; }
    }
}
