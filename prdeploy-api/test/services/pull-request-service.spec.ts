import { WorkflowRun } from '@octokit/webhooks-types';
import { expect } from '@jest/globals';
import { PullRequestService } from '@src/services';
import { ContainerHelper } from '@test/helpers';

import { container } from 'tsyringe';
import { PullRequest } from '@src/models';

const commitId = 'ac9b3c39161cce9fea4aed7ce6da8722e0a48740';
const pullNumber = 3097;
const useMocks = !process.env.GITHUB_TOKEN || process.env.USE_MOCKS === 'true';

describe('getAsync', () => {
  beforeEach(async () => {
    const octokit = await ContainerHelper.registerDefaults();

    if (useMocks) {
      Object.assign(octokit.rest.pulls, {
        get: jest.fn().mockResolvedValueOnce({
          data: {
            number: pullNumber,
            title: 'Some new feature',
            labels: [],
            head: {
              ref: 'SCRUM-12345-my-feature'
            }
          }
        })
      });
    }
  });

  it('gets a pull request by number', async () => {
    const service = container.resolve(PullRequestService);
    const pullRequest = await service.get(pullNumber);
    expect(pullRequest).not.toBeNull();
    expect(pullRequest.title).not.toBeFalsy();
  });
});

describe('getByCommit', () => {
  beforeEach(async () => {
    const octokit = await ContainerHelper.registerDefaults();

    if (useMocks) {
      Object.assign(octokit.rest.repos, {
        listPullRequestsAssociatedWithCommit: jest.fn().mockResolvedValueOnce({
          data: [
            {
              number: pullNumber,
              title: 'Some new feature',
              labels: [],
              head: {
                ref: 'SCRUM-12345-my-feature'
              }
            }
          ]
        })
      });
    }
  });

  it('gets a pull request by commit', async () => {
    const service = container.resolve(PullRequestService);
    const pullRequest = await service.getByCommit(commitId);
    expect(pullRequest).not.toBeNull();
    expect(pullRequest.title).not.toBeFalsy();
  });
});

describe('getEnvironment', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();
  });

  it('gets environment from workflow run name', async () => {
    const service = container.resolve(PullRequestService);
    const run: WorkflowRun = {
      name: '[stage] SCRUM-12345-my-feature: This is a test commit message.'
    } as any;

    const environment = service.getEnvironment(null, run);
    expect(environment).toBeTruthy();
    expect(environment).toBe('stage');
  });

  it('gets environment from workflow run name with rollback', async () => {
    const service = container.resolve(PullRequestService);
    const run: WorkflowRun = {
      name: '[prod ROLLBACK] SCRUM-12345-my-feature: This is a test commit message.'
    } as any;

    const environment = service.getEnvironment(null, run);
    expect(environment).toBeTruthy();
    expect(environment).toBe('prod');
  });

  it('gets environment from pull request when in single environment and no run', async () => {
    const service = container.resolve(PullRequestService);
    const pullRequest: PullRequest = {
      labels: [
        {
          name: 'stage'
        },
        {
          name: 'stage-lock'
        }
      ]
    } as any;

    const environment = service.getEnvironment(pullRequest);
    expect(environment).toBeTruthy();
    expect(environment).toBe('stage');
  });

  it('gets environment by priority from pull request when in multiple environments and no run', async () => {
    const service = container.resolve(PullRequestService);
    let pullRequest: PullRequest = {
      labels: [
        {
          name: 'dev'
        },
        {
          name: 'dev-lock'
        },
        {
          name: 'stage'
        },
        {
          name: 'stage-lock'
        }
      ]
    } as any;

    let environment = service.getEnvironment(pullRequest);
    expect(environment).toBeTruthy();
    expect(environment).toBe('stage');

    pullRequest = {
      labels: [
        {
          name: 'dev'
        },
        {
          name: 'dev-lock'
        },
        {
          name: 'prod'
        },
        {
          name: 'prod-lock'
        },
        {
          name: 'stage'
        },
        {
          name: 'stage-lock'
        }
      ]
    } as any;

    environment = service.getEnvironment(pullRequest);
    expect(environment).toBeTruthy();
    expect(environment).toBe('prod');
  });
});

describe('isRollback', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();
  });

  it('gets rollback status from workflow run name when present', async () => {
    const service = container.resolve(PullRequestService);
    const run: WorkflowRun = {
      name: '[prod ROLLBACK] SCRUM-12345-my-feature: This is a test commit message.'
    } as any;

    const isRollback = service.isRollback(run);
    expect(isRollback).toBeTruthy();
  });

  it('gets not rollback status from workflow run name when not present', async () => {
    const service = container.resolve(PullRequestService);
    const run: WorkflowRun = {
      name: '[prod] SCRUM-12345-my-feature: This is a test commit message.'
    } as any;

    const isRollback = service.isRollback(run);
    expect(isRollback).toBeFalsy();
  });
});

describe('getRollbackCount', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();
  });

  it('gets default rollback count of 1 when not present', async () => {
    const service = container.resolve(PullRequestService);
    const run: WorkflowRun = {
      name: '[prod ROLLBACK] SCRUM-12345-my-feature: This is a test commit message.'
    } as any;

    const rollbackCount = service.getRollbackCount(run);
    expect(rollbackCount).toBe(1);
  });

  it('gets actual rollback count when present', async () => {
    const service = container.resolve(PullRequestService);
    const run: WorkflowRun = {
      name: '[prod ROLLBACK 3] SCRUM-12345-my-feature: This is a test commit message.'
    } as any;

    const rollbackCount = service.getRollbackCount(run);
    expect(rollbackCount).toBe(3);
  });
});
