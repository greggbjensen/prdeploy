using System.Net;
using Amazon.SimpleSystemsManagement.Model;

namespace PrDeploy.Api.Tests.Framework;
/// <summary>
/// Represents mocker data used by the application.
/// </summary>
public static partial class Mocker
{
    /// <summary>
    /// Retrieves the parameter response.
    /// </summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="value">Value to store or process.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public static GetParameterResponse GetParameterResponse(string name, string value) =>
        new()
        {
            HttpStatusCode = HttpStatusCode.OK, Parameter = new Parameter { Name = name, Value = value }
        };

    /// <summary>
    /// Performs the put parameter response operation and returns its result.
    /// </summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="value">Value to store or process.</param>
    /// <returns>The put parameter response produced by the operation.</returns>
    public static PutParameterResponse PutParameterResponse(string name, string value) =>
        new()
        {
            HttpStatusCode = HttpStatusCode.OK
        };
}
