import * as core from '@actions/core';
import { Octokit } from '@octokit/rest';
import { PullRequest } from '../models';

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
      : null;
  }
}
