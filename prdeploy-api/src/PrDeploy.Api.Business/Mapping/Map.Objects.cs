using Octokit;
using PrDeploy.Api.Models.Auth;
using PrDeploy.Api.Models.PullRequests;
using PullRequest = PrDeploy.Api.Models.PullRequests.PullRequest;
using Repository = PrDeploy.Api.Models.OwnerRepo.Repository;

namespace PrDeploy.Api.Business.Mapping;
/// <summary>
/// Represents map data used by the application.
/// </summary>
public static partial class Map
{
    /// <summary>
    /// Performs the user info operation and returns its result.
    /// </summary>
    /// <param name="source">Source environment or value for the operation.</param>
    /// <returns>The user info produced by the operation.</returns>
    public static UserInfo? UserInfo(User? source) =>
        source != null
            ? new UserInfo
            {
                Id = source.Id,
                AvatarUrl = source.AvatarUrl,
                Login = source.Login,
                Name = !string.IsNullOrEmpty(source.Name) ? source.Name : source.Login,
                Admin = source.SiteAdmin,
                Type = (GitHubAccountType)source.Type.GetValueOrDefault()
            }
            : null;

    /// <summary>
    /// Performs the pull request operation and returns its result.
    /// </summary>
    /// <param name="source">Source environment or value for the operation.</param>
    /// <returns>The pull request produced by the operation.</returns>
    public static PullRequest? PullRequest(Octokit.PullRequest? source) =>
        source != null
            ? new PullRequest
            {
                Number = source.Number,
                Url = source.HtmlUrl,
                Title = source.Title,
                Body = source.Body,
                User = Map.DeployUser(source.User),
                UpdatedAt = source.UpdatedAt
            }
            : null;

    /// <summary>
    /// Performs the pull request operation and returns its result.
    /// </summary>
    /// <param name="source">Source environment or value for the operation.</param>
    /// <returns>The pull request produced by the operation.</returns>
    public static PullRequest? PullRequest(Octokit.Issue? source) =>
        source != null
            ? new PullRequest
            {
                Number = source.Number,
                Url = source.HtmlUrl,
                Title = source.Title,
                Body = source.Body,
                User = Map.DeployUser(source.User),
                UpdatedAt = source.UpdatedAt
            }
            : null;

    /// <summary>
    /// Performs the deploy user operation and returns its result.
    /// </summary>
    /// <param name="source">Source environment or value for the operation.</param>
    /// <returns>The deploy user produced by the operation.</returns>
    public static DeployUser? DeployUser(Octokit.User? source) =>
        source != null
            ? new DeployUser
            {
                Name = source.Name ?? source.Login,
                Username = source.Login
            }
            : null;

    /// <summary>
    /// Performs the repository operation and returns its result.
    /// </summary>
    /// <param name="source">Source environment or value for the operation.</param>
    /// <returns>The repository produced by the operation.</returns>
    public static Repository? Repository(Octokit.Repository? source) =>
        source != null
            ? new Repository { Owner = source.Owner.Login, Repo = source.Name }
            : null;
}
