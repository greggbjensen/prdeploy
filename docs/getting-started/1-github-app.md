In order to be able to listen and respond to pull request events, we need to create a GitHub App for prdeploy.

## Create GitHub App

1. Go to https://github.com and sign in.
2. Click on your profile photo in the top right and choose **Your Organizations**.
3. Click on the organization you want to add **prdeploy** to.
4. Select the **Settings** tab, expand **Developer settings** on the left nav and choose **GitHub Apps**.
5. Click **New GitHub App** in the top right.
6. Fill in at least the following information:

| Field           | Value                                                                       |
| --------------- | --------------------------------------------------------------------------- |
| GitHub App Name | prdeploy                                                                    |
| Description     | Allows the entire build-deploy lifecycle to happen within a feature branch. |
| Homepage URL    | https://prdeploy.myorg.com                                                  |
| Webhook URL     | https://prdeploy.myorg.com/webhooks                                         |
| Secret          | _Any secure set of characters (see below)._                                 |

Generating a secret with Node:

```bash
node -e "console.log(require('crypto').randomBytes(64).toString('base64'));"
```

7. Set each of the **Permissions** as follows:

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

| Scope   | Permission |
| ------- | ---------- |
| Members | Read-only  |

**Account Permissions**

| Scope           | Permission |
| --------------- | ---------- |
| Email addresses | Read-only  |

8. Subscribe to the following events:

```
Issue comment
Pull request
Workflow run
```

9. Click **Create GitHub App**.
