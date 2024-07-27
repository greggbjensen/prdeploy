# get-build-info action

Gets build data and a matrix for a set of deploy and sync run IDs.

## Inputs

### `token`

**Required** The GitHub token to use for authentication.

### `pull_number`

**Required** Number of the pull request to display.

### `deploy_run_ids`

**Required** Run IDs of builds being deployed.

### `sync_run_ids`

Run IDs of builds being synced with stable release.

### `workflow_regex`

Regular expression with a single capture group that is the build name, defaults to `/([^/]+?)-build.ya?ml`.

## Outputs

### `builds_json`

JSON text of an array of build objects that are name and version.

### `builds_matrix_json`

Array of build names only that can be bound to a workflow matrix strategy for parallel runs.

### `builds_count`

Total number of builds that were found.

### `pull_branch`

The branch the pull request is from.

## Example usage

```yaml
- name: Get build info
  uses: greggbjensen/prdeploy/.github/actions/get-build-info@main
  with:
    token: ${{ secrets.GITHUB_TOKEN }}
    pull_number: ${{ inputs.pull_number }}
    deploy_run_ids: '5150198235,4683559959'
    sync_run_ids: '7921342161'
```
