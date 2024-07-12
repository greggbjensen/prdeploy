# prdeploy
GitHub App that allows the entire build-deploy lifecycle happens within a feature branch.  If any problems are found in the deployment, a new commit is done and rolled through without needing multiple PRs.

## Repository Setup

1. Follow the [prdeploy-api App Setup](/prdeploy-api/README.md#app-setup) to get prdeploy configured for your organization.
2. Add a `pr-deploy.yaml` settings file to the root of your repository:

   ```yaml
   deployWorkflow: combined-deploy.yml          # Worklow to trigger for deployments to an environment.
   environments:                                # List of environments to be used.
     - name: dev                                # Name of the environment in GitHub Environments.
       queue: 'DEV_PR_QUEUE'                    # GitHub repository variable for queue, if there are multiple of the same evironment use the same queue.
       url: 'https://dev.mydomain.com'          # URL of the environment the deployment is for.
     - name: stage
       queue: 'STAGE_PR_QUEUE'
       url: 'https://stage.mydomain.com'
       requireBranchUpToDate: true              # Indicates if the feature branch must be up to date with main to deploy.
     - name: prod
       queue: 'PROD_PR_QUEUE'
       url: 'https://www.mydomain.com'
       requireBranchUpToDate: true
       requireApproval: true                    # Indicates if approval is needed for deployment to continue.
       excludeFromRollback: []                  # Services that should not be rolled back in this environment, such as databases.
       automationTest:                          # Automation tests to run and report on as part of the pull request deploy.
         enabled: false                         # Whether or not automation test run should be triggered.
         workflow: test-automation-run.yml
         inputs:
           environment: ${environment}          # Inputs can be tokens bound from pull request deploy, or hard coded values.
           pull_number: ${pull_number}
           run_name: ${run_name}
    services:
      - name: mydomain-api-services                   # Name and path of services that can be deployed for /add command.
        path: mydomain-services/mydomain-api-services # Path is only required if it is not a folder on the root.
      - name: mydomain-api-products
      - name: mydomain-app-main
    defaultEnvironment: dev                      # Default for /deploy only.
    releaseEnvironment: prod                     # Environment that is used for final releases.
    addJiraIssues: true                          # If JIRA issues should be read from branch and added to PR.
    settingsBranch: 'main'                       # Branch to retrieve repo settings from.
    builds:
      checkPattern: 'Build helm chart'           # Pattern to know which checks are builds.
      workflowPattern: '/([^/]+?)-build.ya?ml'   # Pattern to know how to extract the build name from the workflow file.
    slack:
      notificationsEnabled: true                 # Whether or not to send Slack notifications.
        channels:
        deploy: ''                               # Slack webhook for channel to post to for each deployment success and failure.
        release: ''                              # Slack webhook for channel to post final releases with notes to.
    ```

   _NOTE: For effeciency settings cache only updates every 5 minutes._

3. Labels will be automatically created for each environment in your repository.
   1. Environment labels should be the badge color you want for the environment icon.
4. In order for a completed or failed deploy status message to show the version, you must provide a `build-details` artifact to your builds that includes a `build-details.json` file with at least the following:

   ```json
   {
     "version": "2023.11.30-r7040754105"
   }
   ```

   1. The [build-details](/.github/actions/build-details/README.md#build-details-action) action can provide this.

5. Go to **Settings** and **General** for your repository and check `Always suggest updating pull request branches` to get the pull request **Update** button.
   1. This makes it much easier to update your pull requests to latest before deploying.

# Deployments

In order for a repository to handle deployments, it must contain and workflow of `combined-deploy.yml` with the following inputs:

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

# Comment Commands

| Command                                              | Description                                                                                                                         |
| ---------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `/deploy`<br>`/deploy dev`                           | Deploy to first available dev environment or get added to queue.                                                                    |
| `/deploy stage`                                      | Deploy to stage environment or get added to queue.                                                                                  |
| `/deploy prod`                                       | Deploy to prod environment or get added to queue.                                                                                   |
| `/deploy dev2 --force`<br>`/deploy stage --force`    | Override deploy to a specific dev, stage, or prod environment and skip the queue.                                                   |
| `/deploy stage --retain`<br>`/deploy prod --retain`  | Promote pull request to next environment, but retain the lock on the current one as well. Can also be used with --force.            |
| `/free`                                              | Remove lock on all deployed environments for others to use it.                                                                      |
| `/free stage`                                        | Remove lock on specified environment only for others to use it.                                                                     |
| `/rollback`                                          | Rollback current locked environment to the previous version. If there are multiple environments locked, you must specify which one. |
| `/rollback prod`<br>`/rollback stage`                | Rollback a specific environment to the previous version. You must have a lock on an environment to roll it back.                    |
| `/rollback prod 2`                                   | Rollback a specific environment multiple versions in history.                                                                       |
| `/add mydomain-api-service mydomain-app-main`        | Add services to be built and deployed with the pull request that have no code changes.                                              |

## Deployment Practices

1. Each pull request should be deployed as ready in the CD workflow.
2. Only once deployed and verified the pull request should be merged to main.
3. This application is deployed to a kubernetes cluster using the helm chart.

# Contributing

1. For API development and debugging, see [prdeploy-api Development](/prdeploy-api/README.md#development).
2. For Web development and debugging, see [prdeploy-web Development](/prdeploy-web/README.md#development).
