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



[Next step - 7. GitHub Actions](./7-github-actions.md)
