# prdeploy-api

GraphQL API supporting the web application for Engineers, QA, and Product Owners to be able to review and deploy services,
as well as the GitHub App webhooks that respond to pull request, comment, and workflow run events.

| Endpoint                | Purpose                                                                                    |
| ----------------------- | ------------------------------------------------------------------------------------------ |
| `/graphql`              | GraphQL API for the prdeploy portal, authenticated as the signed in GitHub user.           |
| `/api/oauth/*`          | GitHub OAuth token proxy and user info for the portal.                                     |
| `/api/deploy-status/{owner}/{repo}/events` | Server-Sent Events stream of deploy workflow status (`InProgress`, `Successful`, `Failure`) for the portal Deployments dashboard. |
| `/webhooks`             | GitHub App webhook URL, authenticated by the webhook secret signature.                     |
| `/health/live`, `ready` | Kubernetes health checks.                                                                  |

## Deploy status stream

Each environment in the deploy settings can define `urls`, a list of `label` and `url` entries (GraphQL `EnvironmentSettings.urls` / `EnvironmentUrlInput`). `deployEnvironments` returns them as `urls`; blank entries are skipped, and the legacy single `url` is returned as one entry labelled `URL` only when no `urls` are configured (`Map.EnvironmentUrls`).

Pull request users (`DeployUser`) include `avatarUrl` and `url` (the GitHub profile page), mapped from the GitHub user by `Map.DeployUser`.

The deploy workflow `workflow_run` webhooks (`requested`, `completed`) publish a status per environment to an in-memory `DeployStatusBroadcaster`. `GET /api/deploy-status/{owner}/{repo}/events` streams the latest status of each environment on connect, then every change. Statuses are held in memory (like the webhook queue, this relies on a single API replica) so an environment shows a status after its first deploy run since the API started. Browsers cannot send an `Authorization` header with `EventSource`, so the portal reads the stream with `fetch`. Reverse proxies must not buffer it; the API sends `X-Accel-Buffering: no`.

## OpenAPI

When running in the Development environment, the generated OpenAPI document is available at `http://localhost:4302/openapi/v1.json`. It documents the REST endpoints above; the GraphQL schema and operations are available through the `/graphql` endpoint.

## Projects

| Project                  | Purpose                                                                                       |
| ------------------------ | --------------------------------------------------------------------------------------------- |
| `PrDeploy.Api`           | ASP.NET host, GraphQL schema, authentication, and endpoint mapping.                           |
| `PrDeploy.Api.Business`  | Services for the GraphQL API and the configurable deployment document store.                  |
| `PrDeploy.Api.Models`    | Models shared by the GraphQL API and webhooks, including deploy settings and deploy state.    |
| `PrDeploy.Api.Webhooks`  | GitHub App webhook processing for comment commands, deploy status, queues, Slack, and Jira.   |

### Access control

Access is limited to the organizations listed in `GitHubAuth:AllowedOrganizations`. Set it in `appsettings.yaml` or `appsettings.Development.yaml`, as `GitHubAuth__AllowedOrganizations__0`, `__1`, … environment variables, or as `allowedOrganizations` in the Helm chart. Matching ignores case. A listed personal account (rather than an organization) is only available to that user. **While the list is empty, nobody can sign in.**

| Check      | Requirement                                                                                                                         | Enforced by                                    | On failure                                    |
| ---------- | ----------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- | --------------------------------------------- |
| Sign in    | The user is an **active** member of at least one allowed organization.                                                              | `GitHubAuthClient` via `OrganizationAccess`    | `403`; no JWT is issued and the portal shows "Access denied". |
| Owner      | The owner is allowed and the user is an active member of it.                                                                        | `GitHubSecurity.HasOwnerAccessAsync`           | `403` (owner-level settings).                 |
| Repository | Owner check, plus `GET /repos/{owner}/{repo}` reports `push`, `maintain`, or `admin` for the user (a contributor).                  | `GitHubSecurity.HasRepoAccessAsync`            | `403` for deployments, queues, and settings.  |

- `enabledOwnerRepos` returns only the enabled repositories the user has repository access to. An empty list is not an error: the portal keeps the user signed in and opens **Repositories** so they can add one.
- `ownerRepoAddEnabled` rejects a repository the user is not a contributor on (or whose owner they are not an allowed member of) with `success: false` and a `message`, and saves nothing. The add dialog shows that message and stays open.
- Enabling or disabling a repository updates the full stored list, so repositories other users enabled are kept.
- Granted checks are cached per token for 10 minutes. Denials are logged at `Information`; a failed GitHub call is logged at `Warning`, for example when an organization restricts OAuth app access.
- Owner and repository names must be valid GitHub names (owner: letters, digits, and hyphens; repository: letters, digits, `.`, `_`, and `-`, and not `.` or `..`). Anything else, such as a name containing `/`, is denied before GitHub is called, because names are used in storage keys and API paths.
- The deploy, free, rollback, queue, and add-services mutations post a slash command to the pull request as the user, after the repository check. The command must be a single line of word arguments and `--` flags; anything else, such as an environment containing a newline, returns `BAD_REQUEST` and nothing is posted.

### Settings secrets

The Jira token, Slack bot token, and Slack and Teams webhook URLs are never returned by the API. `repoSettings`, `ownerSettings`, and `deploySettingsCompare` return `********` for each one that is set (and `null` when it is not). When settings are saved, a value of `********` keeps the stored secret, so the portal can save the masked values it received; any other value replaces it.

A repository can override owner settings, but Jira credentials are only inherited when the repository keeps the owner's Jira host. Pointing a repository at a different Jira host requires its own username and token, so repository contributors cannot send the owner's credentials to another server.

Webhook processing only calls a Jira host or Slack webhook URL that is absolute `https` and is not a loopback, private, link-local (including cloud metadata), or unspecified IP address. Host names are not resolved, so this does not stop DNS rebinding; restrict outbound traffic from the API's network as well.

### Webhooks

1. `POST /webhooks` verifies the `X-Hub-Signature-256` signature, queues handled events, and returns `202 Accepted` right away.
2. A background worker processes queued events, `GitHubApp:MaxConcurrency` at a time, each in its own dependency injection scope.
3. Each event authenticates as the GitHub App installation that sent it, separate from the user authenticated `IGitHubClient` used by GraphQL.
4. Comment and Slack message templates are Handlebars files in `src/PrDeploy.Api.Webhooks/Templates`.

| Event                                      | Action                                                                  |
| ------------------------------------------ | ----------------------------------------------------------------------- |
| `pull_request.opened`, `edited`            | Adds Jira issue links from the branch name, when enabled.               |
| `pull_request.closed`                      | Frees environments, dequeues the next pull request, promotes to stable. |
| `issue_comment.created`                    | Runs `/deploy`, `/free`, `/rollback`, and `/add` commands from users with write access. |
| `workflow_run.requested`, `completed`      | Posts deploy status, triggers automation tests, retriggers deploys.     |

Comment commands only run when the comment author has `write`, `maintain`, or `admin` permission on the repository (`GET /repos/{owner}/{repo}/collaborators/{user}/permission`). Anyone who can read a repository, or any GitHub user on a public one, can comment, so commands from other users, bots, and apps are logged at `Warning` and ignored. `/deploy` and `/add` are also ignored for pull requests from forks, because deploys find checks and builds, and `/add` commits trigger files, by branch name in this repository; a fork pull request taken off a queue is not deployed and gets a failure comment instead.

Because the queue and some caches are in memory, the API runs as a single replica.

## Development

1. Create `src/PrDeploy.Api/appsettings.Development.yaml` (it is gitignored) with your local `GitHubAuth`, `GitHubApp`, and `Jwt` values, including a non-empty `Jwt:Key` (at least 16 characters). The API fails during startup if the signing key is missing. See [Application configuration](../docs/getting-started/2-github-oauth-app.md#4-application-configuration-local-development) for the full set of keys. `.env` files are not read.
2. Download the private key from your organization's GitHub App page and save it as `gh-app-key-local.pem` in this folder, or set `GitHubApp:PrivateKey` to its contents.
3. BLite is the default local storage provider for deployment values and Data Protection keys; local startup does not require cloud credentials. Use `Storage:Provider` to select a cloud provider.
4. Start the API with `dotnet run --project src/PrDeploy.Api`, which listens on `http://localhost:4302`.

### Storage providers

`Storage:Provider` defaults to `Local` (BLite). Supported values are `Local`, `Azure`, `AWS`, and `GCP`; `BLite` and the legacy value `LiteDB` are also accepted as aliases for `Local`. Provider settings and connection details are configured in the `Storage` section of `appsettings.yaml`. `Storage:DeploymentValuePathPrefix` defaults to `/prdeploy` and scopes owner/repository values; the old `Aws:SecretPathPrefix` remains a compatibility fallback. Only the selected backend is registered; Local does not initialize cloud SDK clients or resolve cloud credentials.

- **Local / BLite:** deployment values (YAML text plus every historical revision) and Data Protection key XML are stored in one BLite file. By default it is `${LocalApplicationData}/prdeploy/deployment-storage.blite`; override with `Storage:Local:DatabasePath`. Set `Storage:Local:Password` to enable BLite AES-256-GCM encryption at rest. If the file does not exist at startup, the API creates it (and any missing parent directories). Protect the file and backups; `isSecure` is metadata and does not independently encrypt values. Existing LiteDB files are not compatible with BLite and are not migrated automatically; back up and export existing data before switching.
- **Azure / Cosmos DB:** configure `Storage:Azure:ConnectionString`, or `Storage:Azure:Endpoint` and `Storage:Azure:Key`. Optional `Storage:Azure:DatabaseName` and `Storage:Azure:ContainerName` default to `prdeploy` and `storage`. The service creates the database and container if missing. The container uses `/partitionKey`, with each deployment-value path as a partition key and a dedicated partition for Data Protection keys.
- **AWS / DynamoDB:** configure `Storage:AWS:Region` and optional `Storage:AWS:TableName` (default `PrDeployStorage`); credentials use the normal AWS SDK credential chain. Pre-create a table with string partition key `PK` and string sort key `SK`, on-demand capacity, and grant the service role `dynamodb:PutItem` and `dynamodb:Query` for that table. Deployment values are versioned items and Data Protection key XML is persisted in the same table. The chart retains the existing AWS Systems Manager Parameter Store Data Protection key path under `Storage:AWS:DataProtectionParameterPath` for continuity, but application deployment values use DynamoDB.
- **GCP / Firestore:** configure `Storage:GCP:ProjectId` and use Application Default Credentials. Firestore must be enabled and its default database provisioned. Optional `Storage:GCP:CollectionName` defaults to `prdeploy_storage`; key XML is kept in a companion collection. Create the Firestore composite index for `path` (ascending) and `revision` (descending) used to read revision history.

All four providers store deployment-value YAML and historical revisions, and persist the Data Protection XML key ring. The `isSecure` flag is retained as metadata. Cloud providers rely on each cloud database's at-rest encryption and IAM controls; configure those controls for your environment. Switching providers does not copy data. Before switching an existing deployment, migrate deployment-value history and Data Protection keys intentionally; otherwise previously encrypted payloads may no longer be readable. The Helm chart selects AWS and expects the DynamoDB table and IAM permissions described above.

### Receiving webhooks locally

The GitHub App Webhook must be configured to send events to a URL that is accessible from the internet, such as [smee](https://smee.io/).

1. Create a channel on [smee.io](https://smee.io/) and set it as the **Webhook URL** of a development GitHub App.
2. Forward events to the API with:

   ```bash
   npx smee-client -u https://smee.io/<channel> -t http://localhost:4302/webhooks
   ```

## Configuration files

Settings live in YAML, not JSON: `appsettings.yaml` and `appsettings.{Environment}.yaml` in `src/PrDeploy.Api`, plus the optional `secrets/appsettings.secret.yaml` mounted by the Helm chart (`charts/prdeploy-api/secrets/`). `YamlConfigurationProvider` (in `Configuration/`) flattens mappings and sequences into the usual `Section:Key` / `Section:0` keys, and `UseYamlAppSettings` swaps the default JSON sources in place, so precedence is unchanged: YAML files, then user secrets, environment variables, and the command line. Unquoted empty values and `~` are null; quote values (`""`) that must be empty strings. JSON appsettings files are no longer read.

## Microsoft Teams settings

`DeploySettings.Teams` (`TeamsSettings`) stores `NotificationsEnabled` and `Webhooks.DeployUrl` / `Webhooks.ReleaseUrl`. They are exposed through the same `deploySettingsCompare` query and `deploySettingsSet` mutation as the Slack settings, and merged from owner to repo like them. Only storage is implemented; the API does not post to Teams yet.

## Logging

The `repositoryWorkflows` GraphQL query lists the files in a repository's `.github/workflows` folder (for the portal's Add Service dialog), using the signed in user's GitHub token after the same repository access check as the other repository queries. Every YAML file is listed, and a repository without the folder returns an empty list.

Build names come from the mapped **Services**: a build's workflow file name is matched (case-insensitively) to a service's `workflow`, and that service's name is used. A workflow that is not mapped falls back to its file name without the extension and without a trailing `-ci` or `-build`. The old `builds.workflowPattern` and `builds.checkPattern` settings no longer exist; leftover values in stored settings are ignored.

The API uses Serilog (built by `SerilogConfiguration`, which both the bootstrap logger and the host logger in `Program.cs` use to read the `Serilog` section of the `appsettings.yaml` files) and writes to the console: colorized, human-readable text in the `Development` environment and structured JSON (one document per event) in every other environment, selected in code by `SerilogConfiguration.Configure(..., prettyConsole)` rather than by configuration. All application code logs through `ILogger<T>` using **message templates** with named placeholders (`"Updated repo settings for {Owner}/{Repo}."`), never string interpolation, so every value is a queryable property.

- **Levels:** `Information` for state changes and business events (deploys, settings changes, tokens issued), `Warning` for denied or rejected requests and expected client errors, `Error` for unexpected failures, and `Debug` for reads, cache hits, and timings. The default minimum level is `Information`; raise verbosity with `Serilog:MinimumLevel:Default` (for example `Debug`). Framework namespaces (`Microsoft`, `System`) are limited to `Warning` by `Serilog:MinimumLevel:Override`, except `Microsoft.Hosting.Lifetime` (startup and listening addresses). `appsettings.Development.yaml` also enables `Information` for `Microsoft.AspNetCore`; to see more framework logs elsewhere, add an override such as `Serilog__MinimumLevel__Override__Microsoft.AspNetCore=Information`.
- **Context:** authenticated requests run inside a scope with the `User` property (the GitHub login), and the request summary line includes it. Webhook processing adds `Owner`, `Repo`, and `PullNumber` scopes.
- **Storage:** every `IDeploymentStore` provider is wrapped by `LoggingDeploymentStore`, which logs the owner, repo, value name, revision offset, and duration at `Debug` and failures at `Error`.
- **Errors:** a `catch` block never swallows an exception silently. Handled failures log a template with the exception attached (`LogWarning(ex, ...)`) plus identifying properties such as `Event`, `DeliveryId`, `Owner`, `Repo`, and `StatusCode`, for example rejected webhook deliveries (invalid JSON, missing event header, bad signature), unparseable GitHub token responses, and fallbacks to the default branch.
- **Never logged:** GitHub or JWT tokens, client secrets, and stored values (they may be secure). Log identifiers, counts, and statuses only.

## Testing

Run all tests with `dotnet test`, or only the webhook tests with:

```bash
dotnet test --filter "FullyQualifiedName~Webhooks"
```

Prefer functional tests that call the API over HTTP and mock only external services:

- **GraphQL and REST:** derive from `DeployApiTest`, or use `SecuredApiApplicationFactory` to run the real repository and organization access checks with a mocked GitHub client. `ApiSecurityFunctionalTest` shows the pattern: it posts GraphQL operations to `/graphql` and asserts the response and the GitHub calls.
- **Webhooks:** `WebhookCommandFunctionalTest` posts signed payloads to `POST /webhooks` and waits for the background worker. `WebhookTestHost` runs the real webhook services with a mocked GitHub client and HTTP handler for focused scenarios.

Every test class that starts the API host must be in the `[Collection(ApiHostCollection.Name)]` collection. Hosts replace the static Serilog bootstrap logger on startup, so starting two at once fails with "The logger is already frozen."

## Deployment

### Persistent storage for the Local provider

- **Docker:** the image sets `Storage__Local__DatabasePath=/app/data/deployment-storage.blite` and declares `/app/data` as a volume. `docker-compose.yaml` mounts the named volume `prdeploy-data` there, so data survives container replacement. An empty volume gets a new database on first start.
- **Helm:** set `persistence.enabled=true` (with `storage` provider `Local`) in the `prdeploy-api` values to create a PersistentVolumeClaim (`persistence.size`, `persistence.storageClass`, `persistence.accessMode`, `persistence.mountPath`). The deployment then uses the `Recreate` strategy so pods do not overlap while sharing the local database volume. The chart's default provider is AWS, so persistence is disabled by default.

This application is deployed to a kubernetes cluster using the `prdeploy` helm chart, which routes `/webhooks`, `/api`, and `/graphql` to it.
