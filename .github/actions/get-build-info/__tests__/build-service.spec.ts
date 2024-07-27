import { expect } from '@jest/globals';
import { BuildService } from '../src/services/build-service';
import { Octokit } from '@octokit/rest';
import { describe } from 'node:test';

const auth = process.env.GITHUB_TOKEN;

describe('list', () => {
  it.skip('Lists builds and versions from a reference', async () => {
    const octokit = new Octokit({ auth });
    const checkService = new BuildService(octokit);
    const builds = await checkService.list(
      'greggbjensen',
      'prdeploy-example-repo',
      '/([^/]+?)-build.ya?ml',
      [5158739075, 5157116550]
    );

    expect(builds).not.toBeNull();
    expect(builds.length).toBeGreaterThan(0);

    const build = builds[0];
    expect(build.name).toBeTruthy();
    expect(build.version).toBeTruthy();
  });
});
