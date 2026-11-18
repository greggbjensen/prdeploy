---
name: github-api
description: Call the GitHub REST and GraphQL APIs correctly and efficiently. Use when adding or changing code that reads or writes GitHub data (repositories, pull requests, issues, checks, statuses, deployments, comments), choosing between REST and GraphQL, setting API version headers, paginating, handling rate limits and errors, or mocking GitHub in tests. For obtaining tokens use github-app or github-oauth.
---

# GitHub REST and GraphQL APIs

Make GitHub API calls that use the right identity, are versioned, paginate fully, respect rate limits, and are testable without live GitHub.

Use this skill for API call patterns. Do not use it to obtain app or installation tokens (see [github-app](../github-app/SKILL.md)) or user tokens (see [github-oauth](../github-oauth/SKILL.md)).

## Before Changing Anything

1. Inspect how the project already calls GitHub: SDK (for example Octokit for .NET or JavaScript), client factories, wrapper services, base URL configuration, retry policies, and test mocks. Add calls through these layers rather than creating new raw HTTP clients.
2. Confirm which token the call should use (installation, user, or app JWT) and that the app's permissions cover it. Check each endpoint's "Fine-grained access tokens" section in the REST docs for the required permission.
3. Look up the endpoint in the [REST API reference](https://docs.github.com/en/rest) or [GraphQL reference](https://docs.github.com/en/graphql) rather than guessing paths, fields, or SDK method names.

## Requests

- Send `Accept: application/vnd.github+json`, a descriptive `User-Agent` (required), and `Authorization: Bearer <token>`.
- Pin `X-GitHub-Api-Version` to the version the project uses. Without the header GitHub uses `2022-11-28`; newer versions exist and older ones are retired 24 months after a successor is released (`410 Gone`). Check [API versions](https://docs.github.com/en/rest/about-the-rest-api/api-versions) and the breaking-changes list before upgrading.
- Keep the API base URL configurable for GitHub Enterprise Server (`https://<host>/api/v3`, GraphQL at `https://<host>/api/graphql`).
- URL-encode path segments such as branch names and file paths. Treat owner, repo, and ref values from users or webhooks as untrusted.

## Choosing REST or GraphQL

- Use REST for simple reads and writes, endpoints without GraphQL equivalents, and when the project's SDK already covers the call.
- Use GraphQL to fetch nested data in one request and avoid N+1 REST calls. Request only needed fields, use variables instead of string interpolation, and paginate connections with `first`/`after` and `pageInfo { hasNextPage endCursor }`.
- Remember GraphQL returns HTTP 200 with an `errors` array for many failures; check it.

## Pagination

- Never assume one page is complete. Follow the `Link` header's `rel="next"` for REST (use `per_page=100` where supported) or the SDK's pagination helpers.
- Bound pagination for large collections (maximum pages or items) and prefer filtered or search endpoints over listing everything.

## Rate Limits and Efficiency

- Read `x-ratelimit-remaining`, `x-ratelimit-reset`, and `retry-after`. On `403`/`429` from primary or secondary limits, wait until reset or `retry-after` and retry with backoff; do not hammer the API.
- Avoid secondary limits: serialize write-heavy operations (comments, labels, statuses), avoid many concurrent requests per token, and pause about a second between content-creating requests.
- Use conditional requests (`ETag` with `If-None-Match`) for polled resources; `304` responses do not count against the primary limit.
- Prefer webhooks over polling. Cache stable data (for example repository IDs, default branches) for an appropriate period.
- See [Best practices for using the REST API](https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api) and [Rate limits](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api).

## Errors

- `401`: token invalid, expired, or revoked; refresh or re-authenticate rather than retrying blindly.
- `403`: missing permission, SSO authorization needed, or rate limited; inspect the body and headers to tell them apart.
- `404`: the resource may exist but the token cannot see it. Report "not found or not accessible" instead of assuming it does not exist.
- `409`/`422`: conflict or validation failure; surface GitHub's `message` and `errors` safely.
- Retry only idempotent requests and transient failures (`5xx`, rate limits) with bounded backoff.
- Do not include tokens or full request headers in logs or user-facing errors.

## Safety

- Writes to GitHub (comments, statuses, merges, branch or release changes, workflow dispatch) are externally visible. Do not run them against real repositories during development or tests without the user's approval.
- Use a dedicated test repository or organization for manual verification.

## Testing

- Mock the SDK interfaces or HTTP handler following the project's existing GitHub test helpers; never call live GitHub in automated tests.
- Cover multi-page responses, empty results, rate-limit responses, `401/403/404/422` paths, and GraphQL `errors` arrays.
- Keep fixture payloads minimal and free of real user data or tokens.

## References

- [Getting started with the REST API](https://docs.github.com/en/rest/using-the-rest-api/getting-started-with-the-rest-api)
- [Using pagination in the REST API](https://docs.github.com/en/rest/using-the-rest-api/using-pagination-in-the-rest-api)
- [Troubleshooting the REST API](https://docs.github.com/en/rest/using-the-rest-api/troubleshooting-the-rest-api)
- [GraphQL rate and query limits](https://docs.github.com/en/graphql/overview/rate-limits-and-query-limits-for-the-graphql-api)
