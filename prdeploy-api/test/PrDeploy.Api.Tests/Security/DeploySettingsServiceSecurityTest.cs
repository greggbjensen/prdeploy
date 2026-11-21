using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Tests.Security;

/// <summary>Verifies which access guard <see cref="DeploySettingsService"/> applies to repository and owner settings.</summary>
public class DeploySettingsServiceSecurityTest : IDisposable
{
    private readonly Mock<IGitHubSecurity> _security = new();
    private readonly Mock<IDeploymentStore> _store = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly DeploySettingsService _service;

    /// <summary>Wires the service with stored owner and repo settings and an owner guard that always denies, simulating an outside collaborator who is not a member of the owning organization.</summary>
    public DeploySettingsServiceSecurityTest()
    {
        _store.Setup(s => s.GetAsync<DeploySettings?>("o", It.IsAny<string>()))
            .ReturnsAsync(new DeploySettings { Environments = new List<EnvironmentSettings>() });
        _store.Setup(s => s.GetAsync<DeploySettings?>("o", "r", It.IsAny<string>()))
            .ReturnsAsync(new DeploySettings());
        _security.Setup(s => s.GuardOwnerAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden));
        _service = new DeploySettingsService(_store.Object, _security.Object, _cache,
            NullLogger<DeploySettingsService>.Instance);
    }

    /// <summary>Releases the in-memory cache used for merged settings.</summary>
    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Confirms merged settings only require repository access, so outside collaborators can load them.</summary>
    [Fact]
    public async Task GetMergedAsync_RepoAccessWithoutOwnerAccess_ReturnsSettings()
    {
        var settings = await _service.GetMergedAsync("o", "r");

        Assert.NotNull(settings);
        _security.Verify(s => s.GuardRepoAsync("o", "r"), Times.AtLeastOnce);
        _security.Verify(s => s.GuardOwnerAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Confirms merged settings are denied when repository access is denied, before anything is loaded.</summary>
    [Fact]
    public async Task GetMergedAsync_NoRepoAccess_ThrowsBeforeLoading()
    {
        _security.Setup(s => s.GuardRepoAsync("o", "r"))
            .ThrowsAsync(new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetMergedAsync("o", "r"));

        _store.Verify(s => s.GetAsync<DeploySettings?>(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>Confirms cached merged settings are still guarded for each caller.</summary>
    [Fact]
    public async Task GetMergedAsync_Cached_StillGuardsRepo()
    {
        await _service.GetMergedAsync("o", "r");
        _security.Setup(s => s.GuardRepoAsync("o", "r"))
            .ThrowsAsync(new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetMergedAsync("o", "r"));
    }

    /// <summary>Confirms comparing settings only requires repository access.</summary>
    [Fact]
    public async Task CompareAsync_RepoAccessWithoutOwnerAccess_ReturnsComparison()
    {
        var compare = await _service.CompareAsync(new RepoQueryInput { Owner = "o", Repo = "r" });

        Assert.NotNull(compare);
        _security.Verify(s => s.GuardOwnerAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Confirms reading owner settings directly still requires owner access.</summary>
    [Fact]
    public async Task GetOwnerSettingsAsync_NoOwnerAccess_ThrowsForbidden()
    {
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetOwnerSettingsAsync("o"));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }
}
