# prdeploy-app

Web application for Engineers, QA, and Product Owners to be able to review and deploy services.

# Setup

1. Register a GitHub OAuth Application.

# Deployments: environment URLs

The **URLs** column on the Deployments dashboard lists each environment's labelled links (`urls`: `label` and `url`), which are
maintained in the **URLs** table of the Settings | Environments tab (`AddEnvironmentUrlDialogComponent` collects a label and an
absolute `http(s)` URL). A legacy single `url` is moved into that table when the environment is edited. Both the form and
the save mapping have Karma specs.

The **User** column shows the pull request author's GitHub avatar (`user.avatarUrl`) and name, linked to their GitHub
profile (`user.url`); the link is omitted when GitHub returns no profile URL.

# Deployments: live deploy status

The **Status** column on the Deployments dashboard shows each environment's deploy workflow state (spinner, check,
error icon). `DeployStatusService` reads the API's `GET /api/deploy-status/{owner}/{repo}/events` Server-Sent Events
stream with `fetch` (so the bearer token can be sent), reconnects after 3 seconds if the stream drops, and the
dashboard matches statuses to environments by name, ignoring case. `SseParser` and the icon component have Karma specs;
run them with `npm test -- --watch=false --browsers=ChromeHeadless`.

# Deployments: pull request services

The **Services** icon on each environment row that has a pull request, and on each queued pull request, opens the
`PrServicesDialogComponent`. It loads the `pullRequestServices` GraphQL query: every configured service with a
`triggered` flag, true when a workflow run for the pull request's head commit used the service's configured workflow
file. Triggered services are checked and locked. Checking extra services enables **Add**, which calls the
`pullRequestAddServices` mutation; the API posts an `/add` comment and the webhook writes a `.deploy` file line
(datestamp and branch) into each service's configured path. **Cancel** closes the dialog. The Karma spec is
`pr-services-dialog.component.spec.ts`.

# Settings: deep routing

The Settings page is routed as `/:owner/:repo/settings/:level/:section`, where `level` is `owner` or `repo` and the
optional `section` is a secondary left nav id: `environments`, `services`, `deployment`, `slack`, `teams`, or `jira`
(for example `/acme/shop/settings/repo/slack`). Picking a section updates the URL, switching level keeps the section,
and a missing or unknown section shows **Environments**.

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
currently declared in `package.json`, so install them before running the specs. The current `jasmine-core` major is not
compatible with zone.js, so install these versions without changing `package.json`:

```bash
npm install --no-save karma@6 karma-jasmine@5 karma-chrome-launcher@3 jasmine-core@5.1 @types/jasmine@5 \
  karma-jasmine-html-reporter@2 karma-coverage@2
npm test -- --watch=false --browsers=ChromeHeadless
```

# Forms

Mark every input the API or deployment flow depends on as required, so the field shows an asterisk and an error
state. Reactive controls with `Validators.required` are marked automatically; template-driven inputs need `required`.
Settings that a repository inherits from its owner are only required at the owner level, for example the deploy
workflow, default and release environments, Jira host and credentials when Jira is enabled, and Slack webhook URLs
when Slack notifications are enabled. Dialogs disable their submit button until the form is valid. Secrets come back
from the API as `********`; saving that value keeps the stored secret.

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
