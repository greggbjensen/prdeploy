import * as core from '@actions/core';
import { Octokit } from '@octokit/rest';
import { BuildInfo } from '../models/build-info';

/**
 * Marks the check run of the job executing this action as a PR Deploy build,
 * so PR Deploy can identify build checks by their output text.
 */
export class CheckService {
  /** Marker written to the check run output text that identifies a PR Deploy build. */
  static readonly BuildMarker = 'PR_DEPLOY_BUILD';

  /** Title shown on the check run output. */
  static readonly OutputTitle = 'Build details';

  /**
   * Creates the service.
   * @param _octokit Authenticated GitHub client; the token needs `actions: read` and `checks: write`.
   */
  constructor(private _octokit: Octokit) {}

  /**
   * Finds the check run of the current job and sets its output text to {@link CheckService.BuildMarker}.
   * @param build Build information used for the output summary and to locate the workflow run.
   * @param runAttempt Attempt number of the current workflow run.
   * @param runnerName Name of the runner executing the current job (`RUNNER_NAME`).
   * @returns The ID of the check run that was updated.
   * @throws Error when the current job's check run cannot be found.
   */
  async markBuild(build: BuildInfo, runAttempt: number, runnerName: string): Promise<number> {
    const checkRunId = await this.findCurrentCheckRunId(build, runAttempt, runnerName);
    core.info(`Marking check run ${checkRunId} as ${CheckService.BuildMarker}.`);

    await this._octokit.rest.checks.update({
      owner: build.owner,
      repo: build.repo,
      check_run_id: checkRunId,
      output: {
        title: CheckService.OutputTitle,
        summary: `Version: ${build.version}`,
        text: CheckService.BuildMarker
      }
    });

    return checkRunId;
  }

  /**
   * Locates the check run of the in-progress job that is running on the given runner.
   * A job's check run ID is taken from the end of its `check_run_url`.
   * @param build Build information holding the owner, repository, and workflow run ID.
   * @param runAttempt Attempt number of the current workflow run.
   * @param runnerName Name of the runner executing the current job.
   * @returns The ID of the current job's check run.
   * @throws Error when no in-progress job on this runner exists or its check run URL has no ID.
   */
  async findCurrentCheckRunId(build: BuildInfo, runAttempt: number, runnerName: string): Promise<number> {
    if (!runnerName) {
      throw new Error('RUNNER_NAME is not set; unable to find the check run for the current job.');
    }

    const jobs = await this._octokit.paginate(this._octokit.rest.actions.listJobsForWorkflowRunAttempt, {
      owner: build.owner,
      repo: build.repo,
      run_id: build.runId,
      attempt_number: runAttempt,
      per_page: 100
    });

    const job = jobs.find(j => j.status === 'in_progress' && j.runner_name === runnerName);
    if (!job) {
      throw new Error(
        `Unable to find an in-progress job on runner "${runnerName}" for run ${build.runId} attempt ${runAttempt}.`
      );
    }

    const checkRunId = CheckService.parseCheckRunId(job.check_run_url);
    if (!checkRunId) {
      throw new Error(`Unable to parse a check run ID from "${job.check_run_url}" for job "${job.name}".`);
    }

    return checkRunId;
  }

  /**
   * Extracts the numeric check run ID from the end of a check run API URL.
   * @param checkRunUrl URL such as `https://api.github.com/repos/o/r/check-runs/123`.
   * @returns The check run ID, or 0 when the URL does not end with a positive integer.
   */
  static parseCheckRunId(checkRunUrl: string | null | undefined): number {
    const match = /\/check-runs\/(\d+)\/?$/.exec(checkRunUrl ?? '');
    return match ? Number(match[1]) : 0;
  }
}
