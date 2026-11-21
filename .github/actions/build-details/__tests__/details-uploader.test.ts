import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { ArtifactClient } from '@actions/artifact';
import { DetailsUploader } from '../src/details-uploader';
import { BuildInfo } from '../src/models/build-info';

jest.mock('@actions/core');

/** Build information shared by the tests. */
const build: BuildInfo = {
  runId: 1234,
  owner: 'o',
  repo: 'r',
  version: '1.0.0',
  base: 'main',
  head: 'feature',
  commit: 'abc',
  eventName: 'pull_request'
};

describe('DetailsUploader.upload', () => {
  let workDir: string;
  let originalCwd: string;
  const uploadArtifact = jest.fn<ArtifactClient['uploadArtifact']>();
  const client = { uploadArtifact } as unknown as ArtifactClient;

  beforeEach(() => {
    originalCwd = process.cwd();
    workDir = fs.realpathSync(fs.mkdtempSync(path.join(os.tmpdir(), 'build-details-')));
    process.chdir(workDir);
    uploadArtifact.mockResolvedValue({ id: 1, size: 1 });
  });

  afterEach(() => {
    process.chdir(originalCwd);
  });

  test('writes build-details.json and uploads it as the build-details artifact', async () => {
    await new DetailsUploader(client).upload({
      ...build,
      pullRequest: { prNumber: 8, title: '', url: '', body: null, head: 'feature', base: 'main' }
    });

    const file = path.join(workDir, 'build-details.json');
    expect(JSON.parse(fs.readFileSync(file, 'utf8'))).toEqual({
      runId: 1234,
      version: '1.0.0',
      head: 'feature',
      base: 'main',
      commit: 'abc',
      pullRequest: 8,
      owner: 'o',
      repo: 'r',
      eventName: 'pull_request'
    });
    expect(uploadArtifact).toHaveBeenCalledWith('build-details', [file], workDir, {});
  });

  test('records pull request 0 when the build has no pull request', async () => {
    await new DetailsUploader(client).upload(build);

    const details = JSON.parse(fs.readFileSync(path.join(workDir, 'build-details.json'), 'utf8'));
    expect(details.pullRequest).toBe(0);
  });

  test('propagates upload failures so the action fails', async () => {
    uploadArtifact.mockRejectedValue(new Error('upload failed'));

    await expect(new DetailsUploader(client).upload(build)).rejects.toThrow('upload failed');
  });
});
