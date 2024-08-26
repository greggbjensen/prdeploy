The web portal for **prdeploy** allows you to view and manage settings and deployments. These are the installation instructions for Kubernetes.

![prdeploy settings](/assets/images/screenshots/prdeploy-portal-settings.png)
{: style="margin: 30px 0 40px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}

## 1. Helm chart install

1. Install the prerequists in Kubernetes:
   1. [ingress-nginx](https://kubernetes.github.io/ingress-nginx/deploy/#quick-start)
   2. [cert-manager with letsencrypt](https://medium.com/@manojit123/lets-encrypt-certificate-using-cert-manager-on-kubernetes-http-challenge-687ce3718baf)
   3. [external-secrets](https://external-secrets.io/v0.4.3/guides-getting-started/)
2. Create a new file `prdeploy-values.yaml` and add the following content:

```yaml
global:
  ingress:
    host: prdeploy.myorg.com
  aws:
    region: us-west-2
  serviceAccounts:
    backend:
      annotations:
        eks.amazonaws.com/role-arn: '<Role ARN of prdeploy-backend from AWS Configuration>'

chart-prdeploy-app:
  github:
    oauth:
      clientId: '<GitHub App OAuth Client ID>'
```

2. Run the following command with helm:

```bash
helm upgrade prdeploy oci://registry-1.docker.io/greggbjensen/prdeploy \
  --install --reset-values --force --create-namespace -n prdeploy \
  -f ./prdeploy-values.yaml
```

3. Configure each of the following steps in the **prdeploy** portal.

## 2. Repositories

![prdeploy repositories](/assets/images/screenshots/prdeploy-portal-repositories.png)
{: style="margin: 30px 0 40px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}

_NOTE: For each of the following sections, you can enter the settings either in **Owner Defaults** for all repositories or **Repository Override** for a specific repository.  Any repository can override the **Owner Defaults**._

1. Log into your **prdeploy.myorg.com** site with your GitHub account.
2. You will be taken to the **Repositories** page.
3. Click **Add Repository**.
4. Fill in the fields as follows:

| Field      | Value                                                                             |
| ---------- | --------------------------------------------------------------------------------- |
| Owner      | The GitHub company name or a company repository or your login for a personal one. |
| Repository | The git repository you want to do deployments from.                               |

_NOTE: You must click **Save** in the top right before leaving the **Settings** area to save changes._

## 3. Environments

![prdeploy settings Environments](/assets/images/screenshots/prdeploy-portal-settings-environments.png)
{: style="margin: 30px 0 40px 0;"}

1. In the portal, click **Settings** on the left nav.
2. Go to the **Repository Override** tab and click **Add Environent**.
3. Type in the **Name** of the environment and click **Add environment**
   1. Repeat this for each environment you want to add (Dev, Stage, Prod, etc.).
4. Populate at least the following fields for each environment:

| Field | Value                                                                                 |
| ----- | ------------------------------------------------------------------------------------- |
| Queue | The unique variable name, without spaces, to use for that environment (DEV_PR_QUEUE). |
| URL   | The full URL where the environment can be viewed.                                     |

## 4. Services

![prdeploy settings Services](/assets/images/screenshots/prdeploy-portal-settings-services.png)
{: style="margin: 30px 0 40px 0;"}

1. Go to the **Services** link on the left panel.
2. Click on **Add Service**.
3. Type the **Service Name** of what you want deployed and click **Add Service**.
4. Adjust the **Path** of the service as needed.
   1. This will be the path from the root of your repository to where the code for that service is.
   2. Do this for each service you want to deploy.

## 5. Slack

![prdeploy settings Slack](/assets/images/screenshots/prdeploy-portal-settings-slack.png)
{: style="margin: 30px 0 60px 0;"}

1. Check the **Notifications enabled** box.
2. Fill in the fields as follows:

| Field        | Value                                                                                 |
| ------------ | ------------------------------------------------------------------------------------- |
| Token        | The **Bot User OAuth Token** you kept from the [3. Slack App](./3-slack-app.md) step. |
| Email domain | The root domain of your email addresses you use in Slack (myorg.com).                 |
| Deploy URL   | The deploy webhook URL you kept from the [3. Slack App](./3-slack-app.md) step.       |
| Release URL  | The release webhook URL you kept from the [3. Slack App](./3-slack-app.md) step.      |

_NOTE: As necessary, you can fill in email aliases if **prdeploy** finds the incorrect match for the email used by a team member._

## 6. JIRA

![prdeploy settings JIRA](/assets/images/screenshots/prdeploy-portal-settings-jira.png)
{: style="margin: 30px 0 40px 0;"}

1. Check the **Add issues enabled** box.
2. Fill in the fields as follows:

| Field    | Value                                                                                                                   |
| -------- | ----------------------------------------------------------------------------------------------------------------------- |
| Host     | The host name from the [4. Jira Integration](./4-jira-integration.md) step (myorg.atlassian.net).                       |
| Username | The email address of the user you created the API token for in the [4. Jira Integration](./4-jira-integration.md) step. |
| Token    | The **API token** you created under that user in the [4. Jira Integration](./4-jira-integration.md) step.               |

## 7. Deployment

![prdeploy settings Deployment](/assets/images/screenshots/prdeploy-portal-settings-deployment.png)
{: style="margin: 30px 0 40px 0;"}

1. The defaults here will generall be correct, verify the **Release Environment** and the **Default Environment**.
2. Make sure the **prdeploy portal URL** reflects the current site (https://prdeploy.myorg.com).
    1. This is used in pull request comment links.
3. Make sure to click **Save** in the top right before leaving the **Settings** page.


_NOTES:_

_**Builds workflow pattern** is the regular expression used to extract the service name to match with **Services** from your **GitHub Action** build workflow._

_**Builds check pattern** is the regular expression used to recognize the job within the matched **Build workflow** that produce a Docker image for your services._

[Next step - 7. GitHub Actions](./7-github-actions.md)
