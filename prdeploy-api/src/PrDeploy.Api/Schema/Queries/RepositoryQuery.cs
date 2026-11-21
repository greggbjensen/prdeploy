using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>Defines queries for retrieving the repository Query.</summary>
[ExtendObjectType("DeployQuery")]
public class RepositoryQuery
{
    /// <summary>Performs the repository services operation and returns its result.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The collection of items produced by the operation.</returns>
    [GraphQLName("repositoryServices")]
    public async Task<List<string>> RepositoryServices(IDeploySettingsService service, RepoQueryInput input) =>
        await service.ListServicesAsync(input);

    /// <summary>Lists the GitHub Actions workflow files in a repository.</summary>
    /// <param name="service">Service that reads repository information from GitHub.</param>
    /// <param name="input">Owner and repository whose workflows are listed.</param>
    /// <returns>Workflow file paths such as <c>.github/workflows/api-build.yaml</c>.</returns>
    [GraphQLName("repositoryWorkflows")]
    public async Task<List<string>> RepositoryWorkflows(IRepositoryService service, RepoQueryInput input) =>
        await service.ListWorkflowsAsync(input);
}
