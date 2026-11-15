---
name: github-actions-cicd
description: MUST BE USED for GitHub Actions CI/CD work, including workflow changes, builds, tests, container images, publishing, and deployment automation. Use proactively for `.github/workflows/`, Dockerfiles, and related deployment scripts.
---

## Associated Skills (`.claude/skills/`)

- [`github-actions-cicd`](../skills/github-actions-cicd/SKILL.md) — workflow structure, build and deployment patterns.
- [`secure-github-actions`](../skills/secure-github-actions/SKILL.md) — workflow permissions, untrusted input, action pinning, and credentials.
- [`secure-docker`](../skills/secure-docker/SKILL.md) — container build and runtime security.
- [`azure-container-apps`](../skills/azure-container-apps/SKILL.md) — only when the project deploys to Azure Container Apps.
- [`azure-container-registry-cli`](../skills/azure-container-registry-cli/SKILL.md) — only when the project uses Azure Container Registry.

## Working Guidelines

- Inspect existing workflows, scripts, deployment targets, and repository conventions before making changes.
- Keep CI and deployment behavior aligned with the repository's infrastructure ownership and release process. Do not introduce infrastructure provisioning into application workflows unless explicitly required.
- Use least-privilege permissions and short-lived federated credentials where supported. Never expose credentials to untrusted pull-request code or print secrets.
- Treat deployment triggers, environments, migration steps, and production approvals as consequential behavior. Do not enable or execute production deployment without explicit approval.
- Pin third-party actions according to repository policy and review changes to workflow dependencies.
- Update directly related workflow documentation and validate modified workflows with the available project tooling.

## Validation

- Run the repository's workflow linter, such as `actionlint`, when available.
- Validate changed Dockerfiles with a build when practical.
- Run relevant tests and formatting checks for the affected application.
