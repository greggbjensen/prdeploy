using System.Net;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Records HTTP requests and returns responses from a handler, for Slack and Jira calls.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    /// <summary>
    /// Gets the requests.
    /// </summary>
    /// <value>Current requests value.</value>
    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    /// <summary>
    /// Gets or sets the respond.
    /// </summary>
    /// <value>Current respond value.</value>
    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };

    /// <summary>
    /// Performs the send operation and returns its result.
    /// </summary>
    /// <param name="request">Request to process or send to the associated service.</param>
    /// <param name="cancellationToken">Object containing the cancellation token data used by the operation.</param>
    /// <returns>The HTTP response message produced by the operation.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
        Requests.Add((request, body));
        return Respond(request);
    }
}
