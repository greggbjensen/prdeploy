using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Models;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Coordinates operations for the slack Service and its collaborators.
/// </summary>
public partial class SlackService
{
    public const string HttpClientName = "slack";
    private const string LookupByEmailUrl = "https://slack.com/api/users.lookupByEmail";

    private readonly WebhookContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackService> _logger;

    /// <summary>
    /// Initializes a slack Service with the supplied dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="httpClientFactory">Value supplied for http Client Factory.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public SlackService(WebhookContext context, IHttpClientFactory httpClientFactory, ILogger<SlackService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Posts the Message to the configured destination.
    /// </summary>
    /// <param name="channel">Value supplied for channel.</param>
    /// <param name="json">Value supplied for json.</param>
    public async Task PostMessageAsync(SlackChannel channel, string json)
    {
        var slack = _context.Settings.Slack;
        if (slack?.NotificationsEnabled != true)
        {
            _logger.LogInformation("Slack notifications disabled");
            return;
        }

        var webhookUrl = channel == SlackChannel.Release ? slack.Webhooks?.ReleaseUrl : slack.Webhooks?.DeployUrl;
        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("No {Channel} Slack channel specified.", channel);
            return;
        }

        _logger.LogDebug("Sending Slack message to {Channel} channel:\n{Json}", channel, json);
        try
        {
            using var _ = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Slack message is not valid json.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request);
            var resultText = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Slack {Channel} messaged posted with response ({Status}):\n{Result}",
                channel, (int)response.StatusCode, resultText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to post Slack message to {Channel}.", channel);
        }
    }

    /// <summary>
    /// Performs the lookup User operation and returns its result.
    /// </summary>
    /// <param name="user">Value supplied for user.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<SlackUser?> LookupUserAsync(User user)
    {
        var slack = _context.Settings.Slack;
        var result = new SlackUser
        {
            Name = !string.IsNullOrEmpty(user.Name) ? user.Name : user.Login,
            Email = user.Email
        };
        _logger.LogInformation("Slack name: {Name}", result.Name);
        _logger.LogInformation("Slack email: {Email}", result.Email);

        // If there is no email, try and build one from name.
        var lookupEmail = result.Email;
        if (string.IsNullOrEmpty(lookupEmail))
        {
            if (string.IsNullOrEmpty(result.Name))
            {
                _logger.LogWarning("One input of name or email is required.");
                return null;
            }

            lookupEmail = BuildEmailFromName(result.Name, slack?.EmailDomain);
        }

        if (string.IsNullOrEmpty(lookupEmail))
        {
            _logger.LogWarning("Unable to find user email.");
            return null;
        }

        if (slack?.EmailAliases != null && slack.EmailAliases.TryGetValue(lookupEmail, out var alias)
                                        && !string.IsNullOrEmpty(alias?.ToString()))
        {
            _logger.LogInformation("alias: {Alias}", alias);
            lookupEmail = alias.ToString()!;
        }

        if (string.IsNullOrEmpty(slack?.Token))
        {
            _logger.LogWarning("No Slack token configured to look up user {Email}.", lookupEmail);
            return result;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{LookupByEmailUrl}?email={Uri.EscapeDataString(lookupEmail)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", slack.Token);

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request);
            var lookup = await response.Content.ReadFromJsonAsync<SlackLookupResponse>();
            if (lookup?.Ok == true && lookup.User != null)
            {
                result.Id = lookup.User.Id;
                result.Username = lookup.User.Name;
                _logger.LogInformation("Found slack user: {Username}", lookup.User.Name);
            }
            else
            {
                _logger.LogWarning("Unable to find user for email {Email}: {Error}", lookupEmail, lookup?.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to find user for email {Email}.", lookupEmail);
        }

        return result;
    }

    /// <summary>
    /// Performs the translate Git Hub Markdown operation and returns its result.
    /// </summary>
    /// <param name="markdown">Value supplied for markdown.</param>
    /// <returns>The value produced by this operation.</returns>
    public string TranslateGitHubMarkdown(string? markdown)
    {
        // Preprocess HTML content (tables, br tags, etc.) before converting markdown.
        var htmlProcessed = HtmlProcessor.ProcessHtmlInMarkdown(markdown);
        return SlackMarkdown.Convert(htmlProcessed);
    }

    /// <summary>
    /// Builds the Email From Name from the supplied inputs.
    /// </summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="emailDomain">Value supplied for email Domain.</param>
    /// <returns>The value produced by this operation.</returns>
    private string BuildEmailFromName(string name, string? emailDomain)
    {
        // Get only first and last name, skipping middle if present.
        var nameParts = WhitespaceRegex().Split(name.Trim());
        var firstName = nameParts[0].ToLowerInvariant();
        var lastName = nameParts.Length > 1 ? nameParts[^1].ToLowerInvariant() : string.Empty;

        var email = !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName)
            ? $"{firstName}.{lastName}@{emailDomain}"
            : $"{firstName}{lastName}@{emailDomain}";

        _logger.LogInformation("Email address not passed, so built email is: {Email}", email);
        return email;
    }

    /// <summary>
    /// Performs the whitespace Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    /// <summary>
    /// Represents a response containing the slack Lookup Response.
    /// </summary>
    private class SlackLookupResponse
    {
        /// <summary>
        /// Gets or sets the Ok.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the User.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("user")]
        public SlackLookupUser? User { get; set; }
    }

    /// <summary>
    /// Represents the slack Lookup User used by the application.
    /// </summary>
    private class SlackLookupUser
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
