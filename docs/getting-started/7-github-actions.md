
The deployment process keeps all environments in sync with production except what is being deployed with the pull request.  As such you have both `sync_run_ids` and `deploy_run_ids` as input to a `combined-deploy.yaml` workflow.

- `sync_run_ids` - GitHub Actions run IDs of the services to sync with production to update your environment.
- `deploy_run_ids` - GitHub Actions run IDs of the services to deploy from your pull request.

## Combined Deploy Workflow

In order for a repository to handle deployments, it must contain and workflow of `combined-deploy.yaml` with the following inputs:

```yaml
on:
  workflow_dispatch:
    inputs:
      pull_number:
        description: 'Pull request number'
        type: string
        required: false
      environment:
        type: choice
        description: 'Deploy environment'
        options:
          - dev
          - stage
          - prod
      deploy_run_ids:
        description: 'IDs of builds to deploy'
        type: string
        required: true
      sync_run_ids:
        description: 'IDs of builds to sync'
        type: string
        required: false
      deploy_name:
        description: 'Deploy run name (automated)'
        type: string
        required: true
```

A job can be used like the following to get the build information from the run IDs:

```yaml
jobs:
  deploy_info:
    name: Deploy Info
    runs-on: ubuntu-latest
    concurrency:
      group: deploy-info-${{ github.workflow }}-${{ github.ref }}
      cancel-in-progress: true
    outputs:
      branch: ${{ steps.build_info.outputs.branch }}
      builds_matrix_json: ${{ steps.build_info.outputs.builds_matrix_json }}
      builds_json: ${{ steps.build_info.outputs.builds_json }}
    steps:
      - name: Get build info
        id: build_info
        uses: greggbjensen/prdeploy/.github/actions/get-build-info@v1
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          pull_number: ${{ inputs.pull_number }}
          deploy_run_ids: ${{ inputs.deploy_run_ids }}
          sync_run_ids: ${{ inputs.sync_run_ids }}
```

## GitHub Actions

Actions that apply and retrieve the information needed for builds and deployments.

| Action                                                                            | Description                                                                      |
|-----------------------------------------------------------------------------------|----------------------------------------------------------------------------------|
| [build-details](/.github/actions/build-details/README.md#build-details-action)    | Creates a summary of your build with version and pull request info.              |
| [date-version](/.github/actions/date-version/README.md#date-version-action)       | Creates a version for a build using the current date and run ID of the workflow. |
| [get-build-info](/.github/actions/get-build-info/README.md#get-build-info-action) | Gets build data and a matrix for a set of deploy and sync run IDs.               |


## Notes

1. Labels will be automatically created for each environment in your repository.
   1. Environment labels should be the badge color you want for the environment icon.
3. In order for a completed or failed deploy status message to show the version, you must provide a `build-details` artifact to your builds that includes a `build-details.json` file with at least the following:
   1. The [build-details](/.github/actions/build-details/README.md#build-details-action) action can provide this.

```json
{
  "version": "2023.11.30-r7040754105"
}
```

3. Go to **Settings** and **General** for your repository and check `Always suggest updating pull request branches` to get the pull request **Update** button.
   1. This makes it much easier to update your pull requests to latest before deploying.