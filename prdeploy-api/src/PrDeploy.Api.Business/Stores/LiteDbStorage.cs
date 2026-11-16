using LiteDB;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Owns the shared local LiteDB database used for deployment values and Data Protection keys.
/// </summary>
public sealed class LiteDbStorage : IDisposable
{
    private readonly LiteDatabase _database;

    /// <summary>
    /// Opens or creates a LiteDB database at the supplied path.
    /// </summary>
    public LiteDbStorage(string databasePath, string? password = null)
    {
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _database = new LiteDatabase(new ConnectionString
        {
            Filename = fullPath,
            Password = string.IsNullOrWhiteSpace(password) ? null : password
        });
    }

    /// <summary>
    /// Synchronization object for LiteDB operations sharing this database handle.
    /// </summary>
    internal object Gate { get; } = new();

    /// <summary>
    /// Gets a named LiteDB collection using the shared database connection.
    /// </summary>
    internal ILiteCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);

    /// <summary>
    /// Releases the database handle.
    /// </summary>
    public void Dispose() => _database.Dispose();
}
