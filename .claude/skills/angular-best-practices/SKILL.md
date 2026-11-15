---
name: angular-best-practices
description: Use when building, modifying, reviewing, or debugging Angular applications. Covers Angular components, templates, signals, RxJS, dependency injection, routing, forms, performance, security, and testing.
---

# Angular Best Practices

Apply Angular guidance that fits the project's installed Angular version and existing architecture. Prefer current official Angular documentation for version-sensitive APIs and behavior.

## Workflow

1. Inspect `package.json`, the lockfile, `angular.json`, TypeScript configuration, and nearby application code and tests. Identify the Angular version, build and test tooling, and established patterns.
2. Read the closest existing implementation before changing it. Preserve its conventions unless the task calls for a deliberate migration.
3. Check current Angular documentation when using unfamiliar APIs or version-sensitive features. Do not copy examples written for a different Angular version without verifying compatibility.
4. Make the smallest coherent change, add or update focused tests, and run the project's applicable lint, test, and build commands.

## Application Structure

- Organize code around features and cohesive concepts. Keep related components, templates, styles, and tests together according to the project's structure.
- Keep components focused on UI behavior. Move reusable domain logic or transformations out of templates and into appropriately scoped functions or services.
- Keep templates straightforward and declarative. Refactor complex expressions or repeated logic instead of building logic-heavy templates.
- Prefer one primary concept per file, while respecting the project's existing component and file naming conventions.

## Components and Templates

- Follow the component model already used by the application. For new components, prefer standalone components when supported and consistent with the project; do not convert existing NgModule-based areas as an unrelated change.
- Use typed component inputs, outputs, and state. Adopt newer signal-based APIs only when supported by the installed version and compatible with neighboring code.
- Use `protected` for component members exposed only to that component's template, and `readonly` for values that should not be reassigned.
- Prefer built-in template control flow (`@if`, `@for`, `@switch`) when available in the installed Angular version. Give repeated items stable identity with `track`.
- Keep lifecycle hooks small and delegate substantive behavior to clearly named methods.
- Prefer direct `class` and `style` bindings for simple conditional styling when supported by the project's conventions.
- Use semantic HTML, accessible names, keyboard interaction, and visible focus states. Do not rely on color alone to convey meaning.

## State, Reactivity, and Dependency Injection

- Use signals for local synchronous UI state and `computed` for derived state when that fits the application. Avoid duplicating derived values as independently mutable state.
- Use RxJS for asynchronous streams and stream composition when it fits existing APIs. Avoid unmanaged subscriptions; use framework lifecycle integration or established cleanup patterns.
- Choose the simplest state boundary that meets the need. Do not add a global store or abstraction for local component state.
- Follow Angular's dependency-injection patterns and the codebase's style. Prefer `inject()` in new code when appropriate, but do not mix styles unnecessarily.
- Scope providers deliberately so services live as long as their consumers require.

## Routing, Forms, and HTTP

- Lazy-load substantial route areas when appropriate to reduce initial loading cost, following the router patterns already in use.
- Use route guards for navigation and user experience, never as the sole authorization boundary; enforce access rules on trusted server-side systems.
- Choose the form approach that matches the complexity and existing conventions. Reactive forms are suitable for dynamic or complex validation; simpler forms may use the established alternative.
- Make validation errors understandable and accessible, and preserve entered values when reporting recoverable errors.
- Use the project's configured `HttpClient` patterns and typed request/response models. Handle loading, empty, success, and error states explicitly.
- Treat network data as untrusted at runtime. TypeScript annotations alone do not validate API responses.

## Performance and Security

- Measure or profile before optimizing runtime behavior. Use route lazy loading, `@defer`, image optimization, or change-detection tuning when they address a demonstrated need and are supported by the project.
- Avoid expensive template computations and unnecessary work during change detection.
- Use Angular template bindings and sanitization by default. Avoid direct DOM manipulation and never construct Angular templates from untrusted input.
- Do not use `DomSanitizer` bypass methods for untrusted or insufficiently validated values.
- Keep authorization, secrets, and other security-sensitive enforcement out of client-only code.

## Testing and Validation

- Use the test runner already configured by the project; do not assume a particular runner from the Angular version alone.
- Test observable behavior for components, services, forms, routing, and error states. Keep tests focused and deterministic.
- Run the repository's existing lint, test, and build scripts for the affected application.
- Do not change framework versions, dependencies, or project-wide build configuration unless required by the task.

## Official References

- [Angular coding style guide](https://angular.dev/style-guide)
- [Angular components](https://angular.dev/guide/components)
- [Angular signals](https://angular.dev/guide/signals)
- [Angular performance best practices](https://angular.dev/best-practices/performance)
- [Angular security](https://angular.dev/best-practices/security)
- [Angular testing](https://angular.dev/guide/testing)
