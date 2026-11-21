import { describe, expect, it, jest } from '@jest/globals';
import { Octokit } from '@octokit/rest';
import { PullRequestService } from '../src/services/pull-request-service';

jest.mock('@actions/core');

describe('PullRequestService.get', () => {
  it('maps the pull request fields used by the summary and outputs', async () => {
    const get = jest.fn(async (_request: unknown) => ({
      data: {
        title: 'Add login',
        html_url: 'https://github.com/o/r/pull/8',
        body: 'Details',
        head: { ref: 'feature/login' },
        base: { ref: 'main' }
      }
    }));
    const octokit = { rest: { pulls: { get } } } as unknown as Octokit;

    const pull = await new PullRequestService(octokit).get('o', 'r', 8);

    expect(get).toHaveBeenCalledWith({ owner: 'o', repo: 'r', pull_number: 8 });
    expect(pull).toEqual({
      prNumber: 8,
      title: 'Add login',
      url: 'https://github.com/o/r/pull/8',
      body: 'Details',
      head: 'feature/login',
      base: 'main'
    });
  });

  it('propagates GitHub errors so the action fails', async () => {
    const get = jest.fn(async (_request: unknown) => {
      throw new Error('Not Found');
    });
    const octokit = { rest: { pulls: { get } } } as unknown as Octokit;

    await expect(new PullRequestService(octokit).get('o', 'r', 404)).rejects.toThrow('Not Found');
  });
});
