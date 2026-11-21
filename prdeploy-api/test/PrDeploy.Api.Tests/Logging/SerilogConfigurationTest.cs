using Microsoft.Extensions.Configuration;
using PrDeploy.Api.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PrDeploy.Api.Tests.Logging;

/// <summary>Verifies that the shared Serilog pipeline honors the <c>Serilog</c> configuration section.</summary>
public class SerilogConfigurationTest
{
    /// <summary>Sink that stores every event it receives.</summary>
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();

        /// <inheritdoc />
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    /// <summary>Builds a logger from in-memory settings and captures its output.</summary>
    /// <param name="settings">Configuration key/value pairs.</param>
    /// <param name="sink">Receives the emitted events.</param>
    /// <returns>The configured logger.</returns>
    private static Logger CreateLogger(Dictionary<string, string?> settings, CollectingSink sink)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return SerilogConfiguration.Configure(new LoggerConfiguration(), configuration)
            .WriteTo.Sink(sink)
            .CreateLogger();
    }

    /// <summary>A namespace override must suppress that namespace below its level while other sources use the default.</summary>
    [Fact]
    public void Configure_AppliesMinimumLevelOverrides()
    {
        var sink = new CollectingSink();
        using var logger = CreateLogger(new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Information",
            ["Serilog:MinimumLevel:Override:Microsoft"] = "Warning"
        }, sink);

        logger.ForContext("SourceContext", "Microsoft.AspNetCore.Routing").Information("hidden");
        logger.ForContext("SourceContext", "Microsoft.AspNetCore.Routing").Warning("shown framework");
        logger.ForContext("SourceContext", "PrDeploy.Api").Information("shown app");

        Assert.Equal(new[] { "shown framework", "shown app" },
            sink.Events.Select(e => e.MessageTemplate.Text));
    }

    /// <summary>A more specific override must win over its parent namespace, as the Development settings rely on.</summary>
    [Fact]
    public void Configure_MoreSpecificOverrideWins()
    {
        var sink = new CollectingSink();
        using var logger = CreateLogger(new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Information",
            ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Information",
            ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Hosting.Diagnostics"] = "Warning"
        }, sink);

        logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting.Diagnostics").Information("hidden");
        logger.ForContext("SourceContext", "Microsoft.AspNetCore.Routing").Information("shown");

        Assert.Equal(new[] { "shown" }, sink.Events.Select(e => e.MessageTemplate.Text));
    }

    /// <summary>Debug events must be dropped at the Information default and emitted when the default is lowered.</summary>
    [Theory]
    [InlineData("Information", 0)]
    [InlineData("Debug", 1)]
    public void Configure_RespectsDefaultMinimumLevel(string level, int expectedDebugEvents)
    {
        var sink = new CollectingSink();
        using var logger = CreateLogger(new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = level
        }, sink);

        logger.Debug("debug");

        Assert.Equal(expectedDebugEvents, sink.Events.Count);
    }

    /// <summary>Properties pushed through the log context (such as the user scope) must reach emitted events.</summary>
    [Fact]
    public void Configure_EnrichesFromLogContext()
    {
        var sink = new CollectingSink();
        using var logger = CreateLogger(new Dictionary<string, string?>(), sink);

        using (Serilog.Context.LogContext.PushProperty("User", "octocat"))
        {
            logger.Information("scoped");
        }

        Assert.Equal("\"octocat\"", sink.Events.Single().Properties["User"].ToString());
    }

    /// <summary>Runs the real console sink against a captured <see cref="Console.Out"/> and returns what it wrote.</summary>
    /// <param name="prettyConsole">Whether to use the development formatting.</param>
    /// <returns>The raw console text, including any ANSI escape codes.</returns>
    private static string WriteToConsole(bool prettyConsole)
    {
        var original = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var configuration = new ConfigurationBuilder().Build();
            using (var logger = SerilogConfiguration.Configure(
                       new LoggerConfiguration(), configuration, prettyConsole: prettyConsole).CreateLogger())
            {
                logger.ForContext("SourceContext", "Test").Information("Hello {Owner}", "octocat");
            }
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }

    /// <summary>Production output must be a single structured JSON document with the rendered message and properties.</summary>
    [Fact]
    public void Configure_NonDevelopment_WritesStructuredJson()
    {
        var output = WriteToConsole(prettyConsole: false);

        using var json = System.Text.Json.JsonDocument.Parse(output.Trim());
        Assert.Equal("Hello \"octocat\"", json.RootElement.GetProperty("RenderedMessage").GetString());
        Assert.Equal("octocat", json.RootElement.GetProperty("Properties").GetProperty("Owner").GetString());
    }

    /// <summary>Development output must be colorized (ANSI codes) readable text rather than JSON.</summary>
    [Fact]
    public void Configure_Development_WritesColoredReadableText()
    {
        var output = WriteToConsole(prettyConsole: true);

        Assert.Contains("\u001b[", output);
        Assert.Contains("INF", output);
        Assert.Contains("octocat", output);
        Assert.DoesNotContain("\"RenderedMessage\"", output);
    }
}
