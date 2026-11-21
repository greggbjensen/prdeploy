using Microsoft.Extensions.Logging;
using Moq;
using PrDeploy.Api.Business.Stores;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Tests.Framework;

namespace PrDeploy.Api.Tests.Logging;

/// <summary>Verifies that the deployment store decorator logs structured entries without leaking values.</summary>
public class LoggingDeploymentStoreTest
{
    /// <summary>Confirms a read logs structured identifiers and whether a value was found.</summary>
    [Fact]
    public async Task GetAsync_LogsStructuredPropertiesAndFound()
    {
        var inner = new Mock<IDeploymentStore>();
        inner.Setup(s => s.GetAsync<string>("octocat", "hello", "SETTING")).ReturnsAsync("secret-value");
        var logger = new CapturingLogger<LoggingDeploymentStore>();

        var value = await new LoggingDeploymentStore(inner.Object, logger).GetAsync<string>("octocat", "hello", "SETTING");

        Assert.Equal("secret-value", value);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal("octocat", entry.Properties["Owner"]);
        Assert.Equal("hello", entry.Properties["Repo"]);
        Assert.Equal("SETTING", entry.Properties["Name"]);
        Assert.Equal(true, entry.Properties["Found"]);
        Assert.DoesNotContain("secret-value", entry.Message);
    }

    /// <summary>Confirms a missing value is reported as not found at the revision boundary.</summary>
    [Fact]
    public async Task GetHistoricalAsync_MissingValue_LogsNotFound()
    {
        var inner = new Mock<IDeploymentStore>();
        inner.Setup(s => s.GetHistoricalAsync<string>("o", "r", "N", 3)).ReturnsAsync((string)null!);
        var logger = new CapturingLogger<LoggingDeploymentStore>();

        await new LoggingDeploymentStore(inner.Object, logger).GetHistoricalAsync<string>("o", "r", "N", 3);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(false, entry.Properties["Found"]);
        Assert.Equal(3, entry.Properties["Historical"]);
    }

    /// <summary>Confirms a write logs its identifiers but never the stored value.</summary>
    [Fact]
    public async Task SetAsync_LogsWithoutValue()
    {
        var inner = new Mock<IDeploymentStore>();
        var logger = new CapturingLogger<LoggingDeploymentStore>();

        await new LoggingDeploymentStore(inner.Object, logger).SetAsync("o", "NAME", "super-secret", true);

        inner.Verify(s => s.SetAsync("o", "NAME", "super-secret", true), Times.Once);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal(true, entry.Properties["IsSecure"]);
        Assert.DoesNotContain("super-secret", entry.Message);
    }

    /// <summary>Confirms failures are logged at Error and rethrown unchanged.</summary>
    [Fact]
    public async Task SetAsync_Failure_LogsErrorAndRethrows()
    {
        var failure = new InvalidOperationException("boom");
        var inner = new Mock<IDeploymentStore>();
        inner.Setup(s => s.SetAsync("NAME", "v", false)).ThrowsAsync(failure);
        var logger = new CapturingLogger<LoggingDeploymentStore>();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new LoggingDeploymentStore(inner.Object, logger).SetAsync("NAME", "v"));

        Assert.Same(failure, thrown);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
    }
}
