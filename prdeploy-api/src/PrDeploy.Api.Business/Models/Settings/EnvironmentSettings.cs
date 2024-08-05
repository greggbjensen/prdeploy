namespace PrDeploy.Api.Business.Models.Settings;
public class EnvironmentSettings
{
    public string Name { get; set; } = string.Empty;
    public string Queue { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Url { get; set; }
    public bool RequireApproval { get; set; }
    public bool RequireBranchUpToDate { get; set; }
    public AutomationTestSettings? AutomationTest { get; set; }
    public List<string>? ExcludeFromRollback { get; set; }
}
