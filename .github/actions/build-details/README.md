# build-details action

Creates a summary of your build with version information attached.

![Screenshot](/.github/actions/build-details/images/build-details.png)

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
