# prdeploy
GitHub App that allows the entire build-deploy lifecycle happens within a feature branch.  If any problems are found in the deployment, a new commit is done and rolled through without needing multiple PRs.

## App Setup

1. Install the **PR Deploy** app to your repository.
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
      path: mydomain-services/mydomain-api-services     # Path is only required if it is not a folder on the root.
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

3. Create an organization or repository variable `EMAIL_ALIASES`, for any email account that does not follow the `first.last@mydomain.com` pattern with a JSON value like below:

   ```json
   {
     "jim.smith@mydomain.com": "james.smith@mydomain.com",
     "john.doe@mydomain.com": "jdoe@mydomain.com",
     "will.harris@mydomain.com": "billyh@mydomain.com"
   }
   ```

4. Labels will be automatically created for each environment in your repository.
   1. Environment labels should be the badge color you want for the environment icon.
5. In order for a completed or failed deploy status message to show the version, you must provide a `build-details` artifact to your builds that includes a `build-details.json` file with at least the following:

   ```json
   {
     "version": "2023.11.30-r7040754105"
   }
   ```

   1. The [build-details](/.github/actions/build-details/README.md#build-details-action) action can provide this.

6. Go to **Settings** and **General** for your repository and check `Always suggest updating pull request branches` to get the pull request **Update** button.
   1. This makes it much easier to update your pull requests to latest before deploying.

## Requirements

- Grant app the following permissions:

  **Repository Permissions**

  | Scope          | Permission     |
  | -------------- | -------------- |
  | Actions        | Read and write |
  | Administration | Read-only      |
  | Checks         | Read-only      |
  | Contents       | Read and write |
  | Issues         | Read-only      |
  | Metadata       | Read-only      |
  | Pull requests  | Read and write |
  | Variables      | Read and write |

  **Organization Permissions**

  | Scope     | Permission |
  | --------- | ---------- |
  | Variables | Read-only  |
  | Members   | Read-only  |

  **Account Permissions**

  | Scope           | Permission |
  | --------------- | ---------- |
  | Email addresses | Read-only  |

- Subscribe app to the following events:

  ```
  Issue comment
  Pull request
  Workflow run
  ```

- For local development the GitHub App Webhook must be configured to receive events at a URL that is accessible from the internet using [smee](https://smee.io/).

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
| `/deploy stage --retain`<br>` /deploy prod --retain` | Promote pull request to next environment, but retain the lock on the current one as well. Can also be used with --force.            |
| `/free`                                              | Remove lock on all deployed environments for others to use it.                                                                      |
| `/free stage`                                        | Remove lock on specified environment only for others to use it.                                                                     |
| `/rollback`                                          | Rollback current locked environment to the previous version. If there are multiple environments locked, you must specify which one. |
| `/rollback prod`<br>`/rollback stage`                | Rollback a specific environment to the previous version. You must have a lock on an environment to roll it back.                    |
| `/rollback prod 2`                                   | Rollback a specific environment multiple versions in history.                                                                       |
| `/add mydomain-api-service mydomain-app-main`        | Add services to be built and deployed with the pull request that have no code changes.                                              |

# Development

1. Download the private key from the your organizations GitHub App page and rename to `gh_app_key.pem` in this root folder.
2. Create a `.env` file similar to `.env.example` and set actual values.
3. Install dependencies with `npm install`.
4. Start webhook proxy with `npm run webhook`.
5. Start the server with `npm start`.

## Debugging

1. Open this repository root with VS Code.
2. Set a breakpoint in the App.
3. Click on the **Debug** icon on the left nav.
4. Select **PR Deploy App** from the **RUN AND DEBUG** drop down.
5. Click the **Start Debugging** icon.

## Deployment

1. Each pull request should be deployed as ready in the CD workflow.
2. Only once deployed and verified the pull request should be merged to main.
3. This application is deployed to a kubernetes cluster using the helm chart.

## Usage

With your server running, you can now create a pull request on any repository that
your app can access. GitHub will emit a `pull_request.opened` event and will deliver
the corresponding Webhook [payload](https://docs.github.com/webhooks-and-events/webhooks/webhook-events-and-payloads#pull_request) to your server.

The server in this example listens for `pull_request.opened` events and acts on
them by creating a comment on the pull request, with the message in `message.md`,
using the [octokit.js rest methods](https://github.com/octokit/octokit.js#octokitrest-endpoint-methods).

## Testing

Before you check in code, make sure to run `npm run checks` to verify everything is building, linting, and tested properly.

## Security considerations

To keep things simple, this example reads the `GITHUB_APP_PRIVATE_KEY` from the
environment. A more secure and recommended approach is to use a secrets management system
like [Vault](https://www.vaultproject.io/use-cases/key-management), or one offered
by major cloud providers:
[AWS Secrets Manager](https://docs.aws.amazon.com/AWSJavaScriptSDK/v3/latest/clients/client-secrets-manager/),

