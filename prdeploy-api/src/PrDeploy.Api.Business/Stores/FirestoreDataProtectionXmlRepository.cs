using System.Xml.Linq;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Persists ASP.NET Core Data Protection keys in the configured Firestore collection.
/// </summary>
public sealed class FirestoreDataProtectionXmlRepository(FirestoreDocumentStorage storage) : IXmlRepository
{
    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements() =>
        storage.GetDataProtectionKeysAsync()
            .GetAwaiter()
            .GetResult()
            .Select(XElement.Parse)
            .ToArray();

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName) =>
        storage.StoreDataProtectionKeyAsync(
                friendlyName,
                element.ToString(SaveOptions.DisableFormatting))
            .GetAwaiter()
            .GetResult();
}
