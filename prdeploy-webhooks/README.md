# prdeploy-webhooks
The prdeploy API has webhooks to allow GitHub to pass events, as well as the GraphQL API for displaying the web page.

## App Setup

1. Install the **prdeploy** app to your repository.
2. Create a pull request and **prdeploy** will initialize all the required AWS Parameter Store variables.
3. The following values can be configured in AWS Parameter Store after they are created:

    Each parameter is prefixed with the path  `/prdeploy/myorg/`

    | Parameter               | Description                                       | Example                                                                                                                                                                                     |
    |-------------------------|---------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
    | DEPLOY_MANAGER_SITE_URL | The URL of the site the Deployment Manager is on. | https://prdeploy.myorg.com                                                                                                                                                                  |
    | EMAIL_ALIASES           | Aliases to correct email addresses for Slack.     | {<br>&nbsp;&nbsp;"jim.smith@myorg.com": "james.smith@myorg.com",<br>  &nbsp;&nbsp;"john.doe@myorg.com": "jdoe@myorg.com",<br>  &nbsp;&nbsp;"will.harris@myorg.com": "billyh@myorg.com"<br>} |
    | JIRA_HOST               | Domain name of your Atlassian instance.           | myorg.atlassian.net                                                                                                                                                                         |
    | JIRA_USERNAME           | Username the JIRA API token was generated for.    | greggbjensen@myorg.com                                                                                                                                                                      |
    | JIRA_PASSWORD           | JIRA API token.                                   | add1234.drgx541.edta541td                                                                                                                                                                   |
    | SLACK_EMAIL_DOMAIN      | Root domain for your organization in Slack.       | myorg.com                                                                                                                                                                                   |
    | SLACK_TOKEN             | SLACK API token.                                  | kjhjjh12_fsgfsgds_dfafda                                                                                                                                                                    |
    | SLACK_WEBHOOKS_DEPLOY   | SLACK Webhook URL for deployment channel notices. | https://hooks_slack_com/services/aaa/bbb/ccc                                                                                                                                                |
    | SLACK_WEBHOOKS_RELEASE  | SLACK Webhook URL for release channel notices.    | https://hooks_slack_com/services/aaa/bbb/ccc                                                                                                                                                |


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

  **Organization Permissions**

  | Scope     | Permission |
  | --------- | ---------- |
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
4. Select **prdeploy-webhooks** from the **RUN AND DEBUG** drop down.
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

