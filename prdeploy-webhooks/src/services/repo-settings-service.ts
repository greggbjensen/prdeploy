import { Repository } from '@octokit/webhooks-types';
import { REPOSITORY, DEFAULT_SETTINGS_FILE } from '@src/injection-tokens';
import { EmailAliases, RepoSettings } from '@src/models';
import { Octokit, RestEndpointMethodTypes } from '@octokit/rest';
import { Lifecycle, inject, scoped } from 'tsyringe';
import yaml from 'js-yaml';
import cache from 'memory-cache';
import fs from 'fs/promises';
import { LogService } from './log-service';
import { ObjectUtil } from '@src/utils';
import AsyncLock from 'async-lock';
import _ from 'lodash';
import { ParameterService } from './parameter-service';

type ContentResponse = RestEndpointMethodTypes['repos']['getContent']['response'];

@scoped(Lifecycle.ContainerScoped)
export class RepoSettingsService {
  static readonly RepoSettingsFile = '.prdeploy.yaml';
  private static readonly SettingsCacheTimeout = 300000; // 5m, keep cache short so it can easily get latest.
  private static readonly DefaultEnvironmentColors = {
    default: 'ededed',
    dev: 'd4ac0d',
    stage: '2e86c1',
    prod: '1d8348'
  };
  private static readonly EmailAliasesVariable = 'EMAIL_ALIASES';
  private static readonly DefaultBranch = 'main';
  private static GlobalDefaultSettings: RepoSettings;
  private static readonly Lock = new AsyncLock();

  constructor(
    private _octokit: Octokit,
    private _log: LogService,
    private _parameterService: ParameterService,
    @inject(REPOSITORY) private _repository: Repository,
    @inject(DEFAULT_SETTINGS_FILE) private _defaultSettingsFile: string
  ) {}

  async get(): Promise<RepoSettings> {
    const cacheKey = this.getSettingsCacheKey();
    let result = cache.get(cacheKey) as RepoSettings;
    if (result) {
      return result;
    }

    result = await RepoSettingsService.Lock.acquire(cacheKey, async () => {
      let settings = cache.get(cacheKey) as RepoSettings;
      if (settings) {
        return settings;
      }

      const defaultSettings = await this.getDefaultSettings();
      const repoSettings = await this.getRepoSettings(defaultSettings.settingsBranch);

      settings = ObjectUtil.deepAssign(new RepoSettings(), defaultSettings, repoSettings);
      await this.applyEmailAliases(settings);
      await this.applyEnvironmentColors(settings);
      this.normalizeDefaults(settings);

      cache.put(cacheKey, settings, RepoSettingsService.SettingsCacheTimeout);

      return settings;
    });

    return result;
  }

  private normalizeDefaults(settings: RepoSettings): void {
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

  private async applyEmailAliases(settings: RepoSettings): Promise<void> {
    try {
      const emailAliases = await this._parameterService.getOrCreateObject<EmailAliases>(
        RepoSettingsService.EmailAliasesVariable,
        {},
        'Org'
      );

      settings.emailAliases = emailAliases;
    } catch (err: any) {
      // Do nothing.
    }
  }

  private async getRepoSettings(settingsBranch: string): Promise<RepoSettings> {
    const settings = new RepoSettings();

    // Override if specified.
    if (process.env.SETTINGS_BRANCH) {
      settingsBranch = process.env.SETTINGS_BRANCH;
      settings.settingsBranch = settingsBranch;
    }

    settings.owner = this._repository.owner.login;
    settings.repo = this._repository.name;

    let response: ContentResponse;
    try {
      response = await this._octokit.rest.repos.getContent({
        owner: settings.owner,
        repo: settings.repo,
        path: RepoSettingsService.RepoSettingsFile,
        ref: settingsBranch
      });
    } catch (err: any) {
      if (err.status === 404) {
        this._log.warn(
          `Repository ${settings.owner}/${settings.repo} does not contain a ${RepoSettingsService.RepoSettingsFile} file, using defaults.`
        );
      } else {
        throw err;
      }
    }

    const base64Content = (response?.data as any)?.content as string;
    if (base64Content) {
      const content = Buffer.from(base64Content.trim(), 'base64').toString();
      const values = yaml.load(content) as RepoSettings;
      Object.assign(settings, values);
    }

    settings.defaultBranch = RepoSettingsService.DefaultBranch;
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

  private async applyEnvironmentColors(repoSettings: RepoSettings): Promise<void> {
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
      color = RepoSettingsService.DefaultEnvironmentColors.default;
    } else if (label.startsWith('dev')) {
      color = RepoSettingsService.DefaultEnvironmentColors.dev;
    } else if (label.startsWith('stag')) {
      color = RepoSettingsService.DefaultEnvironmentColors.stage;
    } else if (label.startsWith('prod')) {
      color = RepoSettingsService.DefaultEnvironmentColors.prod;
    } else {
      color = RepoSettingsService.DefaultEnvironmentColors.default;
    }

    return color;
  }

  private async getDefaultSettings(): Promise<RepoSettings> {
    if (!RepoSettingsService.GlobalDefaultSettings) {
      RepoSettingsService.GlobalDefaultSettings = await this.loadGlobalDefaultSettings();
    }

    const settings = Object.assign(new RepoSettings(), RepoSettingsService.GlobalDefaultSettings);
    return settings;
  }

  private getSettingsCacheKey(): string {
    return `${this._repository.owner.login}:${this._repository.name}:repo-settings`;
  }

  private async loadGlobalDefaultSettings(): Promise<RepoSettings> {
    const content = await fs.readFile(this._defaultSettingsFile, 'utf8');
    const values = yaml.load(content) as RepoSettings;
    const settings = Object.assign(new RepoSettings(), values);

    settings.deployManagerSiteUrl = process.env.DEPLOY_MANAGER_SITE_URL;

    Object.assign(settings.slack, {
      token: process.env.SLACK_TOKEN,
      emailDomain: process.env.SLACK_EMAIL_DOMAIN
    });

    return settings;
  }
}
