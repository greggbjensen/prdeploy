---
name: github-app
description: Create, configure, and integrate GitHub Apps. Use when registering a GitHub App or app manifest, choosing permissions and webhook events, generating the app JWT, minting or caching installation access tokens, handling installation events, verifying webhook signatures, or storing app private keys and secrets. For user sign-in use github-oauth; for calling REST or GraphQL endpoints use github-api.
---

# GitHub App

Build GitHub App integrations that authenticate as the app or as an installation, receive webhooks safely, and request only the access they need.

Use this skill for app registration, app-level and installation authentication, and webhook handling. Do not use it for signing users in (see [github-oauth](../github-oauth/SKILL.md)) or for general API call patterns such as pagination and rate limits (see [github-api](../github-api/SKILL.md)).

## Before Changing Anything

1. Inspect the project's existing GitHub integration: SDK (for example Octokit), options/configuration classes, how secrets are loaded, client factories, webhook endpoints, and test doubles. Extend these rather than adding a parallel mechanism.
2. Identify which identity each operation needs:
   - **App (JWT):** manage the app itself, list installations, create installation tokens.
   - **Installation token:** act on repositories and organizations where the app is installed (automation, bots, webhook reactions).
   - **User access token:** act on behalf of a signed-in user, limited by both the user's and the app's access. See [github-oauth](../github-oauth/SKILL.md).
3. Confirm the target host. GitHub Enterprise Server and data-residency hosts use different API and web base URLs; keep them configurable.

## Registering or Changing the App

- Prefer a GitHub App over an OAuth app or personal access token for new integrations: it has fine-grained permissions, per-installation scope, and short-lived tokens.
- Request the minimum repository, organization, and account permissions. Use read-only access where writes are not required. Document why each permission and webhook event is needed.
- Subscribe only to webhook events the code handles. Each event requires its matching permission.
- Treat permission changes as a breaking operational change: existing installations must approve new permissions before tokens include them. Update setup docs and call out the change in the PR.
- When the project automates registration, use the [app manifest flow](https://docs.github.com/en/apps/sharing-github-apps/registering-a-github-app-from-a-manifest) and keep the manifest under version control without secrets.
- Do not register, delete, or change a live app's settings, or install it on accounts, without the user's explicit approval. These actions affect production access.

## Authenticating as the App

Follow [Generating a JWT for a GitHub App](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-json-web-token-jwt-for-a-github-app):

- Sign with **RS256** using the app's private key.
- `iat`: 60 seconds in the past to allow for clock drift.
- `exp`: no more than 10 minutes after the current time.
- `iss`: the app's **client ID** (recommended) or app ID. Do not confuse the client ID with the numeric app ID in configuration names.
- Send it as `Authorization: Bearer <jwt>`. JWTs do not work with `Authorization: token`.
- Prefer the SDK's built-in app authentication when available. If the project signs JWTs itself, inject the clock (for example `TimeProvider`) so expiry is testable.

## Installation Access Tokens

- Create them with `POST /app/installations/{installation_id}/access_tokens` using the app JWT. They expire after one hour.
- Cache per installation and refresh a few minutes before `expires_at`. Guard refreshes against concurrent callers so one expiry does not trigger many token requests.
- Scope tokens down when practical by passing `repositories`/`repository_ids` and a reduced `permissions` set.
- Get the installation ID from the webhook payload (`installation.id`) or from `GET /repos/{owner}/{repo}/installation` (and the org/user equivalents). Do not hard-code installation IDs.
- Handle `installation` and `installation_repositories` events (`created`, `deleted`, `suspend`, `unsuspend`, added/removed repositories) to keep any stored installation state current, and evict cached tokens for deleted or suspended installations.

## Webhooks

- Verify every delivery before parsing or acting on it: compute HMAC-SHA256 of the **raw request body** with the webhook secret and compare it with `X-Hub-Signature-256` using a constant-time comparison. Reject missing or invalid signatures with `401`/`403`. See [Validating webhook deliveries](https://docs.github.com/en/webhooks/using-webhooks/validating-webhook-deliveries).
- Route on `X-GitHub-Event` and the payload `action`. Ignore unhandled events with a `2xx` rather than failing.
- Respond within 10 seconds. Queue slow work and process it asynchronously.
- Treat deliveries as at-least-once and possibly out of order. Use `X-GitHub-Delivery` for idempotency and re-read current state from the API when ordering matters.
- Avoid feedback loops: ignore events caused by the app's own actions (for example check `sender.type == "Bot"` and the app's bot login) where a reaction would trigger another event.
- Treat payload content (titles, bodies, branch names, comments) as untrusted input. Never interpolate it into shell commands, queries, or templates without validation or encoding.

## Secrets and Configuration

- Keep the private key (PEM), webhook secret, and client secret out of source control, logs, exception messages, and test fixtures. Load them from the project's configured secret store or environment.
- Support supplying the PEM by value or by path only if the project already does; validate at startup with clear configuration errors that name the setting, not its value.
- Document key rotation: GitHub allows multiple active private keys, so add the new key, deploy, then delete the old one.
- Use a separate development app (and webhook proxy such as smee.io, if the project uses one) for local work. Never point local development at the production app's webhook URL.

## Testing

- Unit-test JWT claims (`iat`, `exp`, `iss`, algorithm) with a fixed clock and a throwaway test key generated in the test, not a committed real key.
- Test token caching: reuse before expiry, refresh inside the margin, and concurrent requests producing one token call.
- Test webhook signature verification with valid, invalid, missing, and altered-body cases.
- Mock GitHub HTTP calls or SDK interfaces following the project's existing test helpers. Do not call live GitHub from automated tests.

## References

- [About authentication with a GitHub App](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/about-authentication-with-a-github-app)
- [Authenticating as a GitHub App installation](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/authenticating-as-a-github-app-installation)
- [Choosing permissions for a GitHub App](https://docs.github.com/en/apps/creating-github-apps/registering-a-github-app/choosing-permissions-for-a-github-app)
- [Best practices for creating a GitHub App](https://docs.github.com/en/apps/creating-github-apps/about-creating-github-apps/best-practices-for-creating-a-github-app)
- [Webhook events and payloads](https://docs.github.com/en/webhooks/webhook-events-and-payloads)
