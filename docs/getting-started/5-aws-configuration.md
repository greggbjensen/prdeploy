The **prdeploy** app loads external secrets, saves settings, and tracks deployment versions through AWS parameter store.

## 1. AWS role and permissions

![Setup AWS Role](/assets/images/screenshots/getting-started/aws-role.png)
{: style="margin: 30px 0 60px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}

1. Navigate to [https://aws.amazon.com](https://aws.amazon.com).
2. Go to **IAM**, then **Roles**.
3. Create a new role of `prdeloy-backend` with the following assume policy.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::{{AWS_ACCOUNT_ID}}:oidc-provider/oidc.eks.{{AWS_REGION}}.amazonaws.com/id/{{PROVDER_ID}}"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "oidc.eks.{{AWS_REGION}}.amazonaws.com/id/{{PROVDER_ID}}:sub": "system:serviceaccount:prdeploy:prdeploy-backend"
        }
      }
    }
  ]
}
```

4. Go to **Policies**.
5. Create the following IAM policy as `prdeploy-backend` and associate it to the role:

![Setup AWS Policy](/assets/images/screenshots/getting-started/aws-policy.png)
{: style="margin: 30px 0 60px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}

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
        "arn:aws:ssm:{{AWS_REGION}}:{{AWS_ACCOUNT_ID}}:parameter/prdeploy*"
      ]
    }
  ]
}
```

## 2. Parameter store configuration.

![Setup AWS Parameter Store](/assets/images/screenshots/getting-started/aws-parameter-store.png)
{: style="margin: 30px 0 60px 0; box-shadow: rgba(0, 0, 0, 0.2) 0px 3px 3px -2px, rgba(0, 0, 0, 0.14) 0px 3px 4px 0px, rgba(0, 0, 0, 0.12) 0px 1px 8px 0px;"}

1. Navigate to **AWS Systems Manager**, then **Parameter Store**.
2. Add the following entries as `SecureString` to AWS Parameter Store:

| Name                                 | Value                                                                   |
| ------------------------------------ | ----------------------------------------------------------------------- |
| /prdeploy/APP_ID                     | ID from GitHub App.                                                     |
| /prdeploy/WEBHOOK_SECRET             | Webhook secret configured for GitHub App.                               |
| /prdeploy/gh_app_key.pem             | Secret app key downloaded from GitHub App, copy and paste contents.     |
| /prdeploy/GitHubAuth\_\_ClientId     | Client ID for GitHub OAuth App.                                         |
| /prdeploy/GitHubAuth\_\_ClientSecret | Client Secret for GitHub OAuth App.                                     |
| /prdeploy/Jwt\_\_Key                 | Generated JWT validation key.                                           |
| /prdeploy/Jwt\_\_TokenEncryptionKey  | Generated JWT token encryption key.<br>Should not be the same as above. |

`NOTE:` To generate a unique JWT and encryption key, you can run the following:

```bash
node -e "console.log(require('crypto').randomBytes(256).toString('base64'));"
```

[Next step - 6. prdeploy portal](./6-prdeploy-portal.md)
