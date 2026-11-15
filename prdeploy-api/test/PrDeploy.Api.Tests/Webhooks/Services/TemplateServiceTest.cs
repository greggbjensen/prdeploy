using System.Text.Json;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Models;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

public class TemplateServiceTest
{
    private readonly TemplateService _service = new();

    [Fact]
    public void RenderQueueTable_RendersDynamicPullRequestQueueTable()
    {
        var result = _service.RenderQueueTable("greggbjensen", "prdeploy-example-repo", "dev",
            [2987, 2549, 2391], "https://github.com/greggbjensen/prdeploy-example-repo", "https://awssite/deployments");

        Assert.Equal("""
            | Position | 1       | 2       | 3       |
            |----------|---------|---------|---------|
            | [dev queue](https://awssite/deployments/greggbjensen/prdeploy-example-repo/deployments?environment=dev) | [2987](https://github.com/greggbjensen/prdeploy-example-repo/pull/2987) | [2549](https://github.com/greggbjensen/prdeploy-example-repo/pull/2549) | [2391](https://github.com/greggbjensen/prdeploy-example-repo/pull/2391) |
            """.Replace("\r", ""), result);
    }

    [Fact]
    public void Render_RendersStaticTemplateWithoutContext()
    {
        var result = _service.Render(TemplateService.PullRequestIssues);
        Assert.NotNull(result);
    }

    [Fact]
    public void Render_RendersIssuesTemplateWithContext()
    {
        var issues = new List<JiraIssue>
        {
            new()
            {
                Type = "story", Key = "SCRUM-16478", Url = "https://greggbjensen.atlassian.net/browse/SCRUM-16478",
                Summary = "Additional Property types dont appear to be cached on a new load of the sku pane"
            },
            new()
            {
                Type = "bug", Key = "SCRUM-16636", Url = "https://greggbjensen.atlassian.net/browse/SCRUM-16636",
                Summary = "Images are not getting augmented"
            }
        };

        var result = _service.Render(TemplateService.PullRequestIssues, new
        {
            issues,
            pullBody = "\nPull request with fix and feature.\n",
            branchName = "SCRUM-16478-SCRUM-16636-feature-and-bug"
        });

        Assert.Equal("""
            <!-- ISSUES_START:SCRUM-16478-SCRUM-16636-feature-and-bug -->
            story: [SCRUM-16478](https://greggbjensen.atlassian.net/browse/SCRUM-16478) Additional Property types dont appear to be cached on a new load of the sku pane
            bug: [SCRUM-16636](https://greggbjensen.atlassian.net/browse/SCRUM-16636) Images are not getting augmented
            <!-- ISSUES_END -->


            Pull request with fix and feature.


            """.Replace("\r", ""), result);
    }

    [Fact]
    public void Render_OmitsIssuesBlockWhenNoIssues()
    {
        var result = _service.Render(TemplateService.PullRequestIssues, new
        {
            issues = new List<JiraIssue>(),
            pullBody = "Body",
            branchName = "feature"
        });

        Assert.DoesNotContain("ISSUES_START", result);
        Assert.Equal("Body", result.Trim());
    }

    [Fact]
    public void Render_RendersValidJsonWithEscaping()
    {
        var pull = GitHubModels.PullRequest(2987, title: "This pull request title has some \\\"special\\\" characters");
        var result = _service.Render(TemplateService.DeployReleasedJson, new
        {
            pull = TemplateService.Pull(pull),
            slackPullBody = "\nThis pull request body includes multiple\nnew lines and \" these things...\nOh and another one\"\n"
        });

        using var document = JsonDocument.Parse(result);
        var text = document.RootElement.GetProperty("blocks")[3].GetProperty("text").GetProperty("text").GetString();
        Assert.Contains("\" these things", text);
    }

    [Fact]
    public void Render_KeepsNonAsciiCharactersLikeHandlebarsJs()
    {
        var result = _service.Render(TemplateService.EnvironmentApprovalRequiredJson, new
        {
            environment = new EnvironmentSettings { Name = "prod", Color = "#1d8348", Url = "https://prod" },
            owner = "o",
            repo = "r",
            run = TemplateService.Run(GitHubModels.WorkflowRun(1, "[prod] x")),
            slackUser = new SlackUser { Name = "José O'Neil" },
            isRollback = false
        });

        Assert.Contains("José O&#x27;Neil", result);
        Assert.Contains("placehold.co/32x32/1d8348/ffffff", result);
    }

    [Fact]
    public void Render_RendersEachWithParentContext()
    {
        var result = _service.Render(TemplateService.DeployCompletedJson, new
        {
            environment = new EnvironmentSettings { Name = "dev", Color = "#d4ac0d" },
            owner = "greggbjensen",
            repo = "prdeploy-example-repo",
            run = TemplateService.Run(GitHubModels.WorkflowRun(7065200437, "[dev] x")),
            pull = TemplateService.Pull(GitHubModels.PullRequest(8)),
            status = "complete",
            builds = new[] { new { name = "app", runId = 12345L, version = "1.0.0" } },
            isRollback = false
        });

        using var document = JsonDocument.Parse(result);
        Assert.Contains("app <https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/12345|1.0.0>", result);
        Assert.Contains("actions/runs/7065200437/attempts/1|Deployment", result);
    }
}
