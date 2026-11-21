namespace PrDeploy.Api.Webhooks.Services;

/// <summary>Collects the pull requests whose deploy state changed while one webhook event is processed, so their prdeploy check run is refreshed once at the end.</summary>
/// <remarks>Scoped to a webhook event. Services that change labels, queues or deploy status record the pull request
/// here instead of publishing the check themselves, which avoids dependency cycles and repeated GitHub calls.</remarks>
public class DeployCheckTracker
{
    private readonly HashSet<int> _pullNumbers = [];

    /// <summary>Gets the pull requests recorded so far, in ascending order.</summary>
    /// <value>Pull request numbers whose check run should be refreshed.</value>
    public IReadOnlyList<int> PullNumbers => _pullNumbers.Order().ToList();

    /// <summary>Records that a pull request's deploy state changed.</summary>
    /// <param name="pullNumber">Pull request number; values of zero or less are ignored.</param>
    public void Track(int pullNumber)
    {
        if (pullNumber > 0)
        {
            _pullNumbers.Add(pullNumber);
        }
    }

    /// <summary>Records that several pull requests' deploy state changed, such as every pull request in a queue that moved.</summary>
    /// <param name="pullNumbers">Pull request numbers to record.</param>
    public void TrackAll(IEnumerable<int> pullNumbers)
    {
        foreach (var pullNumber in pullNumbers)
        {
            Track(pullNumber);
        }
    }

    /// <summary>Returns the recorded pull requests and clears them, so a second publish does not repeat the work.</summary>
    /// <returns>The pull request numbers recorded since the last call, in ascending order.</returns>
    public IReadOnlyList<int> Drain()
    {
        var pullNumbers = PullNumbers;
        _pullNumbers.Clear();
        return pullNumbers;
    }
}
