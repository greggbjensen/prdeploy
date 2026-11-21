import { beforeEach, describe, expect, it, jest } from '@jest/globals';
import * as core from '@actions/core';
import * as github from '@actions/github';
import AdmZip from 'adm-zip';
import { getRunIds, run } from '../src/run';

jest.mock('@actions/core');
jest.mock('@actions/github', () => ({
  context: { repo: { owner: 'o', repo: 'r' } },
  getOctokit: jest.fn()
}));

/** Workflow paths by run ID, as GitHub would return them. */
const runs: Record<number, string> = {
  1: '.github/workflows/api-build.yaml',
  2: '.github/workflows/web-build.yml',
  3: '.github/workflows/worker-build.yaml'
};

/** Creates the zipped build-details artifact for a run. */
function artifactFor(runId: number): ArrayBuffer {
  const zip = new AdmZip();
  zip.addFile('build-details.json', Buffer.from(JSON.stringify({ version: `${runId}.0.0` })));
  const buffer = zip.toBuffer();
  return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength) as ArrayBuffer;
}

/** Octokit mock serving workflow runs, artifacts, and pull request 8. */
const octokit = {
  rest: {
    actions: {
      getWorkflowRun: jest.fn(async ({ run_id }: { run_id: number }) => {
        if (!runs[run_id]) throw new Error(`Run ${run_id} not found`);
        return { data: { path: runs[run_id], display_title: '', html_url: `https://x/${run_id}` } };
      }),
      listWorkflowRunArtifacts: jest.fn(async ({ run_id }: { run_id: number }) => ({
        data: { artifacts: [{ id: run_id, name: 'build-details' }] }
      })),
      downloadArtifact: jest.fn(async ({ artifact_id }: { artifact_id: number }) => ({
        data: artifactFor(artifact_id)
      }))
    },
    pulls: {
      get: jest.fn(async (_request: unknown) => ({
        data: { title: 't', html_url: 'u', body: '', head: { ref: 'feature/x' }, base: { ref: 'main' } }
      }))
    }
  }
};

/** Sets the action inputs the runner would pass. */
function setInputs(inputs: Record<string, string>) {
  jest.mocked(core.getInput).mockImplementation((name: string) => inputs[name] ?? '');
}

/** Returns the value of an output set by the action. */
function output(name: string): unknown {
  return jest.mocked(core.setOutput).mock.calls.find(([n]) => n === name)?.[1];
}

describe('getRunIds', () => {
  it.each([
    ['1, 2,3', [1, 2, 3]],
    ['', []],
    ['1,,2,', [1, 2]],
    [' 7 ', [7]],
    ['abc,4', [4]]
  ])('parses %p as %p', (value, expected) => {
    expect(getRunIds(value)).toEqual(expected);
  });
});

describe('run', () => {
  beforeEach(() => {
    jest.mocked(github.getOctokit).mockReturnValue(octokit as never);
  });

  it('outputs every build and summarizes only the deploy builds', async () => {
    setInputs({
      token: 't',
      pull_number: '8',
      deploy_run_ids: '1,2',
      sync_run_ids: '2,3',
      workflow_regex: '/([^/]+?)-build.ya?ml'
    });

    await run();

    expect(core.setFailed).not.toHaveBeenCalled();
    expect(JSON.parse(output('builds_json') as string)).toEqual([
      { runId: 1, name: 'api', version: '1.0.0', workflow: 'api-build.yaml' },
      { runId: 2, name: 'web', version: '2.0.0', workflow: 'web-build.yml' },
      { runId: 3, name: 'worker', version: '3.0.0', workflow: 'worker-build.yaml' }
    ]);
    expect(output('builds_matrix_json')).toBe('["api","web","worker"]');
    expect(output('builds_count')).toBe(3);
    expect(output('pull_branch')).toBe('feature/x');
    expect(octokit.rest.pulls.get).toHaveBeenCalledWith({ owner: 'o', repo: 'r', pull_number: 8 });

    // Run 2 is both deployed and synced, but is only fetched once.
    expect(octokit.rest.actions.getWorkflowRun).toHaveBeenCalledTimes(3);
    const summaryRows = jest
      .mocked(core.summary.addRaw)
      .mock.calls.map(([text]) => text)
      .join('');
    expect(summaryRows).toContain('| api | 1.0.0 |');
    expect(summaryRows).not.toContain('| worker |');
  });

  it('fails the action when a run cannot be read', async () => {
    setInputs({ token: 't', pull_number: '8', deploy_run_ids: '99', workflow_regex: 'x' });

    await run();

    expect(core.setFailed).toHaveBeenCalledWith('Run 99 not found');
    expect(core.setOutput).not.toHaveBeenCalled();
  });
});
