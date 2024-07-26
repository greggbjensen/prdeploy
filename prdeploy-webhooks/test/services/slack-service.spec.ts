import { expect } from '@jest/globals';
import { SlackService } from '@src/services';
import { ContainerHelper } from '@test/helpers';
import { container } from 'tsyringe';
import slack from 'slack';

const useMocks =
  !(process.env.SLACK_TOKEN && process.env.SLACK_EMAIL_DOMAIN && process.env.GITHUB_TOKEN) ||
  process.env.USE_MOCKS === 'true';

let fetchMock: jest.SpyInstance = null;

describe('postMessage', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();

    if (useMocks) {
      fetchMock = jest.spyOn(global, 'fetch').mockImplementation(
        jest.fn().mockResolvedValueOnce(async () => ({
          statuc: 200,
          text: async () => 'Resulting text from message'
        }))
      );

      fetchMock.mockClear();
    }
  });

  it('posts message to channel', async () => {
    const service = container.resolve(SlackService);
    await service.postMessage(
      'deploy',
      `{
    "blocks": [
        {
            "type": "section",
            "text": {
                "type": "plain_text",
                "text": "Test message from PR Deploy GitHub App",
                "emoji": true
            }
        }
    ]
}`
    );
  });
});

describe('lookupUser', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();

    if (useMocks) {
      Object.assign(slack, {
        users: {
          lookupByEmail: jest.fn().mockResolvedValueOnce({
            ok: true,
            user: {
              id: 789745,
              name: 'dhivya'
            }
          })
        }
      });
    }
  });

  it('gets slack user by name', async () => {
    const service = container.resolve(SlackService);
    const name = 'dhivya-selvam';
    const user = await service.lookupUser({ name, email: '' } as any);

    expect(user).not.toBeNull();
    expect(user?.username).toBeTruthy();
    expect(user?.id).toBeTruthy();
  });

  it('gets slack user using email alias', async () => {
    const service = container.resolve(SlackService);
    const user = await service.lookupUser({ name: '', email: 'jdoe@myorg.com' } as any);

    expect(user).not.toBeNull();
    expect(user?.username).toBeTruthy();
    expect(user?.id).toBeTruthy();
  });

  it('gets slack user by email', async () => {
    const service = container.resolve(SlackService);
    const user = await service.lookupUser({
      name: 'Gregg B. Jensen',
      email: 'greggbjensen@users.noreply.github.com'
    } as any);

    expect(user).not.toBeNull();
    expect(user?.username).toBeTruthy();
    expect(user?.id).toBeTruthy();
  });

  it('returns null if name and email are empty', async () => {
    const service = container.resolve(SlackService);
    const user = await service.lookupUser({ name: '', email: '' } as any);

    expect(user).toBeNull();
  });
});
