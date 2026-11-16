using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Persists ASP.NET Core Data Protection keys in the configured Cosmos DB container.
/// </summary>
/// <param name="storage">Cosmos DB document adapter that stores and retrieves XML key documents.</param>
public sealed class CosmosDataProtectionXmlRepository(CosmosDocumentStorage storage) : IXmlRepository
{
    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements() =>
        storage.GetDataProtectionKeysAsync()
            .GetAwaiter()
            .GetResult()
            .Select(document => XElement.Parse(document.Xml))
            .ToArray();

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName) =>
        storage.StoreDataProtectionKeyAsync(
                friendlyName,
                element.ToString(SaveOptions.DisableFormatting))
            .GetAwaiter()
            .GetResult();
}
