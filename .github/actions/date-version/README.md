# date-version action

Creates a version for a build using the current date and run ID of the workflow.

## Inputs

### `use_four_digits`

If set to true, the Major.Minor.Build.Revision version is used with four digits.

### `use_prerelease`

If set to false, the last version number becomes the run ID instead of the day.

## Outputs

### `version`

The recommended version to use for the build.

### `env.VERSION`

The recommended version to use for the build.

## Example usage with 3-digit pre-release tag

```yaml
- name: Version
  uses: greggbjensen/prdeploy/.github/actions/date-version@main
```

### Output

```
2022.6.30-r5487634831
```

## Example usage with 3-digit full versions only

```yaml
- name: Version
  uses: greggbjensen/prdeploy/.github/actions/date-version@main
  with:
    use_prerelease: false
```

### Output

```
2022.630.83032
```

## Example usage with 4-digit full versions only

```yaml
- name: Version
  uses: greggbjensen/prdeploy/.github/actions/date-version@main
  with:
    use_four_digits: true
```

### Output

```
2022.6.30.83032
```

## Testing

The logic is in `date-version.sh`. Set `DATE_VERSION_EPOCH` to a Unix time to generate the version for a fixed time.
Run its tests with:

```bash
.github/actions/date-version/__tests__/date-version.test.sh
```

The `actions - test` workflow runs them, and `shellcheck`, on pull requests that change the actions.

The four-digit and full-version formats join date parts without padding, so versions are not always in build order and
can repeat. For example, the full version is `2026.1120.1590` at 1:59:00 but `2026.1120.1000` at 10:00:00, and January 12
and November 2 both produce `112`. Use the pre-release format when versions must sort by build time.
