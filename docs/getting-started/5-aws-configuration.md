The **prdeploy** app loads external secrets, saves settings, and tracks deployment versions through AWS parameter store.

## 1. AWS role and permissions

1. Create a new role of `prdeloy-backend` with the following assume policy.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::{{AWS_ACCOUNT_ID}}:oidc-provider/oidc.eks.{{AWS_REGION}}.amazonaws.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "oidc.eks.{{AWS_REGION}}.amazonaws.com:sub": "system:serviceaccount:prdeploy:prdeploy-backend"
        }
      }
    }
  ]
}
```

2. Create the following IAM policy as `prdeploy-backend`:

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

## 2. Parameter store configuration.

1. Navigate to...
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