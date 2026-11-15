---
applyTo: "**"
trigger: always_on
---

# Tests and Documentation

Apply these requirements to changes throughout the repository. Inspect the affected project's conventions before choosing test and documentation locations.

## Automated Tests

- Every code change must create or update automated tests that exercise the changed behavior, including relevant success, failure, and boundary cases.
- Keep tests focused, deterministic, and consistent with the affected project's existing test framework and patterns.
- Run the relevant automated tests and report any that could not be run. Do not treat manual verification as a substitute for automated tests.

## Readmes and Documentation

- Create and maintain a scoped `README.md` for each distinct application, service, package, tool, or other independently usable project area. Include relevant purpose, setup, development, testing, and operational guidance; avoid adding redundant READMEs to trivial folders.
- Update documentation in the same change whenever behavior, configuration, interfaces, setup, testing, or operations change. Keep instructions accurate and remove obsolete guidance.
- Link every repository `README.md` other than the root README from the root `README.md`. Add or update the root link whenever a README is added, moved, or renamed.
