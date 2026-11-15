---
name: pull-request
description: Create a GitHub pull request from the current branch. Use when asked to open, create, or submit a pull request, including requests to run checks, prepare a title and description, and publish the PR.
---

# Pull Request

Prepare and create a pull request for the current branch using the repository's conventions and GitHub CLI. Inspect the actual changes and run the applicable checks; do not rely on commit messages alone.

## Workflow

1. Inspect the current branch, worktree, upstream, repository default branch, recent commits, and diff against the target base. Prefer the repository's documented base branch; otherwise discover its default branch.
2. Check for uncommitted changes. Do not include unrelated or user-owned edits without confirming they belong in the PR. Do not discard or overwrite user changes.
3. Determine the full change range from the merge base with the target branch. Read the diff and relevant files to understand the behavior and classify changes accurately.
4. Run the smallest repository-prescribed checks that cover the change. If they fail, distinguish failures caused by the branch from unrelated or pre-existing failures. Do not disable checks or broaden scope to hide failures.
5. Draft a concise title following repository conventions. Write a description that explains the purpose and summarizes the changes; include actionable test steps when useful and only when supported by the implementation.
6. Check whether the branch is pushed and has an upstream. If publishing requires a push that the user has not authorized, ask before pushing. Never force-push or skip hooks to bypass an issue.
7. Create the pull request with `gh pr create`, using the discovered base branch and a body file for multi-line content. Follow repository-specific requirements for labels, reviewers, templates, and attribution if present.
8. Report the PR title and URL. If creation is blocked, explain the specific blocker instead of presenting a draft as a created PR.

## Safety and Accuracy

- Follow repository instructions and its CI, test, title, and release conventions over generic examples in this skill.
- Do not commit, amend, or push unrelated changes. Do not commit fixes unless requested or clearly authorized as part of the task.
- Do not claim checks passed unless they were run and succeeded.
- Do not invent features, test plans, or implementation details that are not supported by the diff.
- If the target branch, intended change scope, or inclusion of local edits is unclear, ask before proceeding.
