using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PrDeploy.Api.Webhooks.Models;

namespace PrDeploy.Api.Webhooks.Services;

public partial class JiraService
{
    public const string HttpClientName = "jira";

    private readonly WebhookContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JiraService> _logger;

    public JiraService(WebhookContext context, IHttpClientFactory httpClientFactory, ILogger<JiraService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<JiraIssue>> ListAssociatedIssuesAsync(string branchName)
    {
        _logger.LogInformation("Parsing branch: {BranchName}", branchName);
        var issueIds = JiraIssueRegex().Matches(branchName)
            .Select(m => m.Value.ToUpperInvariant())
            .Distinct()
            .ToList();
        if (issueIds.Count == 0)
        {
            _logger.LogInformation("No matching issues found from branch.");
            return [];
        }

        _logger.LogInformation("Found issue IDs: {IssueIds}", string.Join(", ", issueIds));
        var jql = $"key in ('{string.Join("', '", issueIds)}')";

        try
        {
            var jira = _context.Settings.Jira;
            if (string.IsNullOrEmpty(jira?.Host))
            {
                throw new InvalidOperationException("Jira host is not configured.");
            }

            // SourceRef: https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issue-search/#api-rest-api-3-search-jql-post
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{jira.Host.TrimEnd('/')}/rest/api/3/search/jql");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{jira.Username}:{jira.Password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = JsonContent.Create(new { jql, fields = new[] { "key", "summary", "issuetype" } });

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JiraSearchResponse>();

            return (result?.Issues ?? []).Select(i => new JiraIssue
            {
                Key = i.Key ?? string.Empty,
                Summary = i.Fields?.Summary ?? string.Empty,
                Type = i.Fields?.IssueType?.Name ?? string.Empty,
                Url = $"{GetBaseUrl(i.Self, jira.Host)}/browse/{i.Key}"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to list associated issues.");
            return [];
        }
    }

    private static string GetBaseUrl(string? self, string host)
    {
        var url = Uri.TryCreate(self, UriKind.Absolute, out var selfUri) ? selfUri : new Uri(host);
        return url.GetLeftPart(UriPartial.Authority);
    }

    [GeneratedRegex(@"[a-z]{2,6}-\d+", RegexOptions.IgnoreCase)]
    private static partial Regex JiraIssueRegex();

    private class JiraSearchResponse
    {
        [JsonPropertyName("issues")]
        public List<JiraSearchIssue>? Issues { get; set; }
    }

    private class JiraSearchIssue
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("self")]
        public string? Self { get; set; }

        [JsonPropertyName("fields")]
        public JiraSearchFields? Fields { get; set; }
    }

    private class JiraSearchFields
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("issuetype")]
        public JiraIssueType? IssueType { get; set; }
    }

    private class JiraIssueType
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
