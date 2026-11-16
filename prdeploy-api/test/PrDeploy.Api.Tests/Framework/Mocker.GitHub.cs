using PrDeploy.Api.Models;
using PrDeploy.Api.Tests.Constants;
using Octokit;
using PrDeploy.Api.Models.PullRequests;
using PullRequest = Octokit.PullRequest;

namespace PrDeploy.Api.Tests.Framework;
/// <summary>
/// Represents mocker data used by the application.
/// </summary>
public static partial class Mocker
{
    public static readonly PullRequestData PullRequests = new();
    public static readonly DeployUserData DeployUsers = new();

    /// <summary>
    /// Determines whether the issue condition holds.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="user">Object containing the user data used by the operation.</param>
    /// <returns>The issue produced by the operation.</returns>
    public static Issue Issue(Models.PullRequests.PullRequest pullRequest, DeployUser user) =>
        new(null,
            pullRequest.Url,
            null,
            null,
            pullRequest.Number,
            ItemState.Open,
            pullRequest.Title,
            pullRequest.Body,
            null,
            Mocker.User(user),
            new Label[]
            {
                Mocker.Label("deployed", "ebeae8"),
                Mocker.Label("dev", "d4ac0d")
            },
            null,
            null,
            null,
            3,
            Mocker.PullRequest(pullRequest, user),
            null,
            new DateTimeOffset(2023, 10, 16, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2023, 10, 17, 14, 0, 0, TimeSpan.Zero),
            81234,
            null,
            false,
            null,
            null,
            null,
            null);

    /// <summary>
    /// Performs the pull request operation and returns its result.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="user">Object containing the user data used by the operation.</param>
    /// <returns>The pull request produced by the operation.</returns>
    public static PullRequest PullRequest(Models.PullRequests.PullRequest pullRequest, DeployUser user) =>
        new (81234,
            null,
            null,
            pullRequest.Url,
            null,
            null,
            null,
            null,
            pullRequest.Number,
            ItemState.Open,
            pullRequest.Title,
            pullRequest.Body,
            new DateTimeOffset(2023, 10, 16, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2023, 10, 17, 14, 0, 0, TimeSpan.Zero),
            null,
            null,
            null,
            null,
            Mocker.User(user),
            null,
            null,
            false,
            true,
            MergeableState.Clean,
            null,
            null,
            3,
            4,
            12,
            15,
            10,
            null,
            false,
            null,
            null,
            null,
            new Label[]
            {
                Mocker.Label("deployed", "ebeae8"),
                Mocker.Label("dev", "d4ac0d")
            },
            null);

    /// <summary>
    /// Performs the label operation and returns its result.
    /// </summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="color">Text used as the color.</param>
    /// <returns>The label produced by the operation.</returns>
    public static Label Label(string name, string color) => new(
        1,
        null,
        name,
        null,
        color,
        null,
        false);

    /// <summary>
    /// Performs the user operation and returns its result.
    /// </summary>
    /// <param name="user">Object containing the user data used by the operation.</param>
    /// <returns>The user produced by the operation.</returns>
    public static User User(DeployUser user) => new(
        null,
        null,
        null,
        1,
        null,
        new DateTimeOffset(2023, 10, 16, 14, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2023, 10, 17, 14, 0, 0, TimeSpan.Zero),
        256,
        null,
        1,
        3,
        null,
        null,
        2,
        54321,
        null,
        user.Username,
        user.Name,
        null,
        1,
        null,
        0,
        0,
        1,
        null,
        null,
        false,
        null,
        null);

    /// <summary>
    /// Performs the repository variable operation and returns its result.
    /// </summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="value">Value to store or process.</param>
    /// <returns>The repository variable produced by the operation.</returns>
    public static RepositoryVariable RepositoryVariable(string name, string value) => new(
        name,
        value,
        new DateTimeOffset(2023, 10, 16, 14, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2023, 10, 17, 14, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Determines whether the issue comment condition holds.
    /// </summary>
    /// <returns>The issue comment produced by the operation.</returns>
    public static IssueComment IssueComment() => new(
        9852,
        null,
        null,
        null,
        "Test comment",
        new DateTimeOffset(2023, 10, 16, 14, 0, 0, TimeSpan.Zero),
        null,
        null,
        null,
        AuthorAssociation.Contributor
    );
}
