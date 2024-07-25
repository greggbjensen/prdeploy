import { SummaryWriter } from '../src/summary-writer';
import { DetailsUploader } from '../src/details-uploader';
import * as process from 'process';
import * as cp from 'child_process';
import * as path from 'path';
import { expect, test } from '@jest/globals';
import * as fs from 'fs';
import { ArtifactClient } from '@actions/artifact';

beforeEach(() => {
  const stepSummaryFile = path.join(__dirname, 'github-step-summary.md');
  if (fs.existsSync(stepSummaryFile)) {
    fs.unlinkSync(stepSummaryFile);
  }
  fs.writeFileSync(stepSummaryFile, '');

  Object.assign(process.env, {
    GITHUB_STEP_SUMMARY: stepSummaryFile,
    ACTIONS_RUNTIME_TOKEN: 'abc'
  });
});

test('writes summary', async () => {
  const writer = new SummaryWriter();
  await writer.write({
    runId: 1234,
    owner: 'greggbjensen',
    repo: 'prdeploy-example-repo',
    version: '1.0.0',
    base: 'main',
    head: 'SCRUM-44526-my-feature',
    commit: '123abc',
    eventName: 'push'
  });
});

test.skip('uploads details', async () => {
  // TODO GBJ: Need to mock ArtifactClient.
  const uploader = new DetailsUploader({} as ArtifactClient);
  await uploader.upload({
    runId: 1234,
    owner: 'greggbjensen',
    repo: 'prdeploy-example-repo',
    version: '1.0.0',
    base: 'main',
    head: 'SCRUM-44526-my-feature',
    commit: '123abc',
    eventName: 'push'
  });
});

// shows how the runner will run a javascript action with env / stdout protocol
test.skip('test runs', () => {
  process.env['INPUT_VERSION'] = '1.0.0';
  const np = process.execPath;
  const ip = path.join(__dirname, '..', 'lib', 'main.js');
  const options: cp.ExecFileSyncOptions = {
    env: process.env
  };
  console.log(cp.execFileSync(np, [ip], options).toString());
});
