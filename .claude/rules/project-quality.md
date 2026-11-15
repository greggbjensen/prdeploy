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

## Bruno API Collection

- Keep the repository's Bruno OpenCollection YAML requests under the root `bruno/` directory. Add or update a request for every first-party REST endpoint and GraphQL operation used by the project.
- Document each request's purpose, required inputs, authentication, and meaningful expected responses. Link REST requests to their matching operation in the generated Swagger/OpenAPI document; link GraphQL requests to their client operation and server schema/resolver because GraphQL operations are not OpenAPI REST paths.
- Maintain the requests and documentation whenever an endpoint, GraphQL operation, or API contract changes. Use environment variables for URLs and credentials; never commit real tokens, secrets, or personal data.
- Add relevant response assertions where they are stable and safe to run. Clearly label mutating or externally side-effecting requests, and keep defaults pointed at a local development API.
