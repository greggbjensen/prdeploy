GitHub App that allows the entire build-deploy lifecycle to happen within a feature branch.  If any problems are found in the deployment, a new commit is done and rolled through without needing multiple PRs.

![Logo](./assets/images/logo.svg){: style="height:180px; display: block; margin: 0 auto 20px auto"}

Release multiple times per day with less issues.
{: style="text-align: center; color: #1192ec; margin: 0 0 70px 70px;"}

![Feature Branch Deployments](./assets/images/feature-branch-deployments.jpg)

## Features

The **prdeploy** application supplies the following features:

### Commend commands

Post [comment commands](comment-commands.md) to manage your deployments without leaving your pull request.

![Deploy comment command](./assets/images/screenshots/prcomment-deploy.png)
{: style="margin: 30px 0;"}


### prdeploy portal

The [prdeploy portal](prdeploy-portal.md) dashboard to overview releases and deployment queues and act on them.

![prdeploy deployments](./assets/images/screenshots/prdeploy-portal.png)
{: style="margin: 30px 0 60px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}

### Microservice deploy

Using a **monorepo**, the [Microservice deploy](microservice-deploy.md) determines which services to deploy within a pull requests, and keeps each environment in sync with a stable release to production.

![Combined deploy workflow](./assets/images/screenshots/getting-started/combined-deploy-workflow.png)
{: style="margin: 30px 0 30px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}


### Slack notifications

Automated [Slack notifications](slack-notifications.md) for posting events to keep your process moving.

![Slack deploy notification](./assets/images/screenshots/slack-deploy-notification.png)
{: style="margin: 30px 0;"}

### JIRA links

Associate [JIRA links](jira-links.md) update your pull request with a link to each issue in the branch name.

![JIRA issue link](./assets/images/screenshots/jira-links.png)
{: style="margin: 30px 0 0 0;"}

### Deployment queues

The [deployment queues](deployment-queues.md) organize environment use by putting a pull request in waiting.

![Deployment queue comment](./assets/images/screenshots/deployment-queues.png)
{: style="margin: 30px 0 0 0;"}

## Getting started

To get started with your own hosted **prdeploy** app, you can follow the [Getting started](getting-started/overview.md) section.
