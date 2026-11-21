using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Models.PullRequests;
using PrDeploy.Api.Tests.Framework;

namespace PrDeploy.Api.Tests.Business.Mapping;

/// <summary>
/// Verifies how <see cref="Map.DeployUser"/> maps a GitHub user for display in the portal.
/// </summary>
public class MapDeployUserTest
{
    /// <summary>
    /// The avatar image and profile page URLs are carried through from GitHub.
    /// </summary>
    [Fact]
    public void MapsAvatarAndProfileUrls()
    {
        var source = Mocker.User(new DeployUser
        {
            Name = "Ada Lovelace",
            Username = "ada",
            AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4",
            Url = "https://github.com/ada"
        });

        var user = Map.DeployUser(source)!;

        Assert.Equal("Ada Lovelace", user.Name);
        Assert.Equal("ada", user.Username);
        Assert.Equal("https://avatars.githubusercontent.com/u/1?v=4", user.AvatarUrl);
        Assert.Equal("https://github.com/ada", user.Url);
    }

    /// <summary>
    /// A user without a display name falls back to the login, and missing URLs stay null.
    /// </summary>
    [Fact]
    public void FallsBackToLoginAndLeavesMissingUrlsNull()
    {
        var user = Map.DeployUser(Mocker.User(new DeployUser { Username = "ada" }))!;

        Assert.Equal("ada", user.Name);
        Assert.Null(user.AvatarUrl);
        Assert.Null(user.Url);
    }

    /// <summary>
    /// No GitHub user maps to no deploy user.
    /// </summary>
    [Fact]
    public void ReturnsNullForNoUser()
    {
        Assert.Null(Map.DeployUser(null));
    }
}
