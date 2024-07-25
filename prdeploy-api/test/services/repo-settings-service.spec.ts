import { SSMClient } from '@aws-sdk/client-ssm';
import { expect } from '@jest/globals';
import { SSM_CLIENT } from '@src/injection-tokens';
import { RepoSettingsService } from '@src/services';
import { ContainerHelper } from '@test/helpers';
import { container } from 'tsyringe';

process.env.SKIP_REPO_SETTINGS = 'true';
const useMocks = !process.env.GITHUB_TOKEN || process.env.USE_MOCKS === 'true';

describe('get', () => {
  beforeEach(async () => {
    container.reset();

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

  it('gets settings from repository', async () => {
    const service = container.resolve(RepoSettingsService);
    const settings = await service.get();

    expect(settings).not.toBeFalsy();
  });
});
