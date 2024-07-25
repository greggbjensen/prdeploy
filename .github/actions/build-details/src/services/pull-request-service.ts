import * as core from '@actions/core';
import { Octokit } from '@octokit/rest';
import { PullRequest } from '../models/pull-request';

export class PullRequestService {
  constructor(private _octokit: Octokit) {}

  async get(owner: string, repo: string, prNumber: number): Promise<PullRequest> {
    core.info(`Finding pull request: ${prNumber}`);
    const pullResponse = await this._octokit.rest.pulls.get({
      owner,
      repo,
      pull_number: prNumber
    });

    const pull = pullResponse.data;
    return pull
      ? {
          prNumber,
          title: pull.title,
          url: pull.html_url,
          body: pull.body,
          head: pull.head.ref,
          base: pull.base.ref
        }
      : (null as unknown as PullRequest);
  }

  async getByCommit(owner: string, repo: string, commit: string): Promise<PullRequest> {
    core.info(`Finding pull request by commit: ${commit}`);
    const pullResponse = await this._octokit.rest.repos.listPullRequestsAssociatedWithCommit({
      owner,
      repo,
      commit_sha: commit
    });

    const pull = pullResponse.data ? pullResponse.data[0] : null;
    return pull
      ? {
          prNumber: pull.number,
          title: pull.title,
          url: pull.html_url,
          body: pull.body,
          head: pull.head.ref,
          base: pull.base.ref
        }
      : (null as unknown as PullRequest);
  }
}
