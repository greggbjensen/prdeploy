using System.Xml.Linq;
using PrDeploy.Api.Business.Stores;

namespace PrDeploy.Api.Tests.Stores;

/// <summary>Verifies that the local BLite database is created when missing and reused when present.</summary>
public sealed class BliteStorageTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), "prdeploy-blite-storage-test", Guid.NewGuid().ToString("N"));

    /// <summary>Releases resources owned by this instance.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>Confirms a missing database file, including its parent directories, is created on first open.</summary>
    [Fact]
    public void Constructor_CreatesDatabaseAndDirectories_WhenMissing()
    {
        var databasePath = Path.Combine(_directory, "nested", "data", "deployment-storage.blite");
        Assert.False(File.Exists(databasePath));

        using (var storage = new BliteStorage(databasePath))
        {
            new BliteDataProtectionXmlRepository(storage)
                .StoreElement(new XElement("key", "value"), "key-1");
        }

        Assert.True(File.Exists(databasePath));
    }

    /// <summary>Confirms an existing database on a persistent volume is reused rather than replaced.</summary>
    [Fact]
    public void Constructor_ReusesExistingDatabase_WhenPresent()
    {
        var databasePath = Path.Combine(_directory, "deployment-storage.blite");

        using (var storage = new BliteStorage(databasePath))
        {
            new BliteDataProtectionXmlRepository(storage)
                .StoreElement(new XElement("key", "persisted"), "key-1");
        }

        using var reopened = new BliteStorage(databasePath);
        var elements = new BliteDataProtectionXmlRepository(reopened).GetAllElements();

        Assert.Equal("persisted", Assert.Single(elements).Value);
    }

    /// <summary>Confirms a password-protected database cannot be reopened with a different password.</summary>
    [Fact]
    public void Constructor_Throws_WhenPasswordDoesNotMatch()
    {
        var databasePath = Path.Combine(_directory, "deployment-storage.blite");
        using (var storage = new BliteStorage(databasePath, "correct"))
        {
            new BliteDataProtectionXmlRepository(storage)
                .StoreElement(new XElement("key", "secret"), "key-1");
        }

        Assert.ThrowsAny<Exception>(() =>
        {
            using var wrong = new BliteStorage(databasePath, "wrong");
            new BliteDataProtectionXmlRepository(wrong).GetAllElements();
        });
    }
}
