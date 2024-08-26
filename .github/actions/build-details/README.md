# build-details action

Creates a summary of your build with version and pull request information.

![Screenshot](/docs/assets/images/screenshots/getting-started/github-actions-build-details.png)

## Inputs

### `version`

**Required** The symantic version of the build.

### `token`

**Required** The GitHub token to use for authentication.

## Example

```yaml
- name: Build details
  uses: greggbjensen/prdeploy/.github/actions/build-details@main
  with:
    version: '1.0.0-beta0005'
    token: ${{ secrets.GITHUB_TOKEN }}
```
