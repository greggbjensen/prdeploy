import subprocess
import os
import argparse
import shutil
import re
from dotenv import load_dotenv
from termcolor import cprint

parser = argparse.ArgumentParser('local.py')
parser.add_argument("--skip-aws", help="Skips the AWS configuration.", action="store_true")
parser.add_argument("--skip-prereq", help="Skips the helm rerequisites.", action="store_true")
parser.add_argument("--skip-docker", help="Skips the docker build of the images.", action="store_true")
parser.add_argument("--skip-helm", help="Skips the prdeploy helm install.", action="store_true")
args = parser.parse_args()

load_dotenv()

if not args.skip_aws:
  cprint(f'## Configuring AWS profile {os.environ["AWS_DEFAULT_PROFILE"]}', 'cyan')
  p = subprocess.Popen(f'aws configure --profile {os.environ["AWS_DEFAULT_PROFILE"]}', stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
  p.stdin.write(os.environ["AWS_ACCESS_KEY_ID"].encode() + b'\n')
  p.stdin.write(os.environ["AWS_SECRET_ACCESS_KEY"].encode() + b'\n')
  p.stdin.write(os.environ["AWS_REGION"].encode() + b'\n')
  p.stdin.write(os.environ["AWS_OUTPUT"].encode() + b'\n')
  p.communicate()

  cprint('\n## Creating prdeploy-backend role\n', 'cyan')
  op = subprocess.run('aws iam list-open-id-connect-providers --query "OpenIDConnectProviderList | [0]"', encoding='utf-8', stdout=subprocess.PIPE)
  awsOpenIdProvider=op.stdout.rstrip()
  print(f'Open ID Provider: {awsOpenIdProvider}')
  if awsOpenIdProvider == 'null':
    cprint('Creating AWS Open ID Provider')
    osp = subprocess.Popen(f'openssl s_client -servername oidc.eks.{os.environ["AWS_REGION"]}.amazonaws.com -showcerts -connect oidc.eks.{os.environ["AWS_REGION"]}.amazonaws.com:443', encoding='utf-8', stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    osp.stdin.write('Q\n')
    out, err = osp.communicate()
    cert=re.search('(-----BEGIN CERTIFICATE-----\n(?:.*\n)+?-----END CERTIFICATE-----)', out).group(1)

    fp = subprocess.run('openssl x509 -fingerprint -noout', input=cert, encoding='utf-8', stdout=subprocess.PIPE)
    outThumbprint=fp.stdout.rstrip()
    thumbprint=re.sub('SHA1 Fingerprint=', '', outThumbprint).replace(':', '').lower()

    subprocess.run(f'aws iam create-open-id-connect-provider --client-id-list sts.amazonaws.com --thumbprint-list {thumbprint} --url https://kubernetes.default.svc.cluster.local', encoding='utf-8')
    op2 = subprocess.run('aws iam list-open-id-connect-providers --query "OpenIDConnectProviderList | [0]"', encoding='utf-8', stdout=subprocess.PIPE)
    print(f'Open ID Provider: {awsOpenIdProvider}')

  ap = subprocess.run('aws sts get-caller-identity --query Account --output text', encoding='utf-8', stdout=subprocess.PIPE)
  awsAccountId=ap.stdout.rstrip()
  print(f'AWS_ACCOUNT_ID: {awsAccountId}')

  if not os.path.exists('temp'):
    os.mkdir('temp')

  hasRole = False
  try:
      subprocess.check_output('aws iam get-role --role-name prdeploy-backend')
      hasRole = True
  except:
    print('Role does not exist yet, creating it.')

  if not hasRole:
    shutil.copyfile('assets/aws-iam-assume-role-policy.json', 'temp/aws-iam-assume-role-policy.json')
    with open('temp/aws-iam-assume-role-policy.json', 'r') as file:
      filedata = file.read()

    filedata = filedata.replace('{{AWS_REGION}}', os.environ["AWS_REGION"])
    filedata = filedata.replace('{{AWS_ACCOUNT_ID}}', awsAccountId)

    with open('temp/aws-iam-assume-role-policy.json', 'w') as file:
      file.write(filedata)

    subprocess.run('aws iam create-role --role-name prdeploy-backend --assume-role-policy-document file://temp/aws-iam-assume-role-policy.json')

  print('\nVerifying prdeploy-backend role policy.')
  shutil.copyfile('assets/aws-iam-policy.json', 'temp/aws-iam-policy.json')

  with open('temp/aws-iam-policy.json', 'r') as file:
    filedata = file.read()

  filedata = filedata.replace('{{AWS_REGION}}', os.environ["AWS_REGION"])
  filedata = filedata.replace('{{AWS_ACCOUNT_ID}}', awsAccountId)

  with open('temp/aws-iam-policy.json', 'w') as file:
    file.write(filedata)

  hasPolicy = False
  try:
      subprocess.run(f'aws iam get-policy --policy-arn arn:aws:iam::{awsAccountId}:policy/prdeploy-backend', encoding='utf-8', stdout=subprocess.PIPE)
      hasPolicy = True
  except:
    print('Policy does not exist yet, creating it.')

  if not hasPolicy:
    subprocess.run('aws iam create-policy --policy-name prdeploy-backend --policy-document file://temp/aws-iam-policy.json')

  subprocess.run(f'aws iam attach-role-policy --policy-arn arn:aws:iam::{awsAccountId}:policy/prdeploy-backend --role-name prdeploy-backend')

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

if not args.skip_helm:
  cprint('\n## Installing prdeploy chart.\n', 'cyan')
  awp = subprocess.run('aws sts get-caller-identity --query Account --output text', encoding='utf-8', stdout=subprocess.PIPE)
  awsAccountId=awp.stdout.rstrip()

  shutil.copyfile('assets/local-values.yaml', 'temp/local-values.yaml')

  with open('temp/local-values.yaml', 'r') as file:
    filedata = file.read()

  filedata = filedata.replace('{{AWS_REGION}}', os.environ["AWS_REGION"])
  filedata = filedata.replace('{{AWS_ACCOUNT_ID}}', awsAccountId)

  with open('temp/local-values.yaml', 'w') as file:
    file.write(filedata)

  os.chdir('../charts')
  subprocess.run(f'helm upgrade prdeploy ./prdeploy --values ../scripts/temp/local-values.yaml --reset-values --namespace prdeploy --install --create-namespace --wait --atomic')

cprint('\nLocal configuration complete.', 'green')