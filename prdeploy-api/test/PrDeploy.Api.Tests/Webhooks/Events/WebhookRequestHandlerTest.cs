using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PrDeploy.Api.Tests.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Events;
using PrDeploy.Api.Webhooks.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace PrDeploy.Api.Tests.Webhooks.Events;

/// <summary>Contains tests that verify webhook request handler behavior.</summary>
public class WebhookRequestHandlerTest
{
    private const string Secret = "It's a Secret to Everybody";
    private const string CommentPayload = """
        { "action": "created", "comment": { "id": 1, "body": "/deploy", "issue_url": "https://x/issues/1" },
          "repository": { "name": "r", "html_url": "https://github.com/o/r", "owner": { "login": "o" } },
          "installation": { "id": 42 } }
        """;

    private readonly WebhookEventQueue _queue;
    private readonly WebhookRequestHandler _handler;

    /// <summary>Initializes the test fixture and prepares its dependencies.</summary>
    public WebhookRequestHandlerTest()
    {
        var options = MsOptions.Create(new GitHubAppOptions { WebhookSecret = Secret, QueueCapacity = 1 });
        _queue = new WebhookEventQueue(options);
        _handler = new WebhookRequestHandler(_queue, options, NullLogger<WebhookRequestHandler>.Instance);
    }

    /// <summary>Verifies the expected behavior for this scenario: matches git hub example.</summary>
    [Fact]
    public void WebhookSignature_MatchesGitHubExample()
    {
        // SourceRef: https://docs.github.com/en/webhooks/using-webhooks/validating-webhook-deliveries#testing-the-webhook-payload-validation
        var body = Encoding.UTF8.GetBytes("Hello, World!");
        const string expected = "sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17";

        Assert.Equal(expected, WebhookSignature.Compute(Secret, body));
        Assert.True(WebhookSignature.IsValid(Secret, body, expected));
        Assert.False(WebhookSignature.IsValid(Secret, body, expected.Replace("7571", "7572")));
        Assert.False(WebhookSignature.IsValid(Secret, body, null));
        Assert.False(WebhookSignature.IsValid("", body, expected));
    }

    /// <summary>Verifies the expected behavior for this scenario: queues handled events.</summary>
    [Fact]
    public async Task HandleAsync_QueuesHandledEvents()
    {
        var result = await _handler.HandleAsync(CreateRequest("issue_comment", CommentPayload), CancellationToken.None);

        Assert.IsType<Accepted>(result);
        await using var events = _queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        Assert.True(await events.MoveNextAsync());
        Assert.Equal("issue_comment", events.Current.Name);
        Assert.Equal("created", events.Current.Action);
        Assert.Equal("delivery-1", events.Current.DeliveryId);
    }

    /// <summary>Verifies the expected behavior for this scenario: rejects invalid signature.</summary>
    [Fact]
    public async Task HandleAsync_RejectsInvalidSignature()
    {
        var result = await _handler.HandleAsync(CreateRequest("issue_comment", CommentPayload, "sha256=bad"),
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.True(_queue.TryEnqueue(null!)); // Nothing was queued, so there is still capacity.
    }

    /// <summary>Verifies the expected behavior for this scenario: acknowledges ping and unhandled events.</summary>
    [Fact]
    public async Task HandleAsync_AcknowledgesPingAndUnhandledEvents()
    {
        Assert.IsType<Ok>(await _handler.HandleAsync(CreateRequest("ping", "{}"), CancellationToken.None));
        Assert.IsType<Ok>(await _handler.HandleAsync(
            CreateRequest("issue_comment", CommentPayload.Replace("created", "deleted")), CancellationToken.None));
        Assert.True(_queue.TryEnqueue(null!));
    }

    /// <summary>Verifies the expected behavior for this scenario: returns service unavailable when queue is full.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsServiceUnavailableWhenQueueIsFull()
    {
        await _handler.HandleAsync(CreateRequest("issue_comment", CommentPayload), CancellationToken.None);
        var result = await _handler.HandleAsync(CreateRequest("issue_comment", CommentPayload), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, Assert.IsType<StatusCodeHttpResult>(result).StatusCode);
    }

    /// <summary>Verifies the expected behavior for this scenario: returns service unavailable when secret is not configured.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsServiceUnavailableWhenSecretIsNotConfigured()
    {
        var options = MsOptions.Create(new GitHubAppOptions());
        var handler = new WebhookRequestHandler(new WebhookEventQueue(options), options,
            NullLogger<WebhookRequestHandler>.Instance);

        var result = await handler.HandleAsync(CreateRequest("issue_comment", CommentPayload), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, Assert.IsType<StatusCodeHttpResult>(result).StatusCode);
    }

    /// <summary>Verifies that a signed payload that is not valid JSON is rejected with a structured warning that carries the parse exception and delivery identifiers.</summary>
    [Fact]
    public async Task HandleAsync_InvalidJson_LogsWarningAndReturnsBadRequest()
    {
        var logger = new CapturingLogger<WebhookRequestHandler>();
        var handler = CreateHandler(logger);

        var result = await handler.HandleAsync(CreateRequest("issue_comment", "{not json"), CancellationToken.None);

        Assert.IsType<BadRequest<string>>(result);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.IsAssignableFrom<JsonException>(entry.Exception);
        Assert.Equal("issue_comment", entry.Properties["Event"]);
        Assert.Equal("delivery-1", entry.Properties["DeliveryId"]);
    }

    /// <summary>Verifies that a signed delivery without the event header is rejected with a structured warning.</summary>
    [Fact]
    public async Task HandleAsync_MissingEventHeader_LogsWarningAndReturnsBadRequest()
    {
        var logger = new CapturingLogger<WebhookRequestHandler>();
        var handler = CreateHandler(logger);

        var result = await handler.HandleAsync(CreateRequest("", "{}"), CancellationToken.None);

        Assert.IsType<BadRequest<string>>(result);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("delivery-1", entry.Properties["DeliveryId"]);
        Assert.Equal(WebhookRequestHandler.EventHeader, entry.Properties["Header"]);
    }

    /// <summary>Creates a handler that reports to the supplied logger.</summary>
    /// <param name="logger">Logger that captures the handler's entries.</param>
    /// <returns>A handler using a fresh queue and the shared test secret.</returns>
    private static WebhookRequestHandler CreateHandler(ILogger<WebhookRequestHandler> logger)
    {
        var options = MsOptions.Create(new GitHubAppOptions { WebhookSecret = Secret, QueueCapacity = 1 });
        return new WebhookRequestHandler(new WebhookEventQueue(options), options, logger);
    }

    /// <summary>Creates the request from the supplied inputs.</summary>
    /// <param name="eventName">Text used as the event name.</param>
    /// <param name="payload">Text used as the payload.</param>
    /// <param name="signature">Text used as the signature.</param>
    /// <returns>The object created from the supplied inputs.</returns>
    private static HttpRequest CreateRequest(string eventName, string payload, string? signature = null)
    {
        var body = Encoding.UTF8.GetBytes(payload);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(body);
        context.Request.Headers[WebhookRequestHandler.EventHeader] = eventName;
        context.Request.Headers[WebhookRequestHandler.DeliveryHeader] = "delivery-1";
        context.Request.Headers[WebhookSignature.HeaderName] = signature ?? WebhookSignature.Compute(Secret, body);
        return context.Request;
    }
}
