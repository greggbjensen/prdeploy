---
name: secure-github-actions
description: Security checklist for GitHub Actions CI/CD in .github/workflows — token permissions, untrusted-input injection, action pinning, OIDC deploys, secrets and environments, dependency and artifact integrity.
---

# Secure GitHub Actions

Use for any workflow edit. Pair with `github-actions-cicd` (conventions) and `secure-docker`.

## Permissions & triggers

- Use workflow-level `permissions: contents: read` by default; elevate **per job**
  (`id-token: write` only on publish/deploy jobs). Never `permissions: write-all`.
- Avoid `pull_request_target` and `workflow_run` with checkout of PR code; they run with secrets on
  untrusted input. Fork PRs get no secrets, no deploy.
- Keep deployment triggers and environment protection aligned with repository policy. Untrusted pull
  requests should not receive deployment credentials.
- Protect the default branch with required reviews and status checks; use CODEOWNERS for workflow,
  infrastructure, and container-build files when the repository has ownership rules.

## Script injection

- Never interpolate untrusted context into `run:` — `${{ github.event.pull_request.title }}`,
  `head_ref`, `issue.body`, branch/commit names, artifact contents. Pass via `env:` and quote
  `"$VAR"`, or use an action input.
- `GITHUB_ENV`/`GITHUB_OUTPUT` writes from untrusted data can inject variables; sanitize or avoid.

## Supply chain

- Pin third-party actions to a **full commit SHA** (comment the version), following repository policy.
  Use Dependabot or Renovate to keep actions and lockfiles current.
- Installs use frozen modes: `npm ci`, `pip install --require-hashes` or `uv sync --frozen`, lockfiles
  committed. No `curl | sh`; checksum-verify downloaded tools.
- Build provenance/SBOM and image signing (`cosign`, `actions/attest-build-provenance`) for prod images;
  deploy by digest, not mutable tag.
- Package scanning in CI: `npm audit --omit=dev`, `pip-audit`, and a container scanner such as Trivy
  are CI tools, not shipped dependencies. Follow applicable licensing policy for shipped packages.

## Cloud auth & secrets

- Prefer the cloud provider's OIDC integration with `id-token: write` scoped to jobs that need it.
  Avoid long-lived client secrets or publish profiles; scope federated identities to the repository,
  environment, and minimum required permissions.
- Secrets scoped to environments, not repo-wide; never echoed (`::add-mask::` for derived values); no
  `set -x` around secrets; don't pass secrets as build args (see `secure-docker`).
- Self-hosted runners are ephemeral and not shared across trust levels (prefer GitHub-hosted).
- Debug flags (`ACTIONS_STEP_DEBUG`) off in prod runs.

## Lint & tests

- Run `actionlint` and check for unpinned actions and expression injection with a tool such as zizmor.
- Avoid live external-service calls in CI unless a test is explicitly designed as an integration test
  with isolated credentials and infrastructure.

## Review checklist

- [ ] Minimal `permissions`, elevated per job only
- [ ] No untrusted `${{ }}` in `run:`; no `pull_request_target` + PR checkout
- [ ] Third-party actions SHA-pinned; frozen installs
- [ ] Short-lived cloud credentials with least privilege; no unnecessary stored keys
- [ ] Production deployment protected by the repository's required approvals; immutable artifacts where practical

## Provenance

`UnitOneAI/SecuritySkills` pipeline-security (MIT, OWASP CICD-SEC / SLSA), `agamm/claude-code-owasp`
config-and-supply-chain (MIT), `mukul975/Anthropic-Cybersecurity-Skills` CI/CD supply-chain skill
(Apache-2.0, description only), GitHub Actions hardening guide.
