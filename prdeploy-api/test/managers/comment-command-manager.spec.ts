import * as path from 'path';
import AdmZip from 'adm-zip';
import { CommentCommandManager } from '@src/managers';
import { CreateForIssueCommentParams, CreateWorkflowDispatchParams, PullRequest } from '@src/models';
import { ContainerHelper } from '@test/helpers';
import { container } from 'tsyringe';
import { Octokit } from '@octokit/rest';

const commentId = 2015899564;
const pullNumber = 8;
const useMocks = !process.env.GITHUB_TOKEN || process.env.USE_MOCKS === 'true';

const createForIssueComment = jest.fn().mockResolvedValueOnce({});
const createWorkflowDispatch = jest.fn().mockResolvedValueOnce({});
let pullRequestMock: PullRequest;
let octokit: Octokit;

describe('processComment', () => {
  beforeEach(async () => {
    octokit = await ContainerHelper.registerDefaults();

    if (useMocks) {
      createForIssueComment.mockClear();
      createWorkflowDispatch.mockClear();

      pullRequestMock = {
        number: pullNumber,
        labels: [],
        head: {
          ref: 'add-service-pr'
        }
      } as any;

      Object.assign(octokit.rest.repos, {
        getContent: jest.fn().mockResolvedValue({
          data: {
            content: ''
          }
        }),
        createOrUpdateFileContents: jest.fn().mockResolvedValue({
          data: {
            content: ''
          }
        })
      });

      Object.assign(octokit.rest.pulls, {
        get: jest.fn().mockResolvedValue({
          data: pullRequestMock
        }),
        list: jest.fn().mockResolvedValue({
          data: []
        }),
        listCommits: jest.fn().mockResolvedValueOnce({
          data: [
            {
              commit: {
                message: 'This is a test commit message.'
              }
            }
          ]
        })
      });

      const zipFilePath = path.resolve(path.join(__dirname, '../mocks/build-details.zip'));
      const zipData = new AdmZip(zipFilePath).toBuffer();

      Object.assign(octokit.rest.actions, {
        createWorkflowDispatch,
        getWorkflowRun: jest.fn().mockResolvedValueOnce({
          data: {
            title: 'My new app feature',
            path: 'deploy-app-test-build.yml',
            html_url: 'https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/10014663490'
          }
        }),
        listWorkflowRunArtifacts: jest.fn().mockResolvedValueOnce({
          data: {
            artifacts: [
              {
                id: 654321,
                name: 'build-details'
              }
            ]
          }
        }),
        downloadArtifact: jest.fn().mockResolvedValueOnce({
          data: zipData
        })
      });

      Object.assign(octokit.rest.reactions, {
        createForIssueComment
      });

      Object.assign(octokit.rest.issues, {
        setLabels: jest.fn().mockResolvedValueOnce({}),
        createComment: jest.fn().mockResolvedValueOnce({})
      });

      Object.assign(octokit.rest.checks, {
        listForRef: jest.fn().mockResolvedValue({
          data: {
            check_runs: [
              {
                name: 'Build helm chart',
                details_url:
                  'https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/10014663490/job/27684771116',
                status: 'completed'
              }
            ]
          }
        })
      });
    }
  });

  it('processes comment of /deploy', async () => {
    pullRequestMock.labels = [
      {
        name: 'prod'
      },
      {
        name: 'prod-lock'
      }
    ] as any[];

    const manager = container.resolve(CommentCommandManager);
    await manager.processComment({
      id: commentId,
      body: '/deploy',
      issue_url: `https://github.com/greggbjensen/prdeploy-example-repo/pull/${pullNumber}`
    } as any);

    if (useMocks) {
      const commentParams = createForIssueComment.mock.calls[0][0] as CreateForIssueCommentParams;
      expect(commentParams).toBeTruthy();
      expect(commentParams.content).toBe('rocket');

      const workflowParams = createWorkflowDispatch.mock.calls[0][0] as CreateWorkflowDispatchParams;
      expect(workflowParams).toBeTruthy();
      expect(workflowParams.inputs.pull_number).toBe(pullNumber.toString());
      expect(workflowParams.inputs.environment).toBe('dev');
      expect(workflowParams.inputs.deploy_name).toBe('[dev] add-service-pr: This is a test commit message.');
    }
  });

  it('processes comment of /add single service', async () => {
    const manager = container.resolve(CommentCommandManager);

    await manager.processComment({
      id: commentId,
      body: '/add deploy-api-test',
      issue_url: `https://github.com/greggbjensen/prdeploy-example-repo/pull/${pullNumber}`
    } as any);
  }, 5000);

  it('processes comment of /add multiple services', async () => {
    const manager = container.resolve(CommentCommandManager);

    await manager.processComment({
      id: commentId,
      body: '/add deploy-api-test deploy-app-test',
      issue_url: `https://github.com/greggbjensen/prdeploy-example-repo/pull/${pullNumber}`
    } as any);
  }, 5000);

  it('fails comment of /add when configuration does not exist for service', async () => {
    const manager = container.resolve(CommentCommandManager);

    await manager.processComment({
      id: commentId,
      body: '/add deploy-bad-test',
      issue_url: `https://github.com/greggbjensen/prdeploy-example-repo/pull/${pullNumber}`
    } as any);
  }, 5000);
});
