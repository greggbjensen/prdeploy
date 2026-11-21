using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using PrDeploy.Api.Configuration;

namespace PrDeploy.Api.Tests.Logging;

/// <summary>Verifies the YAML configuration provider and the swap of the default appsettings JSON sources.</summary>
public class YamlConfigurationTest
{
    /// <summary>Parses YAML text through the provider.</summary>
    /// <param name="yaml">YAML document.</param>
    /// <returns>The flattened configuration.</returns>
    private static IConfigurationRoot Build(string yaml)
    {
        var data = YamlConfigurationProvider.Parse(new MemoryStream(Encoding.UTF8.GetBytes(yaml)));
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    /// <summary>Nested mappings and sequences must flatten to colon-delimited and indexed keys.</summary>
    [Fact]
    public void Parse_FlattensMappingsAndSequences()
    {
        var config = Build("A:\n  B: one\n  List:\n    - x\n    - y\n  Items:\n    - Name: n\n");

        Assert.Equal("one", config["A:B"]);
        Assert.Equal("x", config["A:List:0"]);
        Assert.Equal("y", config["A:List:1"]);
        Assert.Equal("n", config["A:Items:0:Name"]);
    }

    /// <summary>Keys are case-insensitive, scalars keep their text, and quoted empty strings stay empty.</summary>
    [Fact]
    public void Parse_KeepsScalarsAndIsCaseInsensitive()
    {
        var config = Build("Key: \"\"\nNum: 4\nFlag: true\nNothing:\nTilde: ~\nQuoted: \"null\"\n");

        Assert.Equal(string.Empty, config["key"]);
        Assert.Equal(4, config.GetValue<int>("Num"));
        Assert.True(config.GetValue<bool>("Flag"));
        Assert.Null(config["Nothing"]);
        Assert.Null(config["Tilde"]);
        Assert.Equal("null", config["Quoted"]);
    }

    /// <summary>An empty document yields no keys, and an empty sequence yields no children.</summary>
    [Fact]
    public void Parse_HandlesEmptyDocumentAndEmptySequence()
    {
        Assert.Empty(YamlConfigurationProvider.Parse(new MemoryStream()));
        Assert.Empty(Build("List: []\n").GetSection("List").GetChildren());
    }

    /// <summary>Malformed YAML and non-mapping roots must raise <see cref="FormatException"/>.</summary>
    [Theory]
    [InlineData("A: [unclosed")]
    [InlineData("- a\n- b\n")]
    public void Parse_InvalidDocument_Throws(string yaml)
    {
        Assert.Throws<FormatException>(() => Build(yaml));
    }

    /// <summary>The swap must replace the appsettings JSON sources in place, keeping later sources after them.</summary>
    [Fact]
    public void UseYamlAppSettings_ReplacesJsonSourcesInPlace()
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        builder.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: false);
        builder.AddJsonFile("other.json", optional: true, reloadOnChange: false);
        builder.AddEnvironmentVariables();

        builder.UseYamlAppSettings("Production");

        Assert.Equal("appsettings.yaml", Assert.IsType<YamlConfigurationSource>(builder.Sources[0]).Path);
        Assert.Equal("appsettings.Production.yaml", Assert.IsType<YamlConfigurationSource>(builder.Sources[1]).Path);
        Assert.Equal("other.json", Assert.IsType<JsonConfigurationSource>(builder.Sources[2]).Path);
        Assert.Equal(4, builder.Sources.Count);
    }

    /// <summary>Files on disk must load, with a later YAML file overriding an earlier one and a missing optional file ignored.</summary>
    [Fact]
    public void AddYamlFile_LoadsFilesWithOverrides()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "base.yaml"), "S:\n  A: base\n  B: base\n");
            File.WriteAllText(Path.Combine(dir.FullName, "over.yaml"), "S:\n  B: over\n");

            var config = new ConfigurationBuilder()
                .SetBasePath(dir.FullName)
                .AddYamlFile("base.yaml", optional: false, reloadOnChange: false)
                .AddYamlFile("over.yaml", optional: false, reloadOnChange: false)
                .AddYamlFile("missing.yaml", optional: true, reloadOnChange: false)
                .Build();

            Assert.Equal("base", config["S:A"]);
            Assert.Equal("over", config["S:B"]);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    /// <summary>The shipped appsettings files must parse and carry the settings the API depends on.</summary>
    [Fact]
    public void ShippedAppSettings_ParseWithExpectedValues()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: false)
            .AddYamlFile("appsettings.Development.yaml", optional: false, reloadOnChange: false)
            .Build();

        Assert.Equal("Local", config["Storage:Provider"]);
        Assert.Equal("Debug", config["Serilog:MinimumLevel:Default"]);
        Assert.True(config.GetValue<bool>("ApolloSandbox:Enabled"));
        Assert.Equal("Serilog.Expressions", config["Serilog:Using:0"]);
        Assert.Contains("ByExcluding", config["Serilog:Filter:1:Name"]);
        Assert.Equal(3, config.GetSection("Cors:AllowedOrigins").GetChildren().Count());
    }
}
