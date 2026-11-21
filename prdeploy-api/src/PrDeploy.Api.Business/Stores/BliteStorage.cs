using BLite.Core;
using BLite.Core.Collections;
using BLite.Core.Encryption;

namespace PrDeploy.Api.Business.Stores;

/// <summary>Owns the shared local BLite database used for deployment values and Data Protection keys.</summary>
public sealed class BliteStorage : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly BliteStorageContext _context;

    /// <summary>Opens or creates a BLite database at the supplied path.</summary>
    public BliteStorage(string databasePath, string? password = null)
    {
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _context = string.IsNullOrWhiteSpace(password)
            ? new BliteStorageContext(fullPath)
            : new BliteStorageContext(fullPath, new CryptoOptions(password));
    }

    /// <summary>Executes one operation against the shared database, serializing access to its context.</summary>
    internal async Task<TResult> ExecuteAsync<TResult>(
        Func<BliteStorageContext, Task<TResult>> operation)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await operation(_context).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Executes a synchronous storage contract against BLite's asynchronous API.</summary>
    internal TResult Execute<TResult>(Func<BliteStorageContext, Task<TResult>> operation) =>
        ExecuteAsync(operation).GetAwaiter().GetResult();

    /// <summary>Releases resources owned by this instance.</summary>
    public void Dispose()
    {
        _context.Dispose();
        _gate.Dispose();
    }
}

/// <summary>Defines the strongly typed collections persisted in the local database.</summary>
public partial class BliteStorageContext : DocumentDbContext
{
    /// <summary>Gets the deployment value revision collection.</summary>
    public DocumentCollection<string, DeploymentRevision> DeploymentValues { get; set; } = null!;

    /// <summary>Gets the Data Protection key collection.</summary>
    public DocumentCollection<string, DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    /// <summary>Opens the database and initializes its typed collections.</summary>
    public BliteStorageContext(string path)
        : base(path)
    {
        InitializeCollections();
    }

    /// <summary>Opens an encrypted database and initializes its typed collections.</summary>
    public BliteStorageContext(string path, CryptoOptions cryptoOptions)
        : base(path, cryptoOptions)
    {
        InitializeCollections();
    }
}

/// <summary>Represents one immutable revision of a deployment value.</summary>
public sealed class DeploymentRevision
{
    /// <summary>Gets or sets the identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Gets or sets the deployment value path.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the revision number.</summary>
    public long Revision { get; set; }

    /// <summary>Gets or sets the serialized value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the caller marked this revision as secure.</summary>
    public bool IsSecure { get; set; }
}

/// <summary>Represents one serialized XML key in the local Data Protection collection.</summary>
public sealed class DataProtectionKey
{
    /// <summary>Gets or sets the identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the friendly name.</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the XML.</summary>
    public string Xml { get; set; } = string.Empty;
}
