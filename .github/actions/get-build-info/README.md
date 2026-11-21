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

## Outputs

### `builds_json`

JSON text of an array of build objects that are name and version.

### `builds_matrix_json`

Array of build names only that can be bound to a workflow matrix strategy for parallel runs.

### `builds_count`

Total number of builds that were found.

### `pull_branch`

The branch the pull request is from.

## Permissions

The token reads each build's workflow run and `build-details` artifact, and the pull request, so the job needs:

```yaml
permissions:
  actions: read
  contents: read
  pull-requests: read
```

Set these on the job that runs the action. A workflow-level `permissions` block, such as `contents: read` alone, applies to every job without its own block, and the action then fails with `Resource not accessible by integration`; the failure message names the missing permissions.

## Example usage

```yaml
deploy_info:
  runs-on: ubuntu-latest
  permissions:
    actions: read
    contents: read
    pull-requests: read
  steps:
    - name: Get build info
      uses: greggbjensen/prdeploy/.github/actions/get-build-info@main
      with:
        token: ${{ secrets.GITHUB_TOKEN }}
        pull_number: ${{ inputs.pull_number }}
        deploy_run_ids: '5150198235,4683559959'
        sync_run_ids: '7921342161'
```

## Development

```bash
npm install
npm test          # Jest unit tests in __tests__/, with the GitHub API and runner mocked
npm run lint
npm run build     # compile to lib/, which the bundle step reads
npm run package   # ncc bundle of lib/main.js to dist/, which action.yaml runs; commit dist/
```

`src/main.ts` only calls `run()` from `src/run.ts`, so tests can import and run the action without it starting on
import. Blank or non-numeric entries in `deploy_run_ids` and `sync_run_ids` are ignored. The `actions - test`
workflow type checks, lints, tests, and bundles the action on pull requests.
