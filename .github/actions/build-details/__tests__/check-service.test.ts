import { beforeEach, describe, expect, jest, test } from '@jest/globals';
import { Octokit } from '@octokit/rest';
import { CheckService } from '../src/services/check-service';
import { BuildInfo } from '../src/models/build-info';

/** Shape of a workflow job returned by the mocked jobs listing; only the fields CheckService reads. */
interface MockJob {
  name: string;
  status: string;
  runner_name: string | null;
  check_run_url: string;
}

/** Build information shared by the tests. */
const build: BuildInfo = {
  runId: 1234,
  owner: 'greggbjensen',
  repo: 'prdeploy-example-repo',
  version: '1.0.0',
  base: 'main',
  head: 'SCRUM-44526-my-feature',
  commit: '123abc',
  eventName: 'push'
};

const listJobs = jest.fn();
const update = jest.fn<(params: unknown) => Promise<unknown>>();
const paginate = jest.fn<(method: unknown, params: unknown) => Promise<MockJob[]>>();

/**
 * Creates a mocked Octokit whose paginated jobs listing returns the given jobs.
 * @param jobs Jobs to return from `listJobsForWorkflowRunAttempt`.
 * @returns Octokit stand-in exposing only the members CheckService uses.
 */
function createOctokit(jobs: MockJob[]): Octokit {
  paginate.mockResolvedValue(jobs);
  update.mockResolvedValue({});
  return {
    paginate,
    rest: {
      actions: { listJobsForWorkflowRunAttempt: listJobs },
      checks: { update }
    }
  } as unknown as Octokit;
}

/**
 * Builds a job on the given runner with a check run URL ending in the given ID.
 * @param runnerName Runner the job runs on.
 * @param checkRunId Check run ID placed at the end of `check_run_url`.
 * @param status Job status; defaults to in progress.
 * @returns The mocked job.
 */
function job(runnerName: string | null, checkRunId: number, status = 'in_progress'): MockJob {
  return {
    name: `job-${checkRunId}`,
    status,
    runner_name: runnerName,
    check_run_url: `https://api.github.com/repos/greggbjensen/prdeploy-example-repo/check-runs/${checkRunId}`
  };
}

beforeEach(() => {
  jest.clearAllMocks();
});

describe('CheckService.markBuild', () => {
  test('sets the current job check run output text to PR_DEPLOY_BUILD', async () => {
    const service = new CheckService(createOctokit([job('runner-a', 111), job('runner-b', 222)]));

    const checkRunId = await service.markBuild(build, 2, 'runner-b');

    expect(checkRunId).toBe(222);
    expect(paginate).toHaveBeenCalledWith(listJobs, {
      owner: build.owner,
      repo: build.repo,
      run_id: build.runId,
      attempt_number: 2,
      per_page: 100
    });
    expect(update).toHaveBeenCalledTimes(1);
    expect(update).toHaveBeenCalledWith({
      owner: build.owner,
      repo: build.repo,
      check_run_id: 222,
      output: {
        title: 'Build details',
        summary: 'Version: 1.0.0',
        text: 'PR_DEPLOY_BUILD'
      }
    });
  });

  test('ignores completed jobs that ran on the same runner', async () => {
    const service = new CheckService(createOctokit([job('runner-a', 111, 'completed'), job('runner-a', 333)]));

    await expect(service.markBuild(build, 1, 'runner-a')).resolves.toBe(333);
  });

  test('throws without updating when no in-progress job runs on this runner', async () => {
    const service = new CheckService(createOctokit([job('runner-a', 111, 'completed'), job(null, 222)]));

    await expect(service.markBuild(build, 1, 'runner-a')).rejects.toThrow(
      'Unable to find an in-progress job on runner "runner-a" for run 1234 attempt 1.'
    );
    expect(update).not.toHaveBeenCalled();
  });

  test('throws without listing jobs when the runner name is empty', async () => {
    const service = new CheckService(createOctokit([job('runner-a', 111)]));

    await expect(service.markBuild(build, 1, '')).rejects.toThrow('RUNNER_NAME is not set');
    expect(paginate).not.toHaveBeenCalled();
    expect(update).not.toHaveBeenCalled();
  });

  test('throws when the job check run URL has no ID', async () => {
    const badJob = { ...job('runner-a', 111), check_run_url: 'https://api.github.com/repos/o/r/check-runs/' };
    const service = new CheckService(createOctokit([badJob]));

    await expect(service.markBuild(build, 1, 'runner-a')).rejects.toThrow('Unable to parse a check run ID');
    expect(update).not.toHaveBeenCalled();
  });
});

describe('CheckService.parseCheckRunId', () => {
  test.each([
    ['https://api.github.com/repos/o/r/check-runs/123', 123],
    ['https://api.github.com/repos/o/r/check-runs/123/', 123],
    ['https://api.github.com/repos/o/r/check-runs/', 0],
    ['https://api.github.com/repos/o/r/check-runs/abc', 0],
    ['', 0],
    [null, 0],
    [undefined, 0]
  ])('parses %p as %p', (url, expected) => {
    expect(CheckService.parseCheckRunId(url)).toBe(expected);
  });
});
