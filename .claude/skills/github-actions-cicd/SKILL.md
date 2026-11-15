---
name: github-actions-cicd
description: General guidance for GitHub Actions CI/CD workflows, application builds, tests, container images, publishing, deployment automation, and workflow validation.
---

# GitHub Actions CI/CD

## Workflow Design

- Inspect the repository's existing workflow layout and release process before adding or changing workflows.
- Use clear triggers and, where useful, path filters to avoid running unrelated work.
- Keep workflows focused on a coherent application or task; share reusable workflows or actions when they reduce duplication without obscuring ownership.
- Make jobs, dependencies, artifacts, and deployment conditions explicit.
- Match workflow behavior to the project's infrastructure and deployment tooling. Do not provision infrastructure from an application workflow unless that is an intentional, documented part of the design.

## Authentication and Permissions

- Grant the minimum required `GITHUB_TOKEN` permissions, at workflow or job scope.
- Prefer short-lived federated identity such as OIDC for cloud access when supported; scope it to the relevant job and environment.
- Never expose secrets to untrusted pull-request code, print them, or pass them in build arguments.
- Require appropriate environment protections and human approval for production deployments when the project uses them.

## Builds and Deployments

- Use the repository's lockfiles and reproducible dependency installation modes.
- Build and test before publishing artifacts. Identify artifacts immutably and promote the same artifact between environments when practical.
- Run database migrations and other stateful changes in the order required by the application's deployment design.
- Use caching only for reproducible, non-sensitive build inputs; do not cache credentials or generated dependency directories unless the package manager specifically supports it.
- Pin third-party actions according to repository policy and review action upgrades.

## Validation

- Lint modified workflow files with `actionlint` when available.
- Validate changed Dockerfiles with a build when practical.
- Run relevant application tests and formatting checks.
- Update workflow documentation when the repository maintains it.
