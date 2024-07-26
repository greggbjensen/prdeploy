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