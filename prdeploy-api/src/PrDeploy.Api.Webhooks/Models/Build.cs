namespace PrDeploy.Api.Webhooks.Models;

public class Build
{
    public long RunId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Url { get; set; }
}
