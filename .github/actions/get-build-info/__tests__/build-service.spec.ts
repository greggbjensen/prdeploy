import { beforeEach, describe, expect, it, jest } from '@jest/globals';
import * as core from '@actions/core';
import AdmZip from 'adm-zip';
import { Octokit } from '@octokit/rest';
import { BuildService } from '../src/services/build-service';

jest.mock('@actions/core');

/** A workflow run as returned by the GitHub API. */
interface MockRun {
  path: string;
  display_title: string;
  html_url: string;
}

/** Creates a zipped build-details artifact, as uploaded by the build-details action. */
function artifactZip(files: Record<string, string>): ArrayBuffer {
  const zip = new AdmZip();
  for (const [name, content] of Object.entries(files)) {
    zip.addFile(name, Buffer.from(content, 'utf8'));
  }
  const buffer = zip.toBuffer();
  return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength) as ArrayBuffer;
}

/** Creates an Octokit whose actions API serves the given runs, artifact names, and artifact zip. */
function createOctokit(runs: Record<number, MockRun>, artifactNames: string[], zip: ArrayBuffer) {
  const actions = {
    getWorkflowRun: jest.fn(async ({ run_id }: { run_id: number }) => ({ data: runs[run_id] })),
    listWorkflowRunArtifacts: jest.fn(async (_request: unknown) => ({
      data: { artifacts: artifactNames.map((name, i) => ({ id: 100 + i, name })) }
    })),
    downloadArtifact: jest.fn(async (_request: unknown) => ({ data: zip }))
  };
  return { octokit: { rest: { actions } } as unknown as Octokit, actions };
}

const apiRun: MockRun = {
  path: '.github/workflows/api-build.yaml',
  display_title: 'Add login',
  html_url: 'https://github.com/o/r/actions/runs/1'
};

describe('BuildService.list', () => {
  beforeEach(() => jest.mocked(core.warning).mockClear());

  it('names each build from the workflow file and reads its version from the build-details artifact', async () => {
    const zip = artifactZip({ 'build-details.json': JSON.stringify({ version: '2026.11.20.1' }) });
    const { octokit, actions } = createOctokit({ 1: apiRun }, ['other', 'build-details'], zip);

    const builds = await new BuildService(octokit).list('o', 'r', '/([^/]+?)-build.ya?ml', [1]);

    expect(builds).toEqual([
      {
        runId: 1,
        name: 'api',
        version: '2026.11.20.1',
        title: 'Add login',
        url: 'https://github.com/o/r/actions/runs/1',
        workflow: 'api-build.yaml'
      }
    ]);
    expect(actions.downloadArtifact).toHaveBeenCalledWith({
      owner: 'o',
      repo: 'r',
      artifact_id: 101,
      archive_format: 'zip'
    });
  });

  it('falls back to the workflow path and warns when the pattern does not match', async () => {
    const zip = artifactZip({ 'build-details.json': JSON.stringify({ version: '1.0.0' }) });
    const run = { ...apiRun, path: '.github/workflows/combined-deploy.yaml' };
    const { octokit } = createOctokit({ 2: run }, ['build-details'], zip);

    const [build] = await new BuildService(octokit).list('o', 'r', '/([^/]+?)-build.ya?ml', [2]);

    expect(build.name).toBe('.github/workflows/combined-deploy.yaml');
    expect(core.warning).toHaveBeenCalledWith(expect.stringContaining('did not match workflow path'));
  });

  it('matches the workflow pattern case-insensitively', async () => {
    const zip = artifactZip({ 'build-details.json': JSON.stringify({ version: '1.0.0' }) });
    const run = { ...apiRun, path: '.github/workflows/App-Build.YML' };
    const { octokit } = createOctokit({ 3: run }, ['build-details'], zip);

    const [build] = await new BuildService(octokit).list('o', 'r', '/([^/]+?)-build.ya?ml', [3]);

    expect(build.name).toBe('App');
  });

  it('returns an empty version and warns when the build-details artifact is missing', async () => {
    const { octokit, actions } = createOctokit({ 1: apiRun }, ['coverage'], new ArrayBuffer(0));

    const [build] = await new BuildService(octokit).list('o', 'r', '/([^/]+?)-build.ya?ml', [1]);

    expect(build.version).toBe('');
    expect(actions.downloadArtifact).not.toHaveBeenCalled();
    expect(core.warning).toHaveBeenCalledWith('Artifact build-details not found on triggering workflow.');
  });

  it('returns an empty version when the artifact has no build-details.json', async () => {
    const zip = artifactZip({ 'other.json': '{}' });
    const { octokit } = createOctokit({ 1: apiRun }, ['build-details'], zip);

    const [build] = await new BuildService(octokit).list('o', 'r', '/([^/]+?)-build.ya?ml', [1]);

    expect(build.version).toBe('');
    expect(core.warning).toHaveBeenCalledWith('Artifact build-details did not contain build-details.json.');
  });

  it('returns an empty version when build-details.json is not valid JSON', async () => {
    const zip = artifactZip({ 'build-details.json': '{ not json' });
    const { octokit } = createOctokit({ 1: apiRun }, ['build-details'], zip);

    const [build] = await new BuildService(octokit).list('o', 'r', '/([^/]+?)-build.ya?ml', [1]);

    expect(build.version).toBe('');
  });

  it('returns no builds for no run IDs', async () => {
    const { octokit, actions } = createOctokit({}, [], new ArrayBuffer(0));

    expect(await new BuildService(octokit).list('o', 'r', 'x', [])).toEqual([]);
    expect(actions.getWorkflowRun).not.toHaveBeenCalled();
  });
});
