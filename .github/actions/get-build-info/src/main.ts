import * as core from '@actions/core';
import * as github from '@actions/github';
import { BuildService, PullRequestService } from './services';
import { Octokit } from '@octokit/rest';
import { SummaryWriter } from './summary-writer';
import _ from 'lodash';

const getRunIds = (idsValue: string): number[] => {
  return idsValue ? idsValue.split(',').map(r => parseInt(r.trim(), 10)) : [];
};

async function run(): Promise<void> {
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
    const workflowRegex: string = core.getInput('workflow_regex', {
      required: true
    });

    const allRunIds = _.uniq(deployRunIds.concat(syncRunIds));

    const octokit = github.getOctokit(token) as unknown as Octokit;
    const buildService = new BuildService(octokit);
    const owner = github.context.repo.owner;
    const repo = github.context.repo.repo;
    const builds = await buildService.list(owner, repo, workflowRegex, allRunIds);

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
      core.setFailed(error.message);
    }
  }
}

// eslint-disable-next-line @typescript-eslint/no-floating-promises
run();
