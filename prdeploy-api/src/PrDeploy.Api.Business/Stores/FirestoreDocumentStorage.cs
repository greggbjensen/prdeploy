using Google.Cloud.Firestore;

namespace PrDeploy.Api.Business.Stores;

/// <summary>Provides document operations for deployment values and Data Protection keys in Firestore.</summary>
/// <param name="firestore">Firestore client used to access the configured collections.</param>
/// <param name="deploymentCollectionName">Collection used to store deployment-value revisions.</param>
/// <param name="dataProtectionCollectionName">Collection used to store Data Protection key documents.</param>
public sealed class FirestoreDocumentStorage(
    FirestoreDb firestore,
    string deploymentCollectionName,
    string dataProtectionCollectionName)
{
    /// <summary>Retrieves a deployment value revision by its storage path.</summary>
    public async Task<DocumentSnapshot?> GetDeploymentValueAsync(
        string path,
        int historical,
        CancellationToken cancellationToken = default)
    {
        var query = firestore.Collection(deploymentCollectionName)
            .WhereEqualTo("path", path)
            .OrderByDescending("revision")
            .OrderByDescending(FieldPath.DocumentId)
            .Offset(Math.Max(historical, 0))
            .Limit(1);
        var snapshot = await query.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.Documents.FirstOrDefault();
    }

    /// <summary>Appends a deployment value revision to its path collection.</summary>
    public Task SetDeploymentValueAsync(
        string path,
        string value,
        bool isSecure,
        CancellationToken cancellationToken = default)
    {
        var document = firestore.Collection(deploymentCollectionName).Document(Guid.NewGuid().ToString("N"));
        return document.SetAsync(new Dictionary<string, object>
        {
            ["path"] = path,
            ["value"] = value,
            ["isSecure"] = isSecure,
            ["revision"] = DateTime.UtcNow.Ticks
        }, cancellationToken: cancellationToken);
    }

    /// <summary>Reads the XML from all persisted Data Protection key documents.</summary>
    public async Task<IReadOnlyList<string>> GetDataProtectionKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var snapshot = await firestore.Collection(dataProtectionCollectionName)
            .OrderBy(FieldPath.DocumentId)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Documents
            .Where(document => document.ContainsField("xml"))
            .Select(document => document.GetValue<string>("xml"))
            .ToArray();
    }

    /// <summary>Adds or replaces a Data Protection key document by its friendly name.</summary>
    public Task StoreDataProtectionKeyAsync(
        string friendlyName,
        string xml,
        CancellationToken cancellationToken = default)
    {
        var documentId = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(friendlyName))).ToLowerInvariant();
        return firestore.Collection(dataProtectionCollectionName)
            .Document(documentId)
            .SetAsync(new Dictionary<string, object>
            {
                ["friendlyName"] = friendlyName,
                ["xml"] = xml
            }, cancellationToken: cancellationToken);
    }
}
