import { Octokit } from '@octokit/rest';
import { DependencyContainer, container } from 'tsyringe';
import { LogService, DeploySettingsService } from './services';
import { Repository } from '@octokit/webhooks-types';
import { REPOSITORY } from './injection-tokens';
import { PullRequest, DeploySettings } from './models';
import { Version3Client } from 'jira.js';

export const webhookFlow = async (
  octokit: Octokit | any,
  repository: Repository | any,
  pullRequest: PullRequest | any,
  action: (scope: DependencyContainer, settings: DeploySettings, log: LogService) => Promise<void>
): Promise<void> => {
  const childScope = container.createChildContainer();
  childScope.register(Octokit, { useValue: octokit });
  childScope.register(REPOSITORY, { useValue: repository });
  const logService = childScope.resolve(LogService);
  Object.assign(logService.context, {
    repository,
    pullRequest
  });
  const settingsService = childScope.resolve(DeploySettingsService);
  const settings = await settingsService.get();
  childScope.register(DeploySettings, { useValue: settings });
  childScope.register(Version3Client, {
    useFactory: () => {
      return new Version3Client({
        host: settings.jira.host,
        authentication: {
          basic: {
            email: settings.jira.username,
            apiToken: settings.jira.password
          }
        }
      });
    }
  });

  try {
    await action(childScope, settings, logService);
  } catch (error: any) {
    logService.error('Error running flow:', error);
  }
};
