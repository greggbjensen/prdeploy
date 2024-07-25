import * as core from '@actions/core';
import { BuildInfo } from './models/build-info';

export class SummaryWriter {
  static readonly HeadingLevel = 2;
  static readonly ShortHashLength = 7;

  async write(build: BuildInfo): Promise<void> {
    core.info('Writing build details.');

    const gitHubUrl = `https://github.com/${build.owner}/${build.repo}`;
    const shortCommit = build.commit?.substring(0, SummaryWriter.ShortHashLength);

    if (build.pullRequest) {
      const pull = build.pullRequest;
      core.summary.addRaw(`
## [#${pull.prNumber}](${pull.url}) ${pull.title}

${pull.body ? pull.body : ''}
`);
    }

    let headRow = '';
    if (build.head) {
      headRow = `
| Head     | [${build.head}](${gitHubUrl}/tree/${build.head}) |`;
    }

    core.summary.addHeading('Build Details', SummaryWriter.HeadingLevel).addRaw(`
| Name     |       Value      |
|----------|------------------|
| Version  | ${build.version} |${headRow}
| Base     | [${build.base}](${gitHubUrl}/tree/${build.base}) |
| Commit   | [${shortCommit}](${gitHubUrl}/commit/${build.commit}) |
| Run ID   | ${build.runId}   |

`);

    await core.summary.write();
  }
}
