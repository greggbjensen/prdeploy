import { WorkflowRun } from '@octokit/webhooks-types';
import slack from 'slack';
import { CreateCommentParams, PullRequest } from '@src/models';
import { StatusService } from '@src/services';
import { ContainerHelper } from '@test/helpers';
import { container } from 'tsyringe';
import { Octokit } from 'octokit';

const pullNumber = 3097;
const useMocks = !process.env.GITHUB_TOKEN || process.env.USE_MOCKS === 'true';
const createComment = jest.fn().mockResolvedValueOnce({});
let octokit: Octokit;

describe('postDeployStarted', () => {
  beforeEach(async () => {
    octokit = await ContainerHelper.registerDefaults();

    if (useMocks) {
      createComment.mockClear();

      Object.assign(octokit.rest.issues, {
        createComment
      });

      Object.assign(octokit.rest.users, {
        getByUsername: jest.fn().mockResolvedValueOnce({
          data: {
            name: 'Gregg Jensen',
            login: 'greggbjensen',
            email: ''
          }
        })
      });

      Object.assign(slack, {
        users: {
          lookupByEmail: jest.fn().mockResolvedValueOnce({
            ok: true,
            user: {
              id: 5467823,
              name: 'greggbjensen'
            }
          })
        }
      });
    }
  });

  it('posts pull request comment for deployment started', async () => {
    const run: WorkflowRun = {
      name: '[dev] My new feature',
      id: 7065200437,
      run_attempt: 1
    } as any;

    Object.assign(octokit.rest.actions, {
      getWorkflowRun: jest.fn().mockResolvedValueOnce({
        data: run
      })
    });

    const service = container.resolve(StatusService);
    await service.postDeployStarted(
      {
        number: pullNumber,
        user: {
          login: 'greggbjensen'
        },
        labels: [
          {
            name: 'dev'
          },
          {
            name: 'dev-lock'
          }
        ]
      } as PullRequest,
      run
    );

    if (useMocks) {
      const commentParams = createComment.mock.calls[0][0] as CreateCommentParams;
      expect(commentParams).toBeTruthy();
      expect(commentParams.body)
        .toEqual(`[![dev](https://badgen.net/badge/dev/Deploy%20Started/0080ff?labelColor=yellow&icon=github&scale=1.2)](https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/7065200437/attempts/1 'Open the deploy')
`);
    }
  });

  it('posts pull request comment for rollback started', async () => {
    const run: WorkflowRun = {
      name: '[prod ROLLBACK] My new feature',
      id: 7065200437,
      run_attempt: 1
    } as any;

    Object.assign(octokit.rest.actions, {
      getWorkflowRun: jest.fn().mockResolvedValueOnce({
        data: run
      })
    });

    const service = container.resolve(StatusService);
    await service.postDeployStarted(
      {
        number: pullNumber,
        user: {
          login: 'greggbjensen'
        },
        labels: [
          {
            name: 'prod'
          },
          {
            name: 'prod-lock'
          }
        ]
      } as PullRequest,
      run
    );

    if (useMocks) {
      const commentParams = createComment.mock.calls[0][0] as CreateCommentParams;
      expect(commentParams).toBeTruthy();
      expect(commentParams.body)
        .toEqual(`[![prod](https://badgen.net/badge/prod/Rollback%20Started/cccc00?labelColor=green&icon=github&scale=1.2)](https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/7065200437/attempts/1 'Open the deploy')
[Approve deployment](https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/7065200437/attempts/1) to continue
`);
    }
  });
});
