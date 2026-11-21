using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Models.Settings;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Tests.Security;

/// <summary>Verifies the service workflow setting survives comparison and YAML serialization.</summary>
public class ServiceSettingsWorkflowTest
{
    /// <summary>Confirms comparing owner and repo settings keeps each service's workflow.</summary>
    [Fact]
    public void Compare_ServiceWithWorkflow_KeepsWorkflow()
    {
        var owner = new DeploySettings { Services = [new ServiceSettings { Name = "api", Workflow = "api-build.yaml" }] };
        var repo = new DeploySettings { Services = [new ServiceSettings { Name = "web" }] };

        var compare = Map.Compare(owner, repo);

        Assert.Equal("api-build.yaml", compare!.Services.Owner![0].Workflow);
        Assert.Null(compare.Services.Repo![0].Workflow);
    }

    /// <summary>Confirms the workflow is written and read back as a camel case YAML key.</summary>
    [Fact]
    public void Yaml_ServiceWithWorkflow_RoundTrips()
    {
        var yaml = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build()
            .Serialize(new ServiceSettings { Name = "api", Workflow = "api-build.yaml" });
        var result = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build()
            .Deserialize<ServiceSettings>(yaml);

        Assert.Contains("workflow: api-build.yaml", yaml);
        Assert.Equal("api-build.yaml", result.Workflow);
    }
}
