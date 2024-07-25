import * as core from '@actions/core';
import * as github from '@actions/github';
import { BuildInfo } from './models/build-info';
import { DetailsUploader } from './details-uploader';
import { Octokit } from '@octokit/rest';
import { PullRequestService } from './services/pull-request-service';
import { SummaryWriter } from './summary-writer';
import artifact from '@actions/artifact';

async function run(): Promise<void> {
  try {
    const version: string = core.getInput('version', { required: true });
    const token = core.getInput('token', { required: true });
    const branchRegex = /^refs\/heads\//i;

    const prNumber = github.context.payload.pull_request?.number;
    const buildInfo: BuildInfo = {
      runId: github.context.runId,
      owner: github.context.repo.owner,
      repo: github.context.repo.repo,
      eventName: github.context.eventName,
      head: '',
      base: '',
      commit: '',
      version
    };

    const octokit = github.getOctokit(token) as unknown as Octokit;
    const pullRequestService = new PullRequestService(octokit);

    // Get branch name.
    buildInfo.base = github.context.ref.replace(branchRegex, '');
    if (buildInfo.base.includes('/tags/')) {
      const branches = await octokit.rest.repos.listBranchesForHeadCommit({
        owner: github.context.repo.owner,
        repo: github.context.repo.repo,
        commit_sha: github.context.sha
      });
      if (branches?.data && branches.data.length > 0) {
        // Find the first branch that matches the tag, or default to the first one.
        const parts = buildInfo.base.split('/');
        const tag = parts[parts.length - 1];
        const matchingBranch = branches.data.find(d => d.name.includes(tag));
        if (matchingBranch) {
          buildInfo.base = matchingBranch.name;
        } else {
          buildInfo.base = branches.data[0].name;
        }
      }
    }

    buildInfo.commit = github.context.sha;
    if (prNumber) {
      buildInfo.pullRequest = await pullRequestService.get(buildInfo.owner, buildInfo.repo, prNumber);
    } else {
      buildInfo.pullRequest = await pullRequestService.getByCommit(buildInfo.owner, buildInfo.repo, buildInfo.commit);
    }

    if (buildInfo.pullRequest) {
      buildInfo.head = buildInfo.pullRequest.head;
    }

    const writer = new SummaryWriter();
    const uploader = new DetailsUploader(artifact);

    await Promise.all([uploader.upload(buildInfo), writer.write(buildInfo)]);
  } catch (error) {
    if (error instanceof Error) core.setFailed(error.message);
  }
}

// eslint-disable-next-line @typescript-eslint/no-floating-promises
run();
