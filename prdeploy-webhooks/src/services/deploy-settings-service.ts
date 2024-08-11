import { Repository } from '@octokit/webhooks-types';
import { REPOSITORY } from '@src/injection-tokens';
import { JiraSettings, DeploySettings, SlackWebhooksSettings, SlackSettings } from '@src/models';
import { Octokit } from '@octokit/rest';
import { Lifecycle, inject, scoped } from 'tsyringe';
import cache from 'memory-cache';
import { LogService } from './log-service';
import { ObjectUtil } from '@src/utils';
import AsyncLock from 'async-lock';
import _ from 'lodash';
import { ParameterService } from './parameter-service';

@scoped(Lifecycle.ContainerScoped)
export class DeploySettingsService {
  private static readonly DeploySettingsKey = 'DEPLOY_SETTINGS';
  private static readonly SettingsCacheTimeout = 300000; // 5m, keep cache short so it can easily get latest.
  private static readonly DefaultEnvironmentColors = {
    default: '#ededed',
    dev: '#d4ac0d',
    stage: '#2e86c1',
    prod: '#1d8348'
  };
  private static readonly DefaultBranch = 'main';
  private static readonly Lock = new AsyncLock();

  constructor(
    private _octokit: Octokit,
    private _log: LogService,
    private _parameterService: ParameterService,
    @inject(REPOSITORY) private _repository: Repository
  ) {}

  async get(): Promise<DeploySettings> {
    const cacheKey = this.getSettingsCacheKey();
    let result = cache.get(cacheKey) as DeploySettings;
    if (result) {
      return result;
    }

    result = await DeploySettingsService.Lock.acquire(cacheKey, async () => {
      let settings = cache.get(cacheKey) as DeploySettings;
      if (settings) {
        return settings;
      }

      const defaultSettings = await this.getOwnerSettings();
      const repoSettings = await this.getRepoSettings();

      settings = ObjectUtil.deepAssign(new DeploySettings(), defaultSettings, repoSettings);
      await this.applyParameters(settings);
      await this.applyEnvironmentColors(settings);
      this.normalizeDefaults(settings);

      cache.put(cacheKey, settings, DeploySettingsService.SettingsCacheTimeout);

      return settings;
    });

    return result;
  }

  private normalizeDefaults(settings: DeploySettings): void {
    if (_.isNil(settings.services)) {
      settings.services = [];
    }

    for (const service of settings.services) {
      // Default service to root path of same name.
      if (!service.path) {
        service.path = service.name;
      }
    }

    for (const environment of settings.environments) {
      if (_.isNil(environment.requireApproval)) {
        environment.requireApproval = false;
      }

      if (_.isNil(environment.requireBranchUpToDate)) {
        environment.requireBranchUpToDate = false;
      }

      if (_.isNil(environment.automationTest)) {
        environment.automationTest = { enabled: false };
      } else if (_.isNil(environment.automationTest.enabled) && environment.automationTest.workflow) {
        // If there is a workflow and no enabled value is set, then enable it.
        environment.automationTest.enabled = true;
      }

      if (_.isNil(environment.excludeFromRollback)) {
        environment.excludeFromRollback = [];
      }
    }
  }

  private async applyParameters(settings: DeploySettings): Promise<void> {
    if (!settings.jira) {
      settings.jira = {} as JiraSettings;
    }
    if (!settings.slack) {
      settings.slack = {} as SlackSettings;
    }
    if (!settings.slack.webhooks) {
      settings.slack.webhooks = {} as SlackWebhooksSettings;
    }
  }

  private async getRepoSettings(): Promise<DeploySettings> {
    const settings = new DeploySettings();
    settings.owner = this._repository.owner.login;
    settings.repo = this._repository.name;

    let values: DeploySettings;
    try {
      values = await this._parameterService.getObject<DeploySettings>(DeploySettingsService.DeploySettingsKey, 'Repo');
    } catch (err: any) {
      if (err.status === 404) {
        this._log.warn(
          `Repository ${settings.owner}/${settings.repo} does not have repository specific settings, using defaults.`
        );
      } else {
        throw err;
      }
    }

    Object.assign(settings, values);

    settings.defaultBranch = DeploySettingsService.DefaultBranch;
    try {
      const repoResponse = await this._octokit.rest.repos.get({
        owner: settings.owner,
        repo: settings.repo
      });

      settings.defaultBranch = repoResponse.data.default_branch;
    } catch (err: any) {
      if (err.status === 404) {
        this._log.error(`Repository ${settings.owner}/${settings.repo} could not be found.`);
      } else {
        throw err;
      }
    }

    return settings;
  }

  private async applyEnvironmentColors(repoSettings: DeploySettings): Promise<void> {
    const response = await this._octokit.rest.issues.listLabelsForRepo({
      owner: repoSettings.owner,
      repo: repoSettings.repo
    });
    const labels = response.data;

    for (const environment of repoSettings.environments) {
      const lowerEnviroment = environment.name.toLowerCase();
      const environmentLabel = labels.find(l => l.name.toLowerCase() === lowerEnviroment);
      if (environmentLabel) {
        environment.color = environmentLabel.color;
      } else {
        this._log.warn(`Label does not exist yet for ${lowerEnviroment}, creating it.`);
        environment.color = this.getDefaultColor(lowerEnviroment);
        await this._octokit.rest.issues.createLabel({
          owner: repoSettings.owner,
          repo: repoSettings.repo,
          name: lowerEnviroment,
          color: environment.color,
          description: `Pull request deployed to ${lowerEnviroment} environment.`
        });
      }

      const environmentLock = `${lowerEnviroment}-lock`;
      const hasLockLabel = labels.some(l => l.name.toLowerCase() === environmentLock);
      if (!hasLockLabel) {
        this._log.warn(`Label does not exist yet for ${environmentLock}, creating it.`);
        const color = this.getDefaultColor(environmentLock);
        await this._octokit.rest.issues.createLabel({
          owner: repoSettings.owner,
          repo: repoSettings.repo,
          name: environmentLock,
          color,
          description: `Environment ${lowerEnviroment} is locked for use by pull request.`
        });
      }
    }
  }

  private getDefaultColor(label: string): string {
    let color: string;
    if (label.endsWith('-lock')) {
      color = DeploySettingsService.DefaultEnvironmentColors.default;
    } else if (label.startsWith('dev')) {
      color = DeploySettingsService.DefaultEnvironmentColors.dev;
    } else if (label.startsWith('stag')) {
      color = DeploySettingsService.DefaultEnvironmentColors.stage;
    } else if (label.startsWith('prod')) {
      color = DeploySettingsService.DefaultEnvironmentColors.prod;
    } else {
      color = DeploySettingsService.DefaultEnvironmentColors.default;
    }

    return color;
  }

  private async getOwnerSettings(): Promise<DeploySettings> {
    let settings: DeploySettings;
    try {
      settings = await this._parameterService.getObject(DeploySettingsService.DeploySettingsKey, 'Owner');
    } catch (error) {
      this._log.error(
        `Unable to retrieve owner ${settings.owner} settings, make sure to configure this in the portal.`
      );
    }
    return settings;
  }

  private getSettingsCacheKey(): string {
    return `${this._repository.owner.login}:${this._repository.name}:repo-settings`;
  }
}
