import * as core from '@actions/core';
import * as fs from 'fs/promises';
import * as process from 'process';
import { ArtifactClient, UploadArtifactOptions } from '@actions/artifact';
import { BuildDetails } from './models/build-details';
import { BuildInfo } from './models/build-info';
import path from 'path';

export class DetailsUploader {
  constructor(private _artifactClient: ArtifactClient) {}

  async upload(build: BuildInfo): Promise<void> {
    core.info('Uploading build details.');
    const jsonFile = path.join(process.cwd(), 'build-details.json');

    const buildDetails = {
      runId: build.runId,
      version: build.version,
      head: build.head,
      base: build.base,
      commit: build.commit,
      pullRequest: build.pullRequest ? build.pullRequest.prNumber : 0,
      owner: build.owner,
      repo: build.repo,
      eventName: build.eventName
    } as BuildDetails;
    const data = JSON.stringify(buildDetails, null, 2);
    await fs.writeFile(jsonFile, data);

    const options: UploadArtifactOptions = {};
    await this._artifactClient.uploadArtifact('build-details', [jsonFile], process.cwd(), options);
  }
}
