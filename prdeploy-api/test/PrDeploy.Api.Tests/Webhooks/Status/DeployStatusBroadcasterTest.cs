using PrDeploy.Api.Webhooks.Status;

namespace PrDeploy.Api.Tests.Webhooks.Status;

/// <summary>Contains tests that verify deploy status broadcaster behavior.</summary>
public class DeployStatusBroadcasterTest
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies that the latest status per environment is returned in the snapshot, scoped to the repository.</summary>
    [Fact]
    public void GetSnapshot_ReturnsLatestStatusPerEnvironmentForRepository()
    {
        var broadcaster = new DeployStatusBroadcaster();
        broadcaster.Publish(Status("o", "r", "dev", DeployWorkflowState.InProgress));
        broadcaster.Publish(Status("o", "r", "dev", DeployWorkflowState.Successful));
        broadcaster.Publish(Status("o", "r", "Prod", DeployWorkflowState.Failure));
        broadcaster.Publish(Status("o", "other", "dev", DeployWorkflowState.Failure));

        var snapshot = broadcaster.GetSnapshot("O", "R");

        Assert.Equal(["dev", "Prod"], snapshot.Select(s => s.Environment));
        Assert.Equal(DeployWorkflowState.Successful, snapshot[0].State);
    }

    /// <summary>Verifies that a subscriber receives the snapshot first, then later changes for its repository only.</summary>
    [Fact]
    public async Task SubscribeAsync_SendsSnapshotThenChangesForRepositoryOnly()
    {
        var broadcaster = new DeployStatusBroadcaster();
        broadcaster.Publish(Status("o", "r", "dev", DeployWorkflowState.Successful));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var enumerator = broadcaster.SubscribeAsync("o", "r", cts.Token).GetAsyncEnumerator(cts.Token);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(DeployWorkflowState.Successful, enumerator.Current.State);

        broadcaster.Publish(Status("o", "other", "dev", DeployWorkflowState.Failure));
        broadcaster.Publish(Status("o", "r", "dev", DeployWorkflowState.InProgress));

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("r", enumerator.Current.Repo);
        Assert.Equal(DeployWorkflowState.InProgress, enumerator.Current.State);
    }

    /// <summary>Verifies that cancelling ends the stream.</summary>
    [Fact]
    public async Task SubscribeAsync_EndsWhenCancelled()
    {
        var broadcaster = new DeployStatusBroadcaster();
        using var cts = new CancellationTokenSource();
        await using var enumerator = broadcaster.SubscribeAsync("o", "r", cts.Token).GetAsyncEnumerator(cts.Token);
        var pending = enumerator.MoveNextAsync().AsTask();

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    /// <summary>Verifies the conclusion mapping: success, failure-like outcomes, and non-final outcomes.</summary>
    [Theory]
    [InlineData("success", DeployWorkflowState.Successful)]
    [InlineData("failure", DeployWorkflowState.Failure)]
    [InlineData("cancelled", DeployWorkflowState.Failure)]
    [InlineData("timed_out", DeployWorkflowState.Failure)]
    [InlineData("skipped", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void FromConclusion_MapsOutcome(string? conclusion, DeployWorkflowState? expected) =>
        Assert.Equal(expected, DeployWorkflowStatus.FromConclusion(conclusion));

    /// <summary>Builds a status for tests.</summary>
    private static DeployWorkflowStatus Status(string owner, string repo, string environment,
        DeployWorkflowState state) => new(owner, repo, environment, state, 1, 2, null, Now);
}
