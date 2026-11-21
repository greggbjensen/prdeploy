using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Webhooks.Models;

/// <summary>Merged owner and repository deploy settings, plus repository details needed to act on events.</summary>
/// <param name="Settings">Settings after owner defaults have been merged with repository overrides.</param>
/// <param name="DefaultBranch">Repository's default branch used for workflow operations.</param>
public record RepoSettings(DeploySettings Settings, string DefaultBranch);
