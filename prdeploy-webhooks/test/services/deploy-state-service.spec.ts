import { expect } from '@jest/globals';
import { DeployStateService, ParameterService } from '@src/services';
import { container } from 'tsyringe';
import { SSMClient } from '@aws-sdk/client-ssm';
import { SSM_CLIENT } from '@src/injection-tokens';
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

  const parameterService = container.resolve(ParameterService);
  await parameterService.setObject('DEPLOY_STATE_DEV', null);
  await parameterService.setObject('DEPLOY_STATE_STAGE', null);
});

afterEach(async () => {
  const parameterService = container.resolve(ParameterService);
  await parameterService.setObject('DEPLOY_STATE_DEV', null);
  await parameterService.setObject('DEPLOY_STATE_STAGE', null);
});

describe('update', () => {
  it('updates by adding services deploy state variable in AWS Parameter Store', async () => {
    const service = container.resolve(DeployStateService);
    await service.update('dev', 3445, [
      {
        name: 'myorg-api-internal',
        version: '2024.2.25-r8067229180',
        runId: 8067207106
      }
    ]);

    let actual = await service.get('dev');

    expect(actual).toBeTruthy();
    expect(actual.services).toBeTruthy();
    expect(actual.services.length).toEqual(1);

    let actualService = actual.services.find(s => s.name === 'myorg-api-internal');
    expect(actualService).toBeTruthy();
    expect(actualService.version).toBe('2024.2.25-r8067229180');

    actual = await service.update('dev', 3296, [
      {
        name: 'myorg-app-main',
        version: '2024.2.27-r8068273297',
        runId: 8068273297
      },
      {
        name: 'myorg-api-service',
        version: '2024.2.27-r8067229180',
        runId: 8067229180
      }
    ]);

    expect(actual).toBeTruthy();
    expect(actual.pullNumber).toBe(3296);
    expect(actual.services).toBeTruthy();
    expect(actual.services.length).toEqual(3);

    actualService = actual.services.find(s => s.name === 'myorg-api-internal');
    expect(actualService).toBeTruthy();
    expect(actualService.version).toBe('2024.2.25-r8067229180');
  });
});

describe('get', () => {
  it('creates a default deploy state when none is available', async () => {
    const service = container.resolve(DeployStateService);
    const actual = await service.get('dev');

    expect(actual).toBeTruthy();
    expect(actual.pullNumber).toBe(0);
    expect(actual.services).toBeDefined();
    expect(actual.services.length).toBe(0);
  });
});

describe('diff', () => {
  it('determines the difference between two environment deploy states', async () => {
    const service = container.resolve(DeployStateService);
    await service.update('dev', 3284, [
      {
        name: 'myorg-api-service',
        version: '2024.2.25-r8067229180',
        runId: 8067207106
      },
      {
        name: 'myorg-app-main',
        version: '2024.2.27-r8067229180',
        runId: 8067229180
      }
    ]);

    await service.update('stage', 3263, [
      {
        name: 'myorg-api-service',
        version: '2024.2.28-r8067285421',
        runId: 8067285421
      },
      {
        name: 'myorg-app-main',
        version: '2024.2.27-r8068273297',
        runId: 8068273297
      }
    ]);

    const actual = await service.diff('stage', 'dev');

    expect(actual).toBeTruthy();
    expect(actual.pullNumber).toBe(3263);
    expect(actual.services).toBeDefined();
    expect(actual.services.length).toBeGreaterThan(0);

    let serviceActual = actual.services.find(s => s.name === 'myorg-api-service');
    expect(serviceActual.version).toBe('2024.2.28-r8067285421');
    expect(serviceActual.runId).toBe(8067285421);

    // myorg-app-main is not on dev yet.
    serviceActual = actual.services.find(s => s.name === 'myorg-app-main');
    expect(serviceActual.version).toBe('2024.2.27-r8068273297');
    expect(serviceActual.runId).toBe(8068273297);

    // myorg-api-service is not different.
    expect(actual.services.length).toBe(2);
  });
});
