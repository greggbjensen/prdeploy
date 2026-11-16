using System.Net;
using Amazon.SimpleSystemsManagement.Model;

namespace PrDeploy.Api.Tests.Framework;
/// <summary>
/// Represents the mocker used by the application.
/// </summary>
public static partial class Mocker
{
    /// <summary>
    /// Retrieves the Parameter Response.
    /// </summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="value">Value to store or process.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GetParameterResponse GetParameterResponse(string name, string value) =>
        new()
        {
            HttpStatusCode = HttpStatusCode.OK, Parameter = new Parameter { Name = name, Value = value }
        };

    /// <summary>
    /// Performs the put Parameter Response operation and returns its result.
    /// </summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="value">Value to store or process.</param>
    /// <returns>The value produced by this operation.</returns>
    public static PutParameterResponse PutParameterResponse(string name, string value) =>
        new()
        {
            HttpStatusCode = HttpStatusCode.OK
        };
}
