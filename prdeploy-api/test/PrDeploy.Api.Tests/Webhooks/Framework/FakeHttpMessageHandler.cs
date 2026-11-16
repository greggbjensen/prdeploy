using System.Net;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Records HTTP requests and returns responses from a handler, for Slack and Jira calls.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    /// <summary>
    /// Gets the Requests.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    /// <summary>
    /// Gets or sets the Respond.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };

    /// <summary>
    /// Performs the send operation and returns its result.
    /// </summary>
    /// <param name="request">Request to process or send to the associated service.</param>
    /// <param name="cancellationToken">Value supplied for cancellation Token.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
        Requests.Add((request, body));
        return Respond(request);
    }
}
