using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Merged owner and repository deploy settings, plus repository details needed to act on events.
/// </summary>
public record RepoSettings(DeploySettings Settings, string DefaultBranch);
