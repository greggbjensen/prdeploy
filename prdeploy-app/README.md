# prdeploy-app

Web application for Engineers, QA, and Product Owners to be able to review and deploy services.

# Setup

1. Register a GitHub OAuth Application.

# Settings: adding services

The **Add Service** dialog (Settings > Services) starts with a **Workflow** dropdown listing the repository's workflow
files in `.github/workflows` that match the builds workflow pattern, loaded through the `repositoryWorkflows` GraphQL query. Selecting one fills in the **Service Name** by applying
the builds workflow pattern (Settings > Deployment, repo value falling back to the owner value) to the workflow path
and using the first capture group, for example `/([^/]+?)-build.ya?ml` turns `.github/workflows/api-build.yaml` into
`api`. When there is no pattern or it does not match, the file name without its extension and without a trailing
`-ci` or `-build` suffix is used. The **Path** field is filled with the same value. Name and Path are both required,
stay editable, and the dropdown can be ignored to type them directly.

The selected workflow's file name (for example `api-build.yaml`) is saved on the service as `workflow` and shown in
the first column of the Services table, before Name. Services typed by hand, and those saved before this column
existed, have no workflow and show a blank cell.

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
