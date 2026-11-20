using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Business.Services;

/// <summary>
/// Reads repository level information from GitHub on behalf of the signed in user.
/// </summary>
public class RepositoryService : IRepositoryService
{
    private const string WorkflowsFolder = ".github/workflows";
    private readonly IGitHubClient _client;
    private readonly IGitHubSecurity _gitHubSecurity;
    private readonly IDeploySettingsService _deploySettingsService;
    private readonly ILogger<RepositoryService> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="client">GitHub client authenticated as the signed in user.</param>
    /// <param name="gitHubSecurity">Guard used to confirm the user can access the repository.</param>
    /// <param name="deploySettingsService">Source of the merged settings, including the builds workflow pattern.</param>
    /// <param name="logger">Logger used to report operational activity.</param>
    public RepositoryService(IGitHubClient client, IGitHubSecurity gitHubSecurity,
        IDeploySettingsService deploySettingsService, ILogger<RepositoryService> logger)
    {
        _client = client;
        _gitHubSecurity = gitHubSecurity;
        _deploySettingsService = deploySettingsService;
        _logger = logger;
    }

    /// <summary>
    /// Lists the build workflow files in the repository's <c>.github/workflows</c> folder.
    /// </summary>
    /// <param name="input">Owner and repository whose workflows are listed.</param>
    /// <returns>Workflow file paths such as <c>.github/workflows/api-build.yaml</c>, sorted alphabetically. Only
    /// files matching the merged builds workflow pattern are returned; when no valid pattern is configured every
    /// YAML file is returned. A repository without the folder yields an empty list.</returns>
    public async Task<List<string>> ListWorkflowsAsync(RepoQueryInput input)
    {
        await _gitHubSecurity.GuardRepoAsync(input.Owner, input.Repo);

        IReadOnlyList<RepositoryContent> contents;
        try
        {
            contents = await _client.Repository.Content.GetAllContents(input.Owner, input.Repo, WorkflowsFolder);
        }
        catch (NotFoundException)
        {
            _logger.LogDebug("No {Folder} folder in {Owner}/{Repo}.", WorkflowsFolder, input.Owner, input.Repo);
            return [];
        }

        var settings = await _deploySettingsService.GetMergedAsync(input.Owner, input.Repo);
        var pattern = CreatePattern(settings.Builds?.WorkflowPattern);

        var paths = contents
            .Where(c => c.Type == ContentType.File && IsYamlFile(c.Path))
            .Select(c => c.Path)
            .Where(p => pattern == null || pattern.IsMatch(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogDebug("Found {Count} build workflow files in {Owner}/{Repo}.", paths.Count, input.Owner, input.Repo);
        return paths;
    }

    /// <summary>
    /// Builds the case-insensitive regular expression for the builds workflow pattern.
    /// </summary>
    /// <param name="pattern">Configured builds workflow pattern, if any.</param>
    /// <returns>The expression, or <c>null</c> when the pattern is empty or not a valid expression.</returns>
    private Regex? CreatePattern(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return null;
        }

        try
        {
            return new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "The builds workflow pattern is not a valid expression; listing all workflows.");
            return null;
        }
    }

    /// <summary>
    /// Determines whether a path points at a YAML file.
    /// </summary>
    /// <param name="path">File path reported by GitHub.</param>
    /// <returns><c>true</c> when the path ends in <c>.yml</c> or <c>.yaml</c>.</returns>
    private static bool IsYamlFile(string? path) =>
        !string.IsNullOrEmpty(path)
        && (path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase));
}
