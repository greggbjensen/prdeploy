namespace PrDeploy.Api.Tests.Framework.Client
{
    /// <summary>
    /// Represents exception detail data used by the application.
    /// </summary>
    public class ExceptionDetail
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>Current message value.</value>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the stack trace.
        /// </summary>
        /// <value>Current stack trace value.</value>
        public string? StackTrace { get; set; }
    }
}
