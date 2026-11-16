using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>
/// Contains tests that verify the behavior of the deploy State Service Test.
/// </summary>
public class DeployStateServiceTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly DeployStateService _service;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    public DeployStateServiceTest()
    {
        _service = _host.Get<DeployStateService>();
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: adds Services To Deploy State.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_AddsServicesToDeployState()
    {
        await _service.UpdateAsync("dev", 3445,
            [new DeployedService { Name = "myorg-api-internal", Version = "2024.2.25-r8067229180", RunId = 8067207106 }]);

        var actual = await _service.GetAsync("dev");
        Assert.Single(actual.Services!);
        Assert.Equal("2024.2.25-r8067229180", actual.Services![0].Version);

        actual = await _service.UpdateAsync("dev", 3296,
        [
            new DeployedService { Name = "myorg-app-main", Version = "2024.2.27-r8068273297", RunId = 8068273297 },
            new DeployedService { Name = "myorg-api-service", Version = "2024.2.27-r8067229180", RunId = 8067229180 }
        ]);

        Assert.Equal(3296, actual.PullNumber);
        Assert.Equal(3, actual.Services!.Count);
        Assert.Equal(["myorg-api-internal", "myorg-api-service", "myorg-app-main"], actual.Services.Select(s => s.Name));
        Assert.Equal("2024.2.25-r8067229180", actual.Services.Single(s => s.Name == "myorg-api-internal").Version);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: creates Default Deploy State When None Is Available.
    /// </summary>
    [Fact]
    public async Task GetAsync_CreatesDefaultDeployStateWhenNoneIsAvailable()
    {
        var actual = await _service.GetAsync("dev");

        Assert.Equal(0, actual.PullNumber);
        Assert.Empty(actual.Services!);
        Assert.True(_host.DeploymentStore.DeploymentValues.ContainsKey(
            $"{WebhookTestHost.Owner}/{WebhookTestHost.Repo}/DEPLOY_STATE_DEV".ToUpperInvariant()));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: returns Historical State Without Overwriting Current.
    /// </summary>
    [Fact]
    public async Task GetAsync_ReturnsHistoricalStateWithoutOverwritingCurrent()
    {
        await _service.UpdateAsync("stable", 1, [new DeployedService { Name = "app", Version = "1", RunId = 1 }]);
        await _service.UpdateAsync("stable", 2, [new DeployedService { Name = "app", Version = "2", RunId = 2 }]);

        // History is the default created on first get, then pull 1, then pull 2.
        Assert.Equal(1, (await _service.GetAsync("stable", 1)).PullNumber);
        Assert.Equal(0, (await _service.GetAsync("stable", 10)).PullNumber);
        Assert.Equal(2, (await _service.GetAsync("stable")).PullNumber);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: determines Difference Between Environments.
    /// </summary>
    [Fact]
    public async Task DiffAsync_DeterminesDifferenceBetweenEnvironments()
    {
        await _service.UpdateAsync("dev", 3284,
        [
            new DeployedService { Name = "myorg-api-service", Version = "2024.2.25-r8067229180", RunId = 8067207106 },
            new DeployedService { Name = "myorg-app-main", Version = "2024.2.27-r8067229180", RunId = 8067229180 },
            new DeployedService { Name = "myorg-same", Version = "1.0.0", RunId = 1 }
        ]);

        await _service.UpdateAsync("stage", 3263,
        [
            new DeployedService { Name = "myorg-api-service", Version = "2024.2.28-r8067285421", RunId = 8067285421 },
            new DeployedService { Name = "myorg-app-main", Version = "2024.2.27-r8068273297", RunId = 8068273297 },
            new DeployedService { Name = "myorg-same", Version = "1.0.0", RunId = 1 }
        ]);

        var actual = await _service.DiffAsync("stage", "dev");

        Assert.Equal(3263, actual.PullNumber);
        Assert.Equal(2, actual.Services!.Count);
        Assert.Equal(8067285421, actual.Services.Single(s => s.Name == "myorg-api-service").RunId);
        Assert.Equal(8068273297, actual.Services.Single(s => s.Name == "myorg-app-main").RunId);
    }

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}
