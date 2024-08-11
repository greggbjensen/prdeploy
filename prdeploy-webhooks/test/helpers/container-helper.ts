import { REPOSITORY } from '@src/injection-tokens';
import { Octokit } from '@octokit/rest';
import { container } from 'tsyringe';
import { RepoSettingsHelper } from './repo-settings-helper';
import { LogService } from '@src/services';

export class ContainerHelper {
  static async registerDefaults(skipRepoSettings = false): Promise<Octokit> {
    container.reset();

    let octokit: Octokit;
    if (process.env.GITHUB_TOKEN) {
      octokit = new Octokit({ auth: process.env.GITHUB_TOKEN });
    } else {
      octokit = new Octokit();
    }

    container.register(Octokit, {
      useValue: octokit
    });

    const repository = {
      owner: { login: 'greggbjensen' },
      name: 'prdeploy-example-repo',
      full_name: 'greggbjensen/prdeploy-example-repo'
    };
    container.register(REPOSITORY, {
      useValue: repository
    });

    const log = container.resolve(LogService);
    log.context.repository = repository as any;
    container.register(LogService, {
      useValue: log
    });

    if (!skipRepoSettings) {
      await RepoSettingsHelper.mockCalls(octokit);
    }

    return octokit;
  }
}
