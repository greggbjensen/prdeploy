When a pull request is create the prdeploy app will check the branch name for any issue IDs in JIRA and link them to your description.

## 1. Create JIRA API token

![Setup JiRA API Token](/assets/images/screenshots/getting-started/jira-token.png)
{: style="margin: 30px 0 60px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}

1. Navigate to your JIRA instance https://myorg.atlassian.net/jira.
2. Click on your user photo in the top right and choose **Manage Account**.
3. Click on the **Security** tab.
4. Under **API tokens** click on **Create and manage API tokens**.
5. Click **Create API token**.
6. Enter the **Label** of `prdeloy` and click **Create**.
7. Copy the API token to your notes for the **prdeloy** portal settings.

[Next step - 5. AWS Configuration](./5-aws-configuration.md)
