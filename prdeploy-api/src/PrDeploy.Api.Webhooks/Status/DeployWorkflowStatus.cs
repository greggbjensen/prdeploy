namespace PrDeploy.Api.Webhooks.Status;

/// <summary>The latest deployment workflow status for one environment of a repository.</summary>
/// <param name="Owner">Repository owner or organization.</param>
/// <param name="Repo">Repository name.</param>
/// <param name="Environment">Environment the workflow deploys to, such as dev or prod.</param>
/// <param name="State">Where the workflow currently stands.</param>
/// <param name="PullNumber">Pull request being deployed.</param>
/// <param name="RunId">GitHub workflow run identifier.</param>
/// <param name="RunUrl">Link to the workflow run in GitHub, when known.</param>
/// <param name="UpdatedAt">When the status last changed.</param>
public record DeployWorkflowStatus(
    string Owner,
    string Repo,
    string Environment,
    DeployWorkflowState State,
    int PullNumber,
    long RunId,
    string? RunUrl,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Maps a completed workflow run conclusion to a state.</summary>
    /// <param name="conclusion">GitHub conclusion such as success, failure, cancelled or timed_out.</param>
    /// <returns>The matching state, or null when the conclusion is not a final outcome (for example skipped).</returns>
    public static DeployWorkflowState? FromConclusion(string? conclusion) => conclusion?.ToLowerInvariant() switch
    {
        "success" => DeployWorkflowState.Successful,
        "failure" or "cancelled" or "timed_out" or "startup_failure" => DeployWorkflowState.Failure,
        _ => null
    };
}
