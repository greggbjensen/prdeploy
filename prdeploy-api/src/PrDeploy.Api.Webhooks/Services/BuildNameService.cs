using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>A build workflow file and the service name it is mapped to in the repository settings.</summary>
/// <param name="Workflow">Workflow file name, such as <c>api-ci.yml</c>.</param>
/// <param name="Name">Name of the service the workflow builds.</param>
public record BuildName(string Workflow, string Name);

/// <summary>Lists the workflow-to-service mappings that the <c>get-build-info</c> GitHub Action uses to name builds.</summary>
/// <remarks>
/// The action only has the workflow's <c>GITHUB_TOKEN</c>, not a portal sign-in, so access is proven by reading the
/// repository with that token instead of by the portal JWT.
/// </remarks>
public class BuildNameService
{
    private const string DeploySettingsKey = "DEPLOY_SETTINGS";

    private readonly IDeploymentStore _deploymentStore;
    private readonly ILogger<BuildNameService> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="deploymentStore">Store holding the owner and repository deploy settings.</param>
    /// <param name="logger">Logger used to report access decisions.</param>
    public BuildNameService(IDeploymentStore deploymentStore, ILogger<BuildNameService> logger)
    {
        _deploymentStore = deploymentStore;
        _logger = logger;
    }

    /// <summary>Lists the services mapped to a build workflow for a repository the caller can read.</summary>
    /// <param name="client">GitHub client authenticated with the caller's token.</param>
    /// <param name="owner">Repository owner or organization.</param>
    /// <param name="repo">Repository name.</param>
    /// <returns>The mappings, sorted by workflow, or <c>null</c> when the token cannot read the repository. Services
    /// without a workflow are left out. Repository settings override owner defaults.</returns>
    public async Task<List<BuildName>?> ListAsync(IGitHubClient client, string owner, string repo)
    {
        try
        {
            await client.Repository.Get(owner, repo);
        }
        catch (Exception ex) when (ex is NotFoundException or AuthorizationException or ForbiddenException)
        {
            _logger.LogInformation("Token cannot read {Owner}/{Repo}; build names withheld.", owner, repo);
            return null;
        }

        var ownerSettings = await _deploymentStore.GetAsync<DeploySettings?>(owner, DeploySettingsKey);
        var settings = await _deploymentStore.GetAsync<DeploySettings?>(owner, repo, DeploySettingsKey)
                       ?? new DeploySettings();
        Map.Merge(settings, ownerSettings);

        return (settings.Services ?? [])
            .Where(s => !string.IsNullOrEmpty(s.Workflow) && !string.IsNullOrEmpty(s.Name))
            .Select(s => new BuildName(Path.GetFileName(s.Workflow!), s.Name))
            .OrderBy(b => b.Workflow, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
