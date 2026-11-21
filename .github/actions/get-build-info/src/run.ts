import * as core from '@actions/core';
import * as github from '@actions/github';
import { BuildService, PullRequestService, ServiceNameService } from './services';
import { Octokit } from '@octokit/rest';
import { SummaryWriter } from './summary-writer';
import _ from 'lodash';

/**
 * Parses a comma-separated list of workflow run IDs, ignoring blank and non-numeric entries so a trailing comma or
 * stray space never produces a `NaN` run ID.
 */
export const getRunIds = (idsValue: string): number[] => {
  return (idsValue ?? '')
    .split(',')
    .map(r => r.trim())
    .filter(r => /^\d+$/.test(r))
    .map(r => parseInt(r, 10));
};

/** Permissions the workflow token needs to read build runs, their artifacts, and the pull request. */
export const REQUIRED_PERMISSIONS = 'actions: read, contents: read, and pull-requests: read';

/**
 * Describes an error for the action failure message. GitHub reports a token missing a permission as
 * "Resource not accessible by integration", so that message gets the permissions the job needs.
 */
export const describeError = (error: Error): string => {
  const status = (error as Error & { status?: number }).status;
  if (status === 403 && /Resource not accessible by integration/i.test(error.message)) {
    return (
      `${error.message}. The job's GITHUB_TOKEN needs ${REQUIRED_PERMISSIONS} permissions; ` +
      'add them under the job running this action, since a workflow-level permissions block applies to every job.'
    );
  }

  return error.message;
};

/**
 * Lists the builds for the deploy and sync run IDs, writes the job summary, and sets the action outputs:
 * `builds_json`, `builds_matrix_json`, `builds_count`, and `pull_branch`. Any error fails the action.
 */
export async function run(): Promise<void> {
  try {
    const token: string = core.getInput('token', { required: true });
    const pullNumberValue: string = core.getInput('pull_number', {
      required: true
    });
    const pullNumber = parseInt(pullNumberValue, 10);
    const deployRunIdsValue: string = core.getInput('deploy_run_ids', {
      required: false
    });
    const deployRunIds = getRunIds(deployRunIdsValue);
    const syncRunIdsValue: string = core.getInput('sync_run_ids', {
      required: false
    });
    const syncRunIds = getRunIds(syncRunIdsValue);

    const allRunIds = _.uniq(deployRunIds.concat(syncRunIds));

    const octokit = github.getOctokit(token) as unknown as Octokit;
    const owner = github.context.repo.owner;
    const repo = github.context.repo.repo;
    const apiUrl: string = core.getInput('api_url', { required: false });
    const serviceNames = await new ServiceNameService(apiUrl, token).list(owner, repo);
    const buildService = new BuildService(octokit, serviceNames);
    const builds = await buildService.list(owner, repo, allRunIds);

    core.info('Builds found:');
    for (const build of builds) {
      core.info(`  ${build.name} (${build.version})`);
    }

    const deployBuilds = builds.filter(b => deployRunIds.includes(b.runId));
    core.info('Deploy builds:');
    for (const build of deployBuilds) {
      core.info(`  ${build.name} (${build.version})`);
    }

    const pullRequestService = new PullRequestService(octokit);
    const pullRequest = await pullRequestService.get(owner, repo, pullNumber);

    const writer = new SummaryWriter();
    await writer.write(pullRequest, deployBuilds);

    // Output cannot be an object as far as I have found so we need to set it as JSON.
    const buildSummaries = builds.map(({ runId, name, version, workflow }) => ({
      runId,
      name,
      version,
      workflow
    }));
    const buildsJson = JSON.stringify(buildSummaries);
    const buildsMatrixJson = JSON.stringify(builds.map(b => b.name));

    core.setOutput('builds_json', buildsJson);
    core.setOutput('builds_matrix_json', buildsMatrixJson);
    core.setOutput('builds_count', builds.length);
    core.setOutput('pull_branch', pullRequest.head);
  } catch (error) {
    if (error instanceof Error) {
      core.setFailed(describeError(error));
    }
  }
}
