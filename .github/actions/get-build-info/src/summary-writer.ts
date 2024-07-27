import * as core from '@actions/core';
import { Build, PullRequest } from './models';
import _ from 'lodash';

export class SummaryWriter {
  static readonly HeadingLevel = 2;

  async write(pull: PullRequest, deployBuilds: Build[]): Promise<void> {
    core.info('Writing deploy builds.');

    // Ensure alphabetical order.
    core.summary.addHeading('Deploy Services', SummaryWriter.HeadingLevel);
    if (deployBuilds && deployBuilds.length > 0) {
      const sortedBuilds = _.sortBy(deployBuilds, d => d.name);

      core.summary.addRaw(`
| Name     | Version  | Run ID    |
|----------|----------|-----------|`);

      for (const build of sortedBuilds) {
        core.summary.addRaw(`
| ${build.name} | ${build.version} | [${build.runId}](${build.url}) |`);
      }

      core.summary.addRaw(`
`);
    } else {
      core.summary.addRaw(`

*No builds found*
`);
    }

    core.info('Writing pull request.');
    core.summary.addRaw(`
## [#${pull.prNumber}](${pull.url}) ${pull.title}

${pull.body ? pull.body : ''}
`);

    await core.summary.write();
  }
}
