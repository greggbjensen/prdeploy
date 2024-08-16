The web portal for **prdeploy** allows you to view and manage settings and deployments. These are the installation instructions for Kubernetes.

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


## 2. Settings

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

[Next step - 7. GitHub Actions](./7-github-actions.md)
