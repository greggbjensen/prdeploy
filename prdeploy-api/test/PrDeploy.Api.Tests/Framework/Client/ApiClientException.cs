using System.Runtime.Serialization;
using System.Text;
using StrawberryShake;

namespace PrDeploy.Api.Tests.Framework.Client
{
    internal class ApiClientException : Exception
    {
        private readonly string? _stackTrace = null;

        public ApiClientException(string message)
            : this(new ClientError(message))
        {
        }

        public ApiClientException(IClientError error)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Message = error.Message;
            Errors = new[] { error };
        }

        public ApiClientException(params IClientError[] errors)
        {
            if (errors is null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

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

        public ApiClientException(IEnumerable<IClientError> errors)
            // We pass this null safe to the constructor using arrays and let it throw there
            // with a proper ArgumentNullException.
            : this(errors?.ToArray()!)
        {
        }

        protected ApiClientException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
            Message = base.Message;
            Errors ??= Array.Empty<IClientError>();
        }

        /// <summary>
        /// The aggregated error message.
        /// </summary>
        public sealed override string Message { get; }

        public override string? StackTrace => _stackTrace;

        /// <summary>
        /// The underlying client errors.
        /// </summary>
        public IReadOnlyList<IClientError> Errors { get; }
    }
}
