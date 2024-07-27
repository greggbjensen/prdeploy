import * as core from '@actions/core';
import * as path from 'path';
import { Octokit } from '@octokit/rest';
import { Build, BuildDetails } from '../models';
import AdmZip from 'adm-zip';

export class BuildService {
  constructor(private _octokit: Octokit) {}

  async list(owner: string, repo: string, workflowPattern: string, runIds: number[]): Promise<Build[]> {
    core.info(`Listing builds for run IDs: ${runIds.join(', ')}`);

    const workflowRegex = new RegExp(workflowPattern, 'i');
    const builds = await Promise.all(runIds.map(async r => await this.extractBuild(owner, repo, workflowRegex, r)));
    return builds;
  }

  async extractBuild(owner: string, repo: string, workflowRegex: RegExp, runId: number): Promise<Build> {
    core.info(`Getting the workflow run with ID: ${runId}`);
    const result = await this._octokit.rest.actions.getWorkflowRun({
      owner,
      repo,
      run_id: runId
    });

    const workflowPath = result.data.path;
    const title = result.data.display_title;
    const url = result.data.html_url;
    const workflow = path.basename(workflowPath);
    let [, name] = workflowRegex.exec(workflowPath) ?? [null, ''];
    if (!name) {
      name = workflowPath;
      core.warning(`workflow_regex ${workflowRegex} did not match workflow path: ${workflowPath}`);
    }

    const version = await this.extractVersion(owner, repo, runId);

    return {
      runId,
      name,
      version,
      title,
      url,
      workflow
    };
  }

  async extractVersion(owner: string, repo: string, runId: number): Promise<string> {
    let version = '';
    try {
      // SourceRef: https://github.com/dawidd6/action-download-artifact/blob/master/main.js
      core.info(`Listing artifacts from Run ID: ${runId} `);
      const response = await this._octokit.rest.actions.listWorkflowRunArtifacts({
        owner,
        repo,
        run_id: runId
      });

      const detailsArtifact = response.data.artifacts.find(a => a.name === 'build-details');
      if (!detailsArtifact) {
        throw new Error('Artifact build-details not found on triggering workflow.');
      }

      const zip = await this._octokit.rest.actions.downloadArtifact({
        owner,
        repo,
        artifact_id: detailsArtifact.id,
        archive_format: 'zip'
      });

      const adm = new AdmZip(Buffer.from(zip.data as ArrayBuffer));
      const entry = adm.getEntry('build-details.json');
      if (!entry) {
        throw new Error('Artifact build-details did not contain build-details.json.');
      }

      const json = entry.getData().toString('utf8');
      const build: BuildDetails = JSON.parse(json);
      version = build.version;
    } catch (err: any) {
      core.warning('The build-details.json file does not exist, make sure your build uses the build-details action');
      core.warning(err.message);
    }

    return version;
  }
}
