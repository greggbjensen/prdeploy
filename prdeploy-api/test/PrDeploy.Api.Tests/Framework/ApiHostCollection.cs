namespace PrDeploy.Api.Tests.Framework;

/// <summary>
/// Groups test classes that start the full API host so they run one at a time. <c>Program</c> replaces
/// the static Serilog bootstrap logger on startup, and starting two hosts concurrently fails with
/// "The logger is already frozen."
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiHostCollection
{
    /// <summary>
    /// The collection name to pass to <see cref="CollectionAttribute"/>.
    /// </summary>
    public const string Name = "API host";
}
