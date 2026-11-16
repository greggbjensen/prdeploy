using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Persists ASP.NET Core Data Protection keys in the configured DynamoDB table.
/// </summary>
/// <param name="storage">DynamoDB document adapter that stores and retrieves XML key documents.</param>
public sealed class DynamoDbDataProtectionXmlRepository(DynamoDbDocumentStorage storage) : IXmlRepository
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
