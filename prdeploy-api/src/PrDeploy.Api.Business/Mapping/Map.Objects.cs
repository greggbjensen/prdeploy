using PrDeploy.Api.Models.OwnerRepo;
using PrDeploy.Api.Models.PullRequests;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Business.Mapping;
public static partial class Map
{
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

    public static DeployUser? DeployUser(Octokit.User? source) =>
        source != null
            ? new DeployUser
            {
                Name = source.Name ?? source.Login,
                Username = source.Login
            }
            : null;

    public static Repository? Repository(Octokit.Repository? source) =>
        source != null
            ? new Repository { Owner = source.Owner.Login, Repo = source.Name }
            : null;
}
