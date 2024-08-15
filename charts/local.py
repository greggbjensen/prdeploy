import subprocess

print('Switching to Docker Kubernetes context.')
subprocess.run('kubectl config use-context docker-desktop')

print('\nInstalling prerequisite helm charts.')
subprocess.run('helm repo add external-secrets https://charts.external-secrets.io')
subprocess.run('helm upgrade external-secrets external-secrets/external-secrets -n external-secrets --create-namespace --set installCRDs=true --install')

print('\nInstalling prdeploy chart.')
subprocess.run('helm upgrade prdeploy ./prdeploy --values local-values.yaml --reset-values --namespace prdeploy --install --create-namespace --wait --atomic')