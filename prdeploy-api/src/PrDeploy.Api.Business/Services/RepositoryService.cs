using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Business.Services;

/// <summary>Reads repository level information from GitHub on behalf of the signed in user.</summary>
public class RepositoryService : IRepositoryService
{
    private const string WorkflowsFolder = ".github/workflows";
    private readonly IGitHubClient _client;
    private readonly IGitHubSecurity _gitHubSecurity;
    private readonly ILogger<RepositoryService> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="client">GitHub client authenticated as the signed in user.</param>
    /// <param name="gitHubSecurity">Guard used to confirm the user can access the repository.</param>
    /// <param name="logger">Logger used to report operational activity.</param>
    public RepositoryService(IGitHubClient client, IGitHubSecurity gitHubSecurity,
        ILogger<RepositoryService> logger)
    {
        _client = client;
        _gitHubSecurity = gitHubSecurity;
        _logger = logger;
    }

    /// <summary>Lists the workflow files in the repository's <c>.github/workflows</c> folder.</summary>
    /// <param name="input">Owner and repository whose workflows are listed.</param>
    /// <returns>Workflow file paths such as <c>.github/workflows/api-build.yaml</c>, sorted alphabetically. Every
    /// YAML file is returned so it can be mapped to a service. A repository without the folder yields an empty list.</returns>
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

        var paths = contents
            .Where(c => c.Type == ContentType.File && IsYamlFile(c.Path))
            .Select(c => c.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogDebug("Found {Count} workflow files in {Owner}/{Repo}.", paths.Count, input.Owner, input.Repo);
        return paths;
    }

    /// <summary>Determines whether a path points at a YAML file.</summary>
    /// <param name="path">File path reported by GitHub.</param>
    /// <returns><c>true</c> when the path ends in <c>.yml</c> or <c>.yaml</c>.</returns>
    private static bool IsYamlFile(string? path) =>
        !string.IsNullOrEmpty(path)
        && (path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase));
}
