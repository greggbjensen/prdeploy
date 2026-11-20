# prdeploy-app

Web application for Engineers, QA, and Product Owners to be able to review and deploy services.

# Setup

1. Register a GitHub OAuth Application.

# Settings: adding services

The **Add Service** dialog (Settings > Services) starts with a **Workflow** dropdown listing the YAML files in the
repository's `.github/workflows` folder, loaded through the `repositoryWorkflows` GraphQL query. Selecting one fills in
**Service Name** and **Path** with the file name minus its extension and any trailing `-ci` or `-build` suffix, for
example `.github/workflows/api-build.yaml` and `api-ci.yml` both become `api`. Name and Path are both required, stay
editable, and the dropdown can be ignored to type them directly. There is no separate builds workflow or check pattern setting;
the mapping from a build workflow to its service is the service's `workflow` on this tab.

The selected workflow's file name (for example `api-build.yaml`) is saved on the service as `workflow` and shown in
the first column of the Services table, before Name. Services typed by hand, and those saved before this column
existed, have no workflow and show a blank cell.

# Settings: Microsoft Teams

The **Microsoft Teams** tab (after Slack) holds a **Notifications enabled** checkbox and a **Webhooks** section with
**Deploy URL** and **Release URL**. They are saved under `teams` in the deploy settings at owner or repo level, like the
Slack webhooks. The values are only stored for now; no Teams notifications are sent yet.

# Testing

Specs live next to their components as `*.spec.ts` and run with `npm test`. The Karma and Jasmine packages are not
currently declared in `package.json`, so install them before running the specs.

# Styles

Global styles use the Sass module system (`@use`/`@forward`); `@import` and global built-in functions
such as `mix()` or `map-get()` are deprecated and produce build warnings, so do not reintroduce them.

- `src/sass/` is a trimmed copy of Bootstrap 5.3's Sass (variables, functions, maps, reboot, root and RFS).
- `src/sass/_config.scss` holds the settings that `_functions.scss` depends on (`$white`, `$black`, `$prefix`,
  contrast settings). `_variables.scss` forwards it, so `@use` the variables module to get every variable.
- `src/themes/m3-theme.scss` builds the Angular Material theme from those variables and functions.

Component stylesheets that need shared variables should start with:

```scss
@use '../../sass/variables' as *; // adjust the relative path to src/sass
```
