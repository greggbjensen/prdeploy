import { Octokit } from '@octokit/rest';
import { DependencyContainer, container } from 'tsyringe';
import { LogService, RepoSettingsService } from './services';
import { Repository } from '@octokit/webhooks-types';
import { REPOSITORY } from './injection-tokens';
import { PullRequest, RepoSettings } from './models';

export const webhookFlow = async (
  octokit: Octokit | any,
  repository: Repository | any,
  pullRequest: PullRequest | any,
  action: (scope: DependencyContainer, settings: RepoSettings, log: LogService) => Promise<void>
): Promise<void> => {
  const childScope = container.createChildContainer();
  childScope.register(Octokit, { useValue: octokit });
  childScope.register(REPOSITORY, { useValue: repository });
  const logService = childScope.resolve(LogService);
  Object.assign(logService.context, {
    repository,
    pullRequest
  });
  const settingsService = childScope.resolve(RepoSettingsService);
  const settings = await settingsService.get();
  childScope.register(RepoSettings, { useValue: settings });

  try {
    await action(childScope, settings, logService);
  } catch (error: any) {
    logService.error('Error running flow:', error);
  }
};
