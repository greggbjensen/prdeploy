---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/Directory.Build.*"
  - "**/Directory.Packages.props"
---

# C# and .NET

Follow the target framework, SDK, language version, package-management setup, and architectural patterns already used by the project. Do not upgrade or change them unless requested.

## Code

- Enable nullable reference types where the project supports them and respect existing nullability annotations.
- Follow the repository's naming and formatting conventions. Prefer clear, idiomatic C# and the least public visibility that meets the design.
- Use asynchronous APIs end to end when available, propagate cancellation tokens, and avoid sync-over-async and unobserved background work.
- Throw meaningful exceptions, handle failures explicitly, and use structured logging rather than writing application output to the console.
- Keep secrets and sensitive data out of source, logs, traces, and exception messages.
- Use the project's established HTTP client and dependency-injection patterns for outbound requests.

## Tests

- Use the test framework and conventions already present in the solution.
- Test externally visible behavior at the appropriate boundary. Keep unit tests focused, and use integration tests when behavior depends on a real database, host, or other infrastructure.
- Isolate tests from external services unless a live integration test is explicitly intended.
- Keep tests deterministic, independent, and safe to run in parallel.
- Prefer simple fakes or the repository's established test utilities; avoid mocking implementation details.

## Dependencies and Changes

- Use the repository's package-management approach and check its dependency and licensing policies before adding packages.
- Add focused tests and update directly related documentation when behavior changes.
- Do not apply database migrations or deploy without explicit approval.

## Validation

Run the narrowest applicable formatter, build, and test commands for the changes. Use solution-wide validation when targeted checks are insufficient.

Adapted from github/awesome-copilot's `csharp.instructions.md` at 7e40881 (MIT, [licence](../licenses/awesome-copilot-MIT.txt)).
