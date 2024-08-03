Each repository can have it's own set of environments and services that **prdeploy** manages.  Here is how to configure those settings.

## Repository Setup

1. Follow the [prdeploy-webhooks App Setup](/prdeploy-webhooks/README.md#app-setup) to get prdeploy configured for your organization.
2. Add a `.prdeploy.yaml` settings file to the root of your repository:

```yaml
deployWorkflow: combined-deploy.yaml         # Worklow to trigger for deployments to an environment.
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
jira:
  addIssuesEnabled: true                     # If JIRA issues should be read from branch and added to PR.
settingsBranch: 'main'                       # Branch to retrieve repo settings from.
builds:
  checkPattern: 'Build helm chart'           # Pattern to know which checks are builds.
  workflowPattern: '/([^/]+?)-build.ya?ml'   # Pattern to know how to extract the build name from the workflow file.
slack:
  notificationsEnabled: true                 # Whether or not to send Slack notifications.
```

   _NOTE: For effeciency settings cache only updates every 5 minutes._

3. Labels will be automatically created for each environment in your repository.
    1. Environment labels should be the badge color you want for the environment icon.
4. In order for a completed or failed deploy status message to show the version, you must provide a `build-details` artifact to your builds that includes a `build-details.json` file with at least the following:
    1. The [build-details](/.github/actions/build-details/README.md#build-details-action) action can provide this.

```json
{
    "version": "2023.11.30-r7040754105"
}
```

5. Go to **Settings** and **General** for your repository and check `Always suggest updating pull request branches` to get the pull request **Update** button.
   1. This makes it much easier to update your pull requests to latest before deploying.