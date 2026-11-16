using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Provides document operations for deployment values and Data Protection keys in DynamoDB.
/// </summary>
public sealed class DynamoDbDocumentStorage(IAmazonDynamoDB dynamoDb, string tableName)
{
    private const string DataProtectionPartition = "DATA_PROTECTION";

    /// <summary>
    /// Retrieves a deployment value revision by its storage path.
    /// </summary>
    public async Task<Dictionary<string, AttributeValue>?> GetDeploymentValueAsync(
        string path,
        int historical,
        CancellationToken cancellationToken = default)
    {
        var remaining = Math.Max(historical, 0);
        Dictionary<string, AttributeValue>? exclusiveStartKey = null;
        var pageSize = remaining >= 1000 ? 1000 : remaining + 1;
        do
        {
            var response = await dynamoDb.QueryAsync(new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new() { S = DeploymentPartition(path) },
                    [":prefix"] = new() { S = "REV#" }
                },
                ScanIndexForward = false,
                Limit = pageSize,
                ExclusiveStartKey = exclusiveStartKey,
                ConsistentRead = true
            }, cancellationToken).ConfigureAwait(false);

            if (response.Items.Count > remaining)
            {
                return response.Items[remaining];
            }

            remaining -= response.Items.Count;
            exclusiveStartKey = response.LastEvaluatedKey;
        }
        while (exclusiveStartKey is { Count: > 0 });

        return null;
    }

    /// <summary>
    /// Appends a deployment value revision to its path partition.
    /// </summary>
    public Task SetDeploymentValueAsync(
        string path,
        string value,
        bool isSecure,
        CancellationToken cancellationToken = default)
    {
        var revision = DateTime.UtcNow.Ticks;
        return dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = DeploymentPartition(path) },
                ["SK"] = new() { S = $"REV#{revision:D19}#{Guid.NewGuid():N}" },
                ["Path"] = new() { S = path },
                ["Value"] = new() { S = value },
                ["IsSecure"] = new() { BOOL = isSecure },
                ["Revision"] = new() { N = revision.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Reads the XML from all persisted Data Protection key records.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetDataProtectionKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var xmlElements = new List<string>();
        Dictionary<string, AttributeValue>? exclusiveStartKey = null;
        do
        {
            var response = await dynamoDb.QueryAsync(new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new() { S = DataProtectionPartition },
                    [":prefix"] = new() { S = "KEY#" }
                },
                ScanIndexForward = true,
                ExclusiveStartKey = exclusiveStartKey,
                ConsistentRead = true
            }, cancellationToken).ConfigureAwait(false);

            xmlElements.AddRange(response.Items
                .Where(item => item.TryGetValue("Xml", out var xml) && !string.IsNullOrEmpty(xml.S))
                .Select(item => item["Xml"].S));
            exclusiveStartKey = response.LastEvaluatedKey;
        }
        while (exclusiveStartKey is { Count: > 0 });

        return xmlElements;
    }

    /// <summary>
    /// Adds or replaces a Data Protection key record by its friendly name.
    /// </summary>
    public Task StoreDataProtectionKeyAsync(
        string friendlyName,
        string xml,
        CancellationToken cancellationToken = default) =>
        dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = DataProtectionPartition },
                ["SK"] = new() { S = $"KEY#{friendlyName}" },
                ["FriendlyName"] = new() { S = friendlyName },
                ["Xml"] = new() { S = xml }
            }
        }, cancellationToken);

    /// <summary>
    /// Creates the partition key that isolates revisions for one deployment-value path.
    /// </summary>
    private static string DeploymentPartition(string path) => $"DEPLOYMENT#{path}";
}
