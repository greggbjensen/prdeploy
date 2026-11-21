using System.Text.Json.Serialization;

namespace PrDeploy.Api.Webhooks.Status;

/// <summary>
/// Describes where the deployment workflow for an environment currently stands.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeployWorkflowState
{
    /// <summary>
    /// The deployment workflow has been requested or is running.
    /// </summary>
    InProgress,

    /// <summary>
    /// The deployment workflow finished successfully.
    /// </summary>
    Successful,

    /// <summary>
    /// The deployment workflow failed, was cancelled, or timed out.
    /// </summary>
    Failure
}
