using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PrDeploy.Api.Webhooks.Models;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Coordinates operations for the jira Service and its collaborators.
/// </summary>
public partial class JiraService
{
    public const string HttpClientName = "jira";

    private readonly WebhookContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JiraService> _logger;

    /// <summary>
    /// Initializes a jira Service with the supplied dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="httpClientFactory">Value supplied for http Client Factory.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public JiraService(WebhookContext context, IHttpClientFactory httpClientFactory, ILogger<JiraService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Finds and returns the Associated Issues.
    /// </summary>
    /// <param name="branchName">Branch associated with the operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Retrieves the Base Url.
    /// </summary>
    /// <param name="self">Value supplied for self.</param>
    /// <param name="host">Value supplied for host.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string GetBaseUrl(string? self, string host)
    {
        var url = Uri.TryCreate(self, UriKind.Absolute, out var selfUri) ? selfUri : new Uri(host);
        return url.GetLeftPart(UriPartial.Authority);
    }

    /// <summary>
    /// Performs the jira Issue Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"[a-z]{2,6}-\d+", RegexOptions.IgnoreCase)]
    private static partial Regex JiraIssueRegex();

    /// <summary>
    /// Represents a response containing the jira Search Response.
    /// </summary>
    private class JiraSearchResponse
    {
        /// <summary>
        /// Gets or sets the Issues.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("issues")]
        public List<JiraSearchIssue>? Issues { get; set; }
    }

    /// <summary>
    /// Represents the jira Search Issue used by the application.
    /// </summary>
    private class JiraSearchIssue
    {
        /// <summary>
        /// Gets or sets the Key.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the Self.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("self")]
        public string? Self { get; set; }

        /// <summary>
        /// Gets or sets the Fields.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("fields")]
        public JiraSearchFields? Fields { get; set; }
    }

    /// <summary>
    /// Represents the jira Search Fields used by the application.
    /// </summary>
    private class JiraSearchFields
    {
        /// <summary>
        /// Gets or sets the Summary.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        /// <summary>
        /// Gets or sets the Issue Type.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("issuetype")]
        public JiraIssueType? IssueType { get; set; }
    }

    /// <summary>
    /// Represents the jira Issue Type used by the application.
    /// </summary>
    private class JiraIssueType
    {
        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
