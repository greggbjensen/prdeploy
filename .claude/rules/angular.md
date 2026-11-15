---
paths:
  - "prdeploy-app/src/**/*.ts"
  - "prdeploy-app/src/**/*.html"
  - "prdeploy-app/src/**/*.scss"
  - "prdeploy-app/angular.json"
  - "prdeploy-app/package.json"
  - "prdeploy-app/tsconfig*.json"
applyTo: "prdeploy-app/src/**/*.ts, prdeploy-app/src/**/*.html, prdeploy-app/src/**/*.scss, prdeploy-app/angular.json, prdeploy-app/package.json, prdeploy-app/tsconfig*.json"
trigger: glob
globs: "prdeploy-app/src/**/*.ts, prdeploy-app/src/**/*.html, prdeploy-app/src/**/*.scss, prdeploy-app/angular.json, prdeploy-app/package.json, prdeploy-app/tsconfig*.json"
---

# Angular Best Practices

Follow the Angular version, dependencies, and conventions already used by `prdeploy-app`. It currently uses Angular 22, standalone-style components, lazy-loaded route files, Angular Material, strict template checking, and Karma. Verify version-sensitive APIs against the installed version; do not upgrade dependencies or change project-wide configuration unless requested.

## Components and Templates

- Keep components focused on UI behavior and templates declarative. Move reusable domain logic and nontrivial transformations into appropriately scoped services or functions.
- Follow nearby component patterns for standalone imports, dependency injection, change detection, and styling. Do not mix in a new style or migrate neighboring code as unrelated cleanup.
- Use typed inputs, outputs, and state. Prefer signals for local synchronous state and `computed` for derived state when consistent with the surrounding code; use RxJS for asynchronous streams and composition.
- Clean up subscriptions with the established lifecycle-aware patterns, such as `takeUntilDestroyed`. Keep lifecycle hooks small.
- Use semantic HTML, accessible names, keyboard support, and visible focus states. Avoid complex template expressions and expensive work during change detection.

## Application Behavior

- Preserve the existing router structure and lazy-load substantial feature areas where appropriate. Guards improve navigation experience but must not be treated as the authorization boundary.
- Use the established reactive or template-driven form approach for the feature. Present validation errors accessibly and preserve user input on recoverable errors.
- Follow configured `HttpClient`, GraphQL, and error-handling patterns. Model loading, empty, success, and error states explicitly; treat API data as untrusted at runtime.
- Prefer Angular bindings and built-in sanitization. Avoid direct DOM manipulation and never bypass sanitization for untrusted values.
- Keep authorization and security-sensitive enforcement on trusted server-side systems, not in client-only code.

## Testing and Validation

- Add focused tests for observable behavior using the configured Karma test setup; keep tests deterministic and isolated from external services.
- Run the applicable `npm` lint, test, and build scripts from `prdeploy-app`. Do not change framework versions, dependencies, or build configuration unless required by the task.
