using System.Net;
using Amazon.SimpleSystemsManagement.Model;

namespace PrDeploy.Api.Tests.Framework;
public static partial class Mocker
{
    public static GetParameterResponse GetParameterResponse(string name, string value) =>
        new()
        {
            HttpStatusCode = HttpStatusCode.OK, Parameter = new Parameter { Name = name, Value = value }
        };

    public static PutParameterResponse PutParameterResponse(string name, string value) =>
        new()
        {
            HttpStatusCode = HttpStatusCode.OK
        };
}
