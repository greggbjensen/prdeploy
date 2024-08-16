The web portal for **prdeploy** allows you to view and manage settings and deployments. These are the installation instructions for Kubernetes.

Each repository can have it's own set of environments and services that **prdeploy** manages. Here is how to configure those settings.

## AWS configuration

1. Create the following IAM policy as `prdeploy-backend`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ssm:PutParameter",
        "ssm:GetParametersByPath",
        "ssm:GetParameter"
      ],
      "Resource": [
        "arn:aws:ssm:{{AWS_REGION}}:{{AWS_ACCOUNT_ID}}:parameter//prdeploy/*"
      ]
    }
  ]
}
```

2. Add the following entries as `SecureString` to AWS Parameter Store:

| Name                                 | Value                                                                   |
| ------------------------------------ | ----------------------------------------------------------------------- |
| /prdeploy/APP_ID                     | ID from GitHub App.                                                     |
| /prdeploy/WEBHOOK_SECRET             | Webhook secret configured for GitHub App.                               |
| /prdeploy/gh_app_key.pem             | Secret app key downloaded from GitHub App.                              |
| /prdeploy/GitHubAuth\_\_ClientId     | Client ID for GitHub OAuth App.                                         |
| /prdeploy/GitHubAuth\_\_ClientSecret | Client Secret for GitHub OAuth App.                                     |
| /prdeploy/Jwt\_\_Key                 | Generated JWT validation key.                                           |
| /prdeploy/Jwt\_\_TokenEncryptionKey  | Generated JWT token encryption key.<br>Should not be the same as above. |

`NOTE:` To generate a unique encryption key, you can run the following:

```bash
node -e "console.log(require('crypto').randomBytes(256).toString('base64'));"
```

## Helm chart install

1. Install the prerequists in Kubernetes:

   1. ingress-nginx
   2. cert-manager with letsencrypt
   3. External secrets

   _NOTE: For effeciency settings cache only updates every 5 minutes._

2. Labels will be automatically created for each environment in your repository.
   1. Environment labels should be the badge color you want for the environment icon.
3. In order for a completed or failed deploy status message to show the version, you must provide a `build-details` artifact to your builds that includes a `build-details.json` file with at least the following:
   1. The [build-details](/.github/actions/build-details/README.md#build-details-action) action can provide this.

```json
{
  "version": "2023.11.30-r7040754105"
}
```

5. Go to **Settings** and **General** for your repository and check `Always suggest updating pull request branches` to get the pull request **Update** button.
   1. This makes it much easier to update your pull requests to latest before deploying.
