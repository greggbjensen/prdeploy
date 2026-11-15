using System.Net;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Records HTTP requests and returns responses from a handler, for Slack and Jira calls.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
        Requests.Add((request, body));
        return Respond(request);
    }
}
