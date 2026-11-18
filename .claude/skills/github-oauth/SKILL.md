---
name: github-oauth
description: Implement "Sign in with GitHub" and user access tokens using GitHub App or OAuth app flows. Use when adding or changing GitHub login, the authorization redirect, state and PKCE handling, the code-for-token exchange, device flow, token refresh, revocation, or how user tokens are stored and passed between a frontend and backend. For app/installation authentication use github-app; for calling GitHub endpoints use github-api.
---

# GitHub OAuth and User Access Tokens

Sign users in with GitHub and obtain user access tokens securely, keeping secrets on the server and tokens short-lived.

Use this skill for user-to-server authentication. Do not use it for app JWTs or installation tokens (see [github-app](../github-app/SKILL.md)) or for API request patterns (see [github-api](../github-api/SKILL.md)).

## Before Changing Anything

1. Inspect the existing login implementation end to end: frontend auth library and configuration, callback route, backend token-exchange endpoint, session or JWT issuance, token storage, and tests. Extend it rather than adding a second flow.
2. Determine whether the project uses a **GitHub App** (recommended: fine-grained, expiring tokens, `ghu_` prefix) or an **OAuth app** (scopes, non-expiring `gho_` tokens). Do not mix their assumptions: GitHub App user tokens ignore OAuth scopes and are limited by the app's permissions and the installations the user can access.
3. Confirm the host. GitHub Enterprise Server uses its own `https://<host>/login/oauth/...` endpoints; keep base URLs configurable.

## Web Application Flow

Follow [Generating a user access token for a GitHub App](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-user-access-token-for-a-github-app) (OAuth apps use the same endpoints with `scope`).

1. **Authorize:** redirect to `https://github.com/login/oauth/authorize` with:
   - `client_id` (the client ID, not the app ID)
   - `redirect_uri` exactly matching a registered callback URL
   - `state`: an unguessable value bound to the user's session
   - `code_challenge` + `code_challenge_method=S256` (PKCE; `plain` is not supported)
   - Optional: `login`, `allow_signup=false`, `prompt=select_account`; for OAuth apps, the minimum `scope`.
2. **Callback:** abort if `state` is missing or does not match. Handle `error` query parameters (for example `access_denied`) without leaking details.
3. **Exchange:** from the **backend**, `POST https://github.com/login/oauth/access_token` with `client_id`, `client_secret`, `code`, `redirect_uri`, and `code_verifier`. Send `Accept: application/json`. The endpoint returns errors with HTTP 200 and an `error` field, so check the body, not just the status.
4. **Use:** call the API with `Authorization: Bearer <token>` and fetch `GET /user` to identify the user. Key users by numeric `id`, not `login`, because logins can change.

Never put the client secret in browser, mobile, or desktop code, and never let the browser call the token endpoint directly. Public clients exchange the code through a backend you control.

## Device Flow

For CLIs and headless tools, use the [device flow](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#device-flow) (must be enabled in app settings): request a device code, show `user_code` and `verification_uri`, then poll the token endpoint at the returned `interval`. Handle `authorization_pending`, `slow_down` (add 5 seconds), `expired_token`, `access_denied`, and `device_flow_disabled`.

## Token Lifetime, Refresh, and Revocation

- GitHub App user tokens expire after 8 hours with a 6-month refresh token when expiration is enabled. Refresh with `grant_type=refresh_token` server-side, replace both tokens atomically, and re-authenticate when the refresh token is rejected.
- Handle `401 Bad credentials` by treating the token as revoked or expired. Subscribe to `github_app_authorization` (GitHub App) to learn when a user revokes access and delete stored tokens.
- Provide sign-out that clears the app session and, where appropriate, revokes the token with `DELETE /applications/{client_id}/token` (basic auth with client ID and secret).
- Organizations using SAML SSO may require the user to authorize or start an SSO session before org resources are visible; surface this rather than reporting "not found" as a bug.

## Storing and Passing Tokens

- Prefer keeping GitHub tokens server-side and giving the client a project-issued session or JWT. If the client must hold a GitHub token, keep it in memory or the project's established secure storage, never in URLs, logs, or analytics.
- Encrypt tokens at rest using the project's existing data-protection or cipher service.
- Do not log authorization codes, `state`, verifiers, tokens, or the token endpoint's request/response bodies. Sanitize errors returned to clients.
- Validate inputs to the exchange endpoint (non-empty code, expected redirect URI) and rate-limit it if the project has a mechanism.

## Testing

- Unit-test the exchange endpoint with a mocked HTTP client: success, `error` in a 200 body, non-2xx responses, invalid input, and missing configuration.
- Test `state` and PKCE generation and validation, including mismatched and missing values.
- Test refresh and revocation handling, including the expired-refresh path.
- Use placeholder tokens with realistic prefixes only (for example `ghu_test`). Never commit real client secrets or tokens.
- When the project has an API request collection (for example Bruno), add or update requests for first-party auth endpoints using environment variables for secrets.

## References

- [Differences between GitHub Apps and OAuth apps](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/differences-between-github-apps-and-oauth-apps)
- [Refreshing user access tokens](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/refreshing-user-access-tokens)
- [Authorizing OAuth apps](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
- [Best practices for creating an OAuth app](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/best-practices-for-creating-an-oauth-app)
