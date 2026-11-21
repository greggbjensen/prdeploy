using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>Reads repository level information from GitHub on behalf of the signed in user.</summary>
public interface IRepositoryService
{
    /// <summary>Lists the GitHub Actions workflow files defined in a repository.</summary>
    /// <param name="input">Owner and repository whose workflows are listed.</param>
    /// <returns>Workflow file paths such as <c>.github/workflows/api-build.yaml</c>, sorted alphabetically.</returns>
    Task<List<string>> ListWorkflowsAsync(RepoQueryInput input);
}
