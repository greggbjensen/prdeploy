namespace PrDeploy.Api.Webhooks.Options;

/// <summary>
/// Settings for the GitHub App that sends webhook events and is used to act on repositories.
/// </summary>
public class GitHubAppOptions
{
    public const string SectionName = "GitHubApp";

    /// <summary>
    /// The ID of the GitHub App installed to your organization.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// The PEM contents of the GitHub App private key.  Takes precedence over <see cref="PrivateKeyPath"/>.
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Path to the PEM file of the GitHub App private key, used when <see cref="PrivateKey"/> is not set.
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// The webhook secret configured on the GitHub App, used to verify event signatures.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// The number of webhook events processed at the same time.
    /// </summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>
    /// The maximum number of webhook events waiting to be processed before new events are rejected.
    /// </summary>
    public int QueueCapacity { get; set; } = 1000;
}
