using System.Xml.Linq;
using LiteDB;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Persists ASP.NET Core Data Protection keys in the shared local LiteDB database.
/// </summary>
public sealed class LiteDbDataProtectionXmlRepository(LiteDbStorage storage) : IXmlRepository
{
    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        lock (storage.Gate)
        {
            return storage.GetCollection<DataProtectionKey>("DataProtectionKeys")
                .FindAll()
                .OrderBy(key => key.FriendlyName, StringComparer.Ordinal)
                .Select(key => XElement.Parse(key.Xml))
                .ToArray();
        }
    }

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName)
    {
        lock (storage.Gate)
        {
            storage.GetCollection<DataProtectionKey>("DataProtectionKeys").Upsert(new DataProtectionKey
            {
                Id = friendlyName,
                FriendlyName = friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting)
            });
        }
    }

    /// <summary>
    /// Represents one serialized XML key in the local Data Protection collection.
    /// </summary>
    private sealed class DataProtectionKey
    {
        /// <summary>
        /// Gets or sets the friendly-name identifier used to replace matching key records.
        /// </summary>
        [BsonId]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key's Data Protection friendly name.
        /// </summary>
        public string FriendlyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the serialized XML key element.
        /// </summary>
        public string Xml { get; set; } = string.Empty;
    }
}
