import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

/** Runner context the action reads; tests change it before each run. */
const context = {
  runId: 1234,
  runAttempt: 1,
  eventName: 'pull_request',
  ref: 'refs/heads/main',
  sha: 'abc1234def',
  repo: { owner: 'o', repo: 'r' },
  payload: {} as { pull_request?: { number: number } }
};

/** GitHub API calls the action makes. */
const api = {
  pullsGet: jest.fn(async (_request: unknown) => ({ data: apiPull(8, 'feature/login') })),
  pullsForCommit: jest.fn(async (_request: unknown) => ({ data: [apiPull(9, 'feature/merged')] })),
  branchesForCommit: jest.fn(async (_request: unknown) => ({ data: [] as { name: string }[] })),
  checksUpdate: jest.fn(async (_request: unknown) => ({})),
  paginate: jest.fn(async (_method: unknown, _request: unknown) => [
    {
      name: 'build',
      status: 'in_progress',
      runner_name: 'runner-1',
      check_run_url: 'https://api.github.com/repos/o/r/check-runs/555'
    }
  ])
};
const uploadArtifact = jest.fn(async (..._args: unknown[]) => ({ id: 1, size: 1 }));

jest.mock('@actions/github', () => ({
  context,
  getOctokit: () => ({
    paginate: api.paginate,
    rest: {
      pulls: { get: api.pullsGet },
      repos: {
        listPullRequestsAssociatedWithCommit: api.pullsForCommit,
        listBranchesForHeadCommit: api.branchesForCommit
      },
      actions: { listJobsForWorkflowRunAttempt: jest.fn() },
      checks: { update: api.checksUpdate }
    }
  })
}));
jest.mock('@actions/artifact', () => ({ __esModule: true, default: { uploadArtifact } }));
jest.mock('@actions/core', () => {
  const actual = jest.requireActual<typeof import('@actions/core')>('@actions/core');
  return { ...actual, info: jest.fn(), setFailed: jest.fn(), getInput: jest.fn() };
});

import * as core from '@actions/core';
import { run } from '../src/run';

/** Creates a pull request as returned by the GitHub API. */
function apiPull(number: number, head: string) {
  return {
    number,
    title: `PR ${number}`,
    html_url: `https://x/pull/${number}`,
    body: '',
    head: { ref: head },
    base: { ref: 'main' }
  };
}

/** Reads the build-details.json the action wrote. */
function writtenDetails(dir: string) {
  return JSON.parse(fs.readFileSync(path.join(dir, 'build-details.json'), 'utf8'));
}

describe('run', () => {
  let workDir: string;
  let originalCwd: string;

  beforeEach(() => {
    jest.clearAllMocks();
    originalCwd = process.cwd();
    workDir = fs.realpathSync(fs.mkdtempSync(path.join(os.tmpdir(), 'build-details-run-')));
    process.chdir(workDir);
    fs.writeFileSync(path.join(workDir, 'summary.md'), '');
    process.env.GITHUB_STEP_SUMMARY = path.join(workDir, 'summary.md');
    process.env.RUNNER_NAME = 'runner-1';
    core.summary.emptyBuffer();
    jest.mocked(core.getInput).mockImplementation(name => ({ version: '2026.11.20.1', token: 't' })[name] ?? '');
    Object.assign(context, { ref: 'refs/heads/main', payload: { pull_request: { number: 8 } } });
  });

  afterEach(() => {
    process.chdir(originalCwd);
    delete process.env.RUNNER_NAME;
  });

  test('records the pull request build, uploads it, and marks the job check run', async () => {
    await run();

    expect(core.setFailed).not.toHaveBeenCalled();
    expect(api.pullsGet).toHaveBeenCalledWith({ owner: 'o', repo: 'r', pull_number: 8 });
    expect(writtenDetails(workDir)).toMatchObject({
      runId: 1234,
      version: '2026.11.20.1',
      head: 'feature/login',
      base: 'main',
      commit: 'abc1234def',
      pullRequest: 8
    });
    expect(uploadArtifact).toHaveBeenCalledWith(
      'build-details',
      [path.join(workDir, 'build-details.json')],
      workDir,
      {}
    );
    expect(api.checksUpdate).toHaveBeenCalledWith(expect.objectContaining({ check_run_id: 555 }));
  });

  test('finds the pull request by commit for push builds', async () => {
    Object.assign(context, { payload: {} });

    await run();

    expect(api.pullsGet).not.toHaveBeenCalled();
    expect(api.pullsForCommit).toHaveBeenCalledWith({ owner: 'o', repo: 'r', commit_sha: 'abc1234def' });
    expect(writtenDetails(workDir)).toMatchObject({ pullRequest: 9, head: 'feature/merged' });
  });

  test('uses the branch matching the tag for tag builds', async () => {
    Object.assign(context, { ref: 'refs/tags/release-2', payload: {} });
    api.branchesForCommit.mockResolvedValueOnce({ data: [{ name: 'main' }, { name: 'hotfix/release-2' }] });

    await run();

    expect(writtenDetails(workDir).base).toBe('hotfix/release-2');
  });

  test('falls back to the first branch when no branch matches the tag', async () => {
    Object.assign(context, { ref: 'refs/tags/v1', payload: {} });
    api.branchesForCommit.mockResolvedValueOnce({ data: [{ name: 'main' }, { name: 'develop' }] });

    await run();

    expect(writtenDetails(workDir).base).toBe('main');
  });

  test('fails the action when the job check run cannot be found', async () => {
    delete process.env.RUNNER_NAME;

    await run();

    expect(core.setFailed).toHaveBeenCalledWith(expect.stringContaining('RUNNER_NAME is not set'));
  });
});
