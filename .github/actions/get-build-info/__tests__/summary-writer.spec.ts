import { beforeAll, beforeEach, describe, expect, it } from '@jest/globals';
import * as core from '@actions/core';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { SummaryWriter } from '../src/summary-writer';
import { Build, PullRequest } from '../src/models';

const pull: PullRequest = {
  prNumber: 8,
  title: 'Add login',
  url: 'https://github.com/o/r/pull/8',
  body: 'Adds the login page.',
  head: 'feature/login',
  base: 'main'
};

/** Creates a build for the summary table. */
const build = (name: string, runId: number): Build => ({
  runId,
  name,
  version: `${runId}.0.0`,
  title: '',
  url: `https://github.com/o/r/actions/runs/${runId}`,
  workflow: `${name}-build.yaml`
});

describe('SummaryWriter.write', () => {
  // core.summary caches the summary path after its first write, so every test shares one file.
  const summaryFile = path.join(fs.mkdtempSync(path.join(os.tmpdir(), 'get-build-info-')), 'summary.md');

  beforeAll(() => {
    process.env.GITHUB_STEP_SUMMARY = summaryFile;
  });

  beforeEach(() => {
    fs.writeFileSync(summaryFile, '');
    core.summary.emptyBuffer();
  });

  it('lists deploy builds in name order and links each run, followed by the pull request', async () => {
    await new SummaryWriter().write(pull, [build('web', 2), build('api', 1)]);

    const summary = fs.readFileSync(summaryFile, 'utf8');
    expect(summary).toContain('<h2>Deploy Services</h2>');
    expect(summary.indexOf('| api | 1.0.0 | [1](https://github.com/o/r/actions/runs/1) |')).toBeLessThan(
      summary.indexOf('| web | 2.0.0 | [2](https://github.com/o/r/actions/runs/2) |')
    );
    expect(summary).toContain('## [#8](https://github.com/o/r/pull/8) Add login');
    expect(summary).toContain('Adds the login page.');
  });

  it('says no builds were found when there are none', async () => {
    await new SummaryWriter().write({ ...pull, body: '' }, []);

    const summary = fs.readFileSync(summaryFile, 'utf8');
    expect(summary).toContain('*No builds found*');
    expect(summary).not.toContain('| Name');
  });
});
