In order to be able to listen and respond to pull request events, we need to create a GitHub App for prdeploy.

## 1. App

1. Create a new **prdeploy** GitHub App in your organization.

## 2. Permissions

Grant app the following permissions:

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


## 3. Events

Subscribe app to these events:

  ```
  Issue comment
  Pull request
  Workflow run
  ```

## 4. Installation

Install the **prdeploy** app to your repository.

## 5. Configure Parameter Store

1. Create a pull request and **prdeploy** will initialize all the required AWS Parameter Store variables.
2. The following values can be configured in AWS Parameter Store after they are created:

    Each parameter is prefixed with the path  `/prdeploy/myorg/`

    | Parameter               | Description                                       | Example                                                                                                                                 |
    |-------------------------|---------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|
    | DEPLOY_MANAGER_SITE_URL | The URL of the site the Deployment Manager is on. | https://prdeploy.myorg.com                                                                                                              |
    | EMAIL_ALIASES           | Aliases to correct email addresses for Slack.     | "jim.smith@myorg.com": "james.smith@myorg.com"<br>"john.doe@myorg.com": "jdoe@myorg.com"<br>"will.harris@myorg.com": "billyh@myorg.com" |
    | JIRA_HOST               | Domain name of your Atlassian instance.           | myorg.atlassian.net                                                                                                                     |
    | JIRA_USERNAME           | Username the JIRA API token was generated for.    | greggbjensen@myorg.com                                                                                                                  |
    | JIRA_PASSWORD           | JIRA API token.                                   | abc.def.hij                                                                                                                             |
    | SLACK_EMAIL_DOMAIN      | Root domain for your organization in Slack.       | myorg.com                                                                                                                               |
    | SLACK_TOKEN             | SLACK API token.                                  | abc_def_hij                                                                                                                             |
    | SLACK_WEBHOOKS_DEPLOY   | SLACK Webhook URL for deployment channel notices. | https://hooks_slack_com/services/aaa/bbb/ccc                                                                                            |
    | SLACK_WEBHOOKS_RELEASE  | SLACK Webhook URL for release channel notices.    | https://hooks_slack_com/services/aaa/bbb/ccc                                                                                            |