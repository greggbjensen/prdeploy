using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Persists ASP.NET Core Data Protection keys in the shared local BLite database.
/// </summary>
/// <param name="storage">Shared BLite adapter that persists and retrieves XML key documents.</param>
public sealed class BliteDataProtectionXmlRepository(BliteStorage storage) : IXmlRepository
{
    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements() =>
        storage.Execute(context => GetAllElementsAsync(context));

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName)
    {
        storage.Execute(async context =>
        {
            await context.DataProtectionKeys.UpsertAsync(new DataProtectionKey
            {
                Id = friendlyName,
                FriendlyName = friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting)
            }).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        });
    }

    private static async Task<IReadOnlyCollection<XElement>> GetAllElementsAsync(BliteStorageContext context)
    {
        var keys = new List<DataProtectionKey>();
        await foreach (var key in context.DataProtectionKeys.FindAllAsync())
        {
            keys.Add(key);
        }

        return keys
            .OrderBy(key => key.FriendlyName, StringComparer.Ordinal)
            .Select(key => XElement.Parse(key.Xml))
            .ToArray();
    }
}
