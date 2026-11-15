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

public partial class SlackService
{
    public const string HttpClientName = "slack";
    private const string LookupByEmailUrl = "https://slack.com/api/users.lookupByEmail";

    private readonly WebhookContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackService> _logger;

    public SlackService(WebhookContext context, IHttpClientFactory httpClientFactory, ILogger<SlackService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

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

    public string TranslateGitHubMarkdown(string? markdown)
    {
        // Preprocess HTML content (tables, br tags, etc.) before converting markdown.
        var htmlProcessed = HtmlProcessor.ProcessHtmlInMarkdown(markdown);
        return SlackMarkdown.Convert(htmlProcessed);
    }

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

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private class SlackLookupResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("user")]
        public SlackLookupUser? User { get; set; }
    }

    private class SlackLookupUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
