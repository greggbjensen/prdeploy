import { describe, expect, jest, test } from '@jest/globals';
import { Octokit } from '@octokit/rest';
import { PullRequestService } from '../src/services/pull-request-service';

jest.mock('@actions/core');

/** A pull request as returned by the GitHub API. */
const apiPull = {
  number: 8,
  title: 'Add login',
  html_url: 'https://github.com/o/r/pull/8',
  body: 'Details',
  head: { ref: 'feature/login' },
  base: { ref: 'main' }
};

/** The pull request mapped for the build details. */
const mapped = {
  prNumber: 8,
  title: 'Add login',
  url: 'https://github.com/o/r/pull/8',
  body: 'Details',
  head: 'feature/login',
  base: 'main'
};

describe('PullRequestService', () => {
  test('get maps the pull request by number', async () => {
    const get = jest.fn(async (_request: unknown) => ({ data: apiPull }));
    const octokit = { rest: { pulls: { get } } } as unknown as Octokit;

    expect(await new PullRequestService(octokit).get('o', 'r', 8)).toEqual(mapped);
    expect(get).toHaveBeenCalledWith({ owner: 'o', repo: 'r', pull_number: 8 });
  });

  test('getByCommit maps the first pull request associated with the commit', async () => {
    const list = jest.fn(async (_request: unknown) => ({ data: [apiPull, { ...apiPull, number: 9 }] }));
    const octokit = { rest: { repos: { listPullRequestsAssociatedWithCommit: list } } } as unknown as Octokit;

    expect(await new PullRequestService(octokit).getByCommit('o', 'r', 'abc')).toEqual(mapped);
    expect(list).toHaveBeenCalledWith({ owner: 'o', repo: 'r', commit_sha: 'abc' });
  });

  test('getByCommit returns null when the commit has no pull request', async () => {
    const list = jest.fn(async (_request: unknown) => ({ data: [] }));
    const octokit = { rest: { repos: { listPullRequestsAssociatedWithCommit: list } } } as unknown as Octokit;

    expect(await new PullRequestService(octokit).getByCommit('o', 'r', 'abc')).toBeNull();
  });
});
