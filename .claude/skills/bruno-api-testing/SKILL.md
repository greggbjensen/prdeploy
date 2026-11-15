---
name: bruno-api-testing
description: Guidance for maintaining Bruno HTTP collections, request specifications, assertions, and environment variables.
---

# Bruno API Testing

Use Bruno collections to keep repeatable API requests and expected behavior alongside the project when Bruno is part of its workflow.

## Collection Structure

Follow the repository's existing collection and environment layout. A typical collection may include:

```text
bruno/
├── environments/
│   ├── local.bru
│   └── staging.bru
└── api/
    ├── bruno.json
    ├── resources/
    └── health/
```

## Request Conventions

- Use environment variables for base URLs and other values that differ by environment. Do not commit credentials or secrets.
- Give each request a clear name and keep request bodies representative, minimal, and free of real personal or sensitive data.
- Add assertions for relevant status codes and important response properties.
- Keep authentication and headers consistent with the API's documented contract.
- Organize requests by resource or behavior, matching the collection's existing conventions.

## Maintenance

- When a collection is used as API documentation or a regression suite, update the relevant requests when the API contract changes.
- Keep request and response examples synchronized with the API's current schemas.
- Run the repository's configured Bruno CLI command or collection test workflow when available.
