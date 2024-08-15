import subprocess
import os
import argparse
from dotenv import load_dotenv
from termcolor import cprint

parser = argparse.ArgumentParser('local.py')
parser.add_argument("--skip-aws", help="Skips the AWS configuration.", action="store_true")
parser.add_argument("--skip-prereq", help="Skips the helm rerequisites.", action="store_true")
parser.add_argument("--skip-docker", help="Skips the docker build of the images.", action="store_true")
args = parser.parse_args()

load_dotenv()

if not args.skip_aws:
  cprint(f'## Configuring AWS profile {os.environ["AWS_DEFAULT_PROFILE"]}.', 'cyan')
  p = subprocess.Popen(f'aws configure --profile {os.environ["AWS_DEFAULT_PROFILE"]}', stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
  p.stdin.write(os.environ["AWS_ACCESS_KEY_ID"].encode() + b'\n')
  p.stdin.write(os.environ["AWS_SECRET_ACCESS_KEY"].encode() + b'\n')
  p.stdin.write(os.environ["AWS_REGION"].encode() + b'\n')
  p.stdin.write(os.environ["AWS_OUTPUT"].encode() + b'\n')
  p.communicate()

  cprint('\n## Configuring AWS Parameter Store required values.', 'cyan')
  prdeployParams = ['APP_ID', 'WEBHOOK_SECRET', 'gh_app_key.pem', 'GitHubAuth__ClientId', 'GitHubAuth__ClientSecret', 'Jwt__Key', 'Jwt__TokenEncryptionKey']
  for param in prdeployParams:
    print(param)
    subprocess.run(f'aws ssm put-parameter --name /prdeloy/{param} --value {os.environ[param]} --type SecureString --overwrite')

if not args.skip_docker:
  cprint('\n## Switching to Docker Kubernetes context.\n', 'cyan')
  subprocess.run('kubectl config use-context docker-desktop')

  cprint('\n## Building Docker images\n', 'cyan')

  print('Building prdeploy-api.')
  os.chdir('../prdeploy-api')
  subprocess.run('docker compose build')

  print('\nBuilding prdeploy-app.')
  os.chdir('../prdeploy-app')
  subprocess.run('docker compose build')

  print('\nBuilding prdeploy-webhooks.')
  os.chdir('../prdeploy-webhooks')
  subprocess.run('docker compose build')

if not args.skip_prereq:
  cprint('\n## Installing prerequisite helm charts.\n', 'cyan')
  subprocess.run('helm upgrade external-secrets external-secrets --repo https://charts.external-secrets.io --namespace external-secrets --create-namespace --set installCRDs=true --install')
  print('\n')
  subprocess.run('helm upgrade ingress-nginx ingress-nginx --repo https://kubernetes.github.io/ingress-nginx --namespace ingress-nginx --create-namespace --set controller.allowSnippetAnnotations=true --install')

cprint('\n## Installing prdeploy chart.\n', 'cyan')
os.chdir('../charts')
subprocess.run('helm upgrade prdeploy ./prdeploy --values ../scripts/local-values.yaml --reset-values --namespace prdeploy --install --create-namespace --wait --atomic')