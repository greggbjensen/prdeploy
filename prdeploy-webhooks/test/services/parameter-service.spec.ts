import { expect } from '@jest/globals';
import { ParameterService } from '@src/services';
import { container } from 'tsyringe';
import { SSMClient } from '@aws-sdk/client-ssm';
import { SSM_CLIENT } from '@src/injection-tokens';
import { Build } from '@src/models';
import { ContainerHelper } from '@test/helpers';

// Allow test to switch between integration and mocks.
const useMocks =
  !(process.env.AWS_REGION && process.env.AWS_ACCESS_KEY_ID && process.env.AWS_SECRET_ACCESS_KEY) ||
  process.env.USE_MOCKS === 'true';

beforeEach(async () => {
  await ContainerHelper.registerDefaults(!useMocks);

  if (!useMocks) {
    container.register(SSM_CLIENT, {
      useFactory: () => {
        return new SSMClient({
          region: process.env.AWS_REGION,
          credentials: {
            accessKeyId: process.env.AWS_ACCESS_KEY_ID,
            secretAccessKey: process.env.AWS_SECRET_ACCESS_KEY
          }
        });
      }
    });
  }
});

describe('setString', () => {
  it('sets a repository string variable in AWS Parameter Store', async () => {
    const service = container.resolve(ParameterService);
    await service.setString('TEST_REPO_VARIABLE', 'Repo variable for testing');

    const value = await service.getString('TEST_REPO_VARIABLE');
    expect(value).toEqual('Repo variable for testing');
  });

  it('sets a organization string variable in AWS Parameter Store', async () => {
    const service = container.resolve(ParameterService);
    await service.setString('TEST_ORG_VARIABLE', 'Org variable for testing', 'Org');

    const value = await service.getString('TEST_ORG_VARIABLE', 'Org');
    expect(value).toEqual('Org variable for testing');
  });

  it('sets a repository string secret in AWS Parameter Store.', async () => {
    const service = container.resolve(ParameterService);
    await service.setString('TEST_REPO_SECRET', 'Repo secret for testing', 'Repo', true);

    const value = await service.getString('TEST_REPO_SECRET');
    expect(value).toEqual('Repo secret for testing');
  });

  it('sets a organization string secret in AWS Parameter Store', async () => {
    const service = container.resolve(ParameterService);
    await service.setString('TEST_ORG_SECRET', 'Org secret for testing', 'Org', true);

    const value = await service.getString('TEST_ORG_SECRET', 'Org');
    expect(value).toEqual('Org secret for testing');
  });
});

describe('getString', () => {
  it('gets a repository string variable from history', async () => {
    const service = container.resolve(ParameterService);
    await service.setString('TEST_REPO_VARIABLE', 'Repo value 1');
    await service.setString('TEST_REPO_VARIABLE', 'Repo value 2');

    let value = await service.getString('TEST_REPO_VARIABLE');
    expect(value).toEqual('Repo value 2');

    value = await service.getString('TEST_REPO_VARIABLE', 'Repo', 1);
    expect(value).toEqual('Repo value 1');
  });
});

describe('setObject', () => {
  it('sets an object value in AWS Parameter Store', async () => {
    const service = container.resolve(ParameterService);
    const objectValue: Build = {
      runId: 8011629437,
      name: 'mydomain-app-main',
      title: 'TypeScript formatting updates',
      version: '2024.23.2-r8011629437',
      url: 'https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/8011629437'
    };
    await service.setObject('TEST_OBJ_REPO_VARIABLE', objectValue);

    const value = await service.getObject<Build>('TEST_OBJ_REPO_VARIABLE');

    expect(value).toBeTruthy();
    expect(value.runId).toEqual(8011629437);
    expect(value.name).toEqual('mydomain-app-main');
    expect(value.title).toEqual('TypeScript formatting updates');
    expect(value.version).toEqual('2024.23.2-r8011629437');
    expect(value.url).toEqual('https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/8011629437');
  });
});
