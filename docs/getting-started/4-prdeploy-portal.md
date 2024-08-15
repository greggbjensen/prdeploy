The web portal for **prdeploy** allows you to view and manage settings and deployments.  These are the installation instructions for Kubernetes.

Each repository can have it's own set of environments and services that **prdeploy** manages.  Here is how to configure those settings.

## Helm chart install

1. Install the prerequists in Kubernetes:
    1. ingress-nginx
    2. cert-manager with letsencrypt
    3. External secrets

   _NOTE: For effeciency settings cache only updates every 5 minutes._

3. Labels will be automatically created for each environment in your repository.
    1. Environment labels should be the badge color you want for the environment icon.
4. In order for a completed or failed deploy status message to show the version, you must provide a `build-details` artifact to your builds that includes a `build-details.json` file with at least the following:
    1. The [build-details](/.github/actions/build-details/README.md#build-details-action) action can provide this.

```json
{
    "version": "2023.11.30-r7040754105"
}
```

5. Go to **Settings** and **General** for your repository and check `Always suggest updating pull request branches` to get the pull request **Update** button.
   1. This makes it much easier to update your pull requests to latest before deploying.