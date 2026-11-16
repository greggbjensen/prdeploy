# prdeploy-app

Web application for Engineers, QA, and Product Owners to be able to review and deploy services.

# Setup

1. Register a GitHub OAuth Application.

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
