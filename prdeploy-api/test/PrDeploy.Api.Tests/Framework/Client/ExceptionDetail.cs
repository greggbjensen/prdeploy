namespace PrDeploy.Api.Tests.Framework.Client
{
    /// <summary>
    /// Represents the exception Detail used by the application.
    /// </summary>
    public class ExceptionDetail
    {
        /// <summary>
        /// Gets or sets the Message.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the Stack Trace.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string? StackTrace { get; set; }
    }
}
