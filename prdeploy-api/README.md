# prdeploy-api

GraphQL API supporting the web application for Engineers, QA, and Product Owners to be able to review and deploy services,
as well as the GitHub App webhooks that respond to pull request, comment, and workflow run events.

| Endpoint                | Purpose                                                                                    |
| ----------------------- | ------------------------------------------------------------------------------------------ |
| `/graphql`              | GraphQL API for the prdeploy portal, authenticated as the signed in GitHub user.           |
| `/api/oauth/*`          | GitHub OAuth token proxy and user info for the portal.                                     |
| `/webhooks`             | GitHub App webhook URL, authenticated by the webhook secret signature.                     |
| `/health/live`, `ready` | Kubernetes health checks.                                                                  |

## OpenAPI

When running in the Development environment, the generated OpenAPI document is available at `http://localhost:4302/openapi/v1.json`. It documents the REST endpoints above; the GraphQL schema and operations are available through the `/graphql` endpoint.

## Projects

| Project                  | Purpose                                                                                       |
| ------------------------ | --------------------------------------------------------------------------------------------- |
| `PrDeploy.Api`           | ASP.NET host, GraphQL schema, authentication, and endpoint mapping.                           |
| `PrDeploy.Api.Business`  | Services for the GraphQL API and the shared AWS Parameter Store access.                       |
| `PrDeploy.Api.Models`    | Models shared by the GraphQL API and webhooks, including deploy settings and deploy state.    |
| `PrDeploy.Api.Webhooks`  | GitHub App webhook processing for comment commands, deploy status, queues, Slack, and Jira.   |

### Webhooks

1. `POST /webhooks` verifies the `X-Hub-Signature-256` signature, queues handled events, and returns `202 Accepted` right away.
2. A background worker processes queued events, `GitHubApp:MaxConcurrency` at a time, each in its own dependency injection scope.
3. Each event authenticates as the GitHub App installation that sent it, separate from the user authenticated `IGitHubClient` used by GraphQL.
4. Comment and Slack message templates are Handlebars files in `src/PrDeploy.Api.Webhooks/Templates`.

| Event                                      | Action                                                                  |
| ------------------------------------------ | ----------------------------------------------------------------------- |
| `pull_request.opened`, `edited`            | Adds Jira issue links from the branch name, when enabled.               |
| `pull_request.closed`                      | Frees environments, dequeues the next pull request, promotes to stable. |
| `issue_comment.created`                    | Runs `/deploy`, `/free`, `/rollback`, and `/add` commands.              |
| `workflow_run.requested`, `completed`      | Posts deploy status, triggers automation tests, retriggers deploys.     |

Because the queue and some caches are in memory, the API runs as a single replica.

## Development

1. Create a `.env` file in `src/PrDeploy.Api` similar to `.env.sample` and set actual values.
2. Download the private key from your organization's GitHub App page and save it as `gh-app-key-local.pem` in this folder, or set `GitHubApp__PrivateKey` to its contents.
3. Make sure you have AWS credentials for Parameter Store, such as an AWS profile.
4. Start the API with `dotnet run --project src/PrDeploy.Api`, which listens on `http://localhost:4302`.

### Receiving webhooks locally

The GitHub App Webhook must be configured to send events to a URL that is accessible from the internet, such as [smee](https://smee.io/).

1. Create a channel on [smee.io](https://smee.io/) and set it as the **Webhook URL** of a development GitHub App.
2. Forward events to the API with:

   ```bash
   npx smee-client -u https://smee.io/<channel> -t http://localhost:4302/webhooks
   ```

## Testing

Run all tests with `dotnet test`, or only the webhook tests with:

```bash
dotnet test --filter "FullyQualifiedName~Webhooks"
```

## Deployment

This application is deployed to a kubernetes cluster using the `prdeploy` helm chart, which routes `/webhooks`, `/api`, and `/graphql` to it.
