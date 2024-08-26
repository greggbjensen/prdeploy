For authorization of the portal to view deployments, we need to create an GitHub OAuth App.

## 1. Create GitHub OAuth App

![Setup GitHub OAuth App](../assets/images/screenshots/getting-started/github-oauth-app.png)
{: style="margin: 30px 0 60px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}

1. Still on your organization page, expand **Developer settings** on the left nav and choose **OAuth Apps**.
2. Click **New Org OAuth App**.
3. Enter the following information:

| Field                      | Value                                                                       |
| -------------------------- | --------------------------------------------------------------------------- |
| Aopplication name          | prdeploy                                                                    |
| Homepage URL               | https://prdeploy.myorg.com                                                  |
| Description                | Allows the entire build-deploy lifecycle to happen within a feature branch. |
| Authorization callback URL | https://prdeploy.myorg.com/login/callback                                   |

4. Click on **Register application**.

## 2. Application configuration

1. Click **Generate a new client secret** and copy the following for your notes for use with AWS Parameter Store:
    * The new Client secret
    * Client ID

2. Under **Application logo** click on **Upload a logo...**.
3. Download and select the file from `https://github.com/greggbjensen/prdeploy/blob/main/docs/assets/images/logo-128x128.png`.
4. Click **Update application**.

[Next step - 3. Slack App](./3-slack-app.md)
