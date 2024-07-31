using PrDeploy.Api.Models;

namespace PrDeploy.Api.Business.Options;
public class PrDeployOptions
{
    public string? Owner { get; set; }

    public string? Repo { get; set; }

    public string? DefaultSettingsPath { get; set; }

    public string? RepoSettingsPath { get; set; }

    public List<Repository>? EnabledRepositories { get; set; } = new();
}
