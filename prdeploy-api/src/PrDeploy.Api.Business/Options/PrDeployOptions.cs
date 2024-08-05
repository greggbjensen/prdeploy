using PrDeploy.Api.Models;
using PrDeploy.Api.Models.Repositories;

namespace PrDeploy.Api.Business.Options;
public class PrDeployOptions
{
    public string Owner { get; set; } = string.Empty;

    public string Repo { get; set; } = string.Empty;

    public string DefaultSettingsPath { get; set; } = string.Empty;

    public string RepoSettingsPath { get; set; } = string.Empty;

    public List<Repository>? EnabledRepositories { get; set; } = new();
}
