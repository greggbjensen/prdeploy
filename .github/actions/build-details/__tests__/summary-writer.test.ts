import { beforeAll, beforeEach, describe, expect, test } from '@jest/globals';
import * as core from '@actions/core';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { SummaryWriter } from '../src/summary-writer';
import { BuildInfo } from '../src/models/build-info';

/** Build information shared by the tests. */
const build: BuildInfo = {
  runId: 1234,
  owner: 'greggbjensen',
  repo: 'prdeploy-example-repo',
  version: '1.0.0',
  base: 'main',
  head: 'SCRUM-44526-my-feature',
  commit: '123abc4567890',
  eventName: 'push'
};

describe('SummaryWriter.write', () => {
  // core.summary caches the summary path after its first write, so every test shares one file.
  const summaryFile = path.join(fs.mkdtempSync(path.join(os.tmpdir(), 'build-details-')), 'summary.md');

  beforeAll(() => {
    process.env.GITHUB_STEP_SUMMARY = summaryFile;
  });

  beforeEach(() => {
    fs.writeFileSync(summaryFile, '');
    core.summary.emptyBuffer();
  });

  test('writes the version, linked branches, short commit, and run ID', async () => {
    await new SummaryWriter().write(build);

    const summary = fs.readFileSync(summaryFile, 'utf8');
    const repoUrl = 'https://github.com/greggbjensen/prdeploy-example-repo';
    expect(summary).toContain('<h3>Build Details</h3>');
    expect(summary).toContain('| Version  | 1.0.0 |');
    expect(summary).toContain(`| Head     | [SCRUM-44526-my-feature](${repoUrl}/tree/SCRUM-44526-my-feature) |`);
    expect(summary).toContain(`| Base     | [main](${repoUrl}/tree/main) |`);
    expect(summary).toContain(`| Commit   | [123abc4](${repoUrl}/commit/123abc4567890) |`);
    expect(summary).toContain('| Run ID   | 1234   |');
    expect(summary).not.toContain('## [#');
  });

  test('leads with the pull request when there is one', async () => {
    await new SummaryWriter().write({
      ...build,
      pullRequest: { prNumber: 8, title: 'Add login', url: 'https://x/pull/8', body: 'Body text', head: 'h', base: 'b' }
    });

    const summary = fs.readFileSync(summaryFile, 'utf8');
    expect(summary.indexOf('## [#8](https://x/pull/8) Add login')).toBeLessThan(summary.indexOf('Build Details'));
    expect(summary).toContain('Body text');
  });

  test('omits the head row when the build has no head branch', async () => {
    await new SummaryWriter().write({ ...build, head: '' });

    expect(fs.readFileSync(summaryFile, 'utf8')).not.toContain('| Head ');
  });
});
