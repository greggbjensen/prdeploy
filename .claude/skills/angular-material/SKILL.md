---
name: angular-material
description: Use when building, modifying, reviewing, or debugging Angular Material or Angular CDK interfaces. Covers available Material components, component selection, accessibility, theming, forms, overlays, tables, and component testing.
---

# Angular Material and CDK

Use the Angular Material component library and CDK in ways that fit the application's installed Angular and Material versions. Read [angular-best-practices](../angular-best-practices/SKILL.md) for framework-wide guidance.

## Workflow

1. Inspect `package.json`, the lockfile, `angular.json`, existing imports, theme setup, and neighboring UI code. Verify the installed versions of `@angular/core`, `@angular/material`, and `@angular/cdk`.
2. Browse the [official Material component catalog](https://material.angular.dev/components/categories) and the component's documentation for the installed version. The catalog below is a quick index, not a guarantee that every item exists in every release.
3. Check the [CDK documentation](https://material.angular.dev/cdk/categories) when a low-level behavior is needed without Material styling.
4. Choose the simplest component that fits the user interaction and existing design. Implement it using its documented public API, then test its behavior and accessibility.

## Component Catalog

The current Angular Material source includes these component packages. Use the official catalog and installed package exports as the authority for availability, API details, and deprecations.

### Input and form controls

- **Autocomplete** — suggestions associated with a text input.
- **Checkbox** — independent or grouped boolean choices.
- **Chips** — compact values or selectable items; use the supported patterns for the installed version.
- **Datepicker** — date entry and calendar selection.
- **Form field** — layout and labels for supported form controls.
- **Input** — text-input directive, commonly paired with a form field.
- **Radio button** — one choice from a mutually exclusive group.
- **Select** — choosing one or more options from a list.
- **Slide toggle** — a binary setting or preference.
- **Slider** — selecting a numeric value or range.
- **Timepicker** — time entry and selection where provided by the installed version.

### Actions and navigation

- **Button** and **button toggle** — actions and selectable button groups.
- **Icon** — display icons through the application's configured icon sources.
- **List** — rows of related navigation or content.
- **Menu** — contextual or command navigation.
- **Paginator** — navigation across pages of data.
- **Sidenav** — persistent or dismissible application navigation.
- **Stepper** — sequential, multi-step workflows.
- **Tabs** — switching between related views.
- **Toolbar** — an application or section header/action bar.
- **Tree** — hierarchical data or navigation.

### Layout and content

- **Card** — grouped content and actions.
- **Divider** — a visual or semantic separator.
- **Expansion panel** — content that can be expanded or collapsed.
- **Grid list** — tiles arranged in a grid.

### Feedback and overlays

- **Badge** — a small status or count marker attached to an element.
- **Bottom sheet** — supplementary content or actions presented from an edge.
- **Dialog** — focused decisions, forms, or information that interrupts the current flow.
- **Progress bar** and **progress spinner** — indeterminate or determinate activity status.
- **Ripple** — interaction feedback where an explicit ripple is appropriate.
- **Snackbar** — brief, non-blocking status messages and optional actions.
- **Tooltip** — short supplemental descriptions for labeled or otherwise clear controls.

### Data presentation

- **Sort** — sorting behavior used with data views.
- **Table** — structured tabular data, commonly combined with sort and paginator.

### CDK primitives

The CDK provides unstyled behavior and infrastructure rather than a Material visual design. Common packages include accessibility utilities, bidirectional layout, clipboard, drag and drop, layout, listbox, menu, observers, overlay, portal, scrolling, stepper, table, text field, and tree. Confirm APIs in the installed CDK version before use.

## Usage Practices

- Import only the components, directives, and services a feature uses. Follow the application's standalone or NgModule setup.
- Prefer Angular Material's documented public APIs over custom imitations. Do not depend on private component internals, generated DOM structure, or undocumented CSS classes.
- Use semantic HTML and the component's expected roles and structure. Provide accessible names for icon-only buttons, meaningful labels, keyboard support, visible focus, and clear validation feedback.
- Use menus for commands or navigation, not as a substitute for a form control. Use dialogs for focused tasks that need attention; prefer inline content when interruption is unnecessary.
- For form fields, associate a visible label with the control, connect errors and hints, and preserve the user's input when validation fails.
- For tables, provide meaningful column headers, stable row identity, sorting and pagination behavior appropriate to the data size, and an accessible empty/loading/error state.
- For dialogs, menus, and overlays, follow documented focus, dismissal, and focus-restoration behavior. Do not add a competing focus trap.
- Apply theme and typography through the theme system supported by the installed version. Reuse existing theme tokens and avoid broad selectors that reach into component internals.
- Avoid adding Material components or the CDK when a semantic native element is sufficient.
- Use Angular Material's component harnesses for tests where available. Prefer assertions against public behavior over brittle selectors or internal DOM structure.
- Do not add or upgrade Angular Material/CDK packages as an incidental change; preserve compatible Angular package versions and the repository's package-management conventions.

## Official References

- [Angular Material components](https://material.angular.dev/components/categories)
- [Angular CDK](https://material.angular.dev/cdk/categories)
- [Angular Material getting started](https://material.angular.dev/guide/getting-started)
- [Angular Material theming](https://material.angular.dev/guide/theming)
- [Angular Material component harnesses](https://material.angular.dev/guide/using-component-harnesses)
- [Angular best practices](../angular-best-practices/SKILL.md)
