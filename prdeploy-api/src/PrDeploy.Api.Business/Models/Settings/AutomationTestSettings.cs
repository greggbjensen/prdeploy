namespace PrDeploy.Api.Business.Models.Settings;
public class AutomationTestSettings
{
    public bool? Enabled { get; set; }
    public string? Workflow { get; set; }
    public WorkflowInputs? Inputs { get; set; }
}
