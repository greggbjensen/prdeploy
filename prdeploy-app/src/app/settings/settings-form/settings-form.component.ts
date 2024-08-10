import { ChangeDetectorRef, Component, Input } from '@angular/core';
import { DxAccordionModule, DxButtonModule, DxLoadIndicatorModule, DxTabsModule } from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
import {
  BadgeSettingsCompare,
  BadgeSettingsInput,
  BuildsSettingsCompare,
  BuildsSettingsInput,
  DeploySettingsCompare,
  DeploySettingsCompareGQL,
  DeploySettingsInput,
  DeploySettingsSetGQL,
  EnvironmentSettings,
  EnvironmentSettingsInput,
  JiraSettingsCompare,
  JiraSettingsInput,
  SlackSettingsCompare,
  SlackSettingsInput
} from 'src/app/shared/graphql';
import { NotificationManager, RepoManager } from 'src/app/shared/managers';
import { EnvironmentFormComponent } from '../environment-form/environment-form.component';
import { JiraFormComponent } from '../jira-form/jira-form.component';
import { SettingsLevel } from '../models';
import { SlackFormComponent } from '../slack-form/slack-form.component';
import { DeployFormComponent } from '../deploy-form/deploy-form.component';
import { Tab } from 'src/app/shared/models';
import { AddEnvironmentDialogComponent } from '../add-environment-dialog/add-environment-dialog.component';
import _ from 'lodash';
import { LoggingService } from 'src/app/shared/services';

interface SetCompareValue<T> {
  level: SettingsLevel;
  compare: T;
}

@Component({
  selector: 'app-settings-form',
  standalone: true,
  imports: [
    DeployFormComponent,
    EnvironmentFormComponent,
    JiraFormComponent,
    SlackFormComponent,
    AddEnvironmentDialogComponent,
    DxLoadIndicatorModule,
    DxAccordionModule,
    DxTabsModule,
    DxButtonModule
  ],
  templateUrl: './settings-form.component.html',
  styleUrl: './settings-form.component.scss'
})
export class SettingsFormComponent {
  addEnvironmentVisible = false;
  settingsCompare: DeploySettingsCompare;
  hasEnvironments = false;
  bindingEnvironments: EnvironmentSettings[];
  loading = true;
  showOwner = true;
  settingsTabs: Tab[] = [
    {
      id: 'environments',
      text: 'Environments',
      icon: 'bi bi-card-list'
    },
    {
      id: 'slack',
      text: 'Slack',
      icon: 'bi bi-slack'
    },
    {
      id: 'jira',
      text: 'JIRA',
      icon: 'bi bi-ticket-detailed'
    },
    {
      id: 'deployment',
      text: 'Deployment',
      icon: 'bi bi-cloud-upload'
    }
  ];

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.updateBindingEnvironments();
    this._changeDetectorRef.detectChanges();
  }

  get level() {
    return this._level;
  }

  constructor(
    private _deploySettingsCompareGQL: DeploySettingsCompareGQL,
    private _deploySettingsSetGQL: DeploySettingsSetGQL,
    private _repoManager: RepoManager,
    private _notificationManager: NotificationManager,
    private _changeDetectorRef: ChangeDetectorRef,
    private _loggingService: LoggingService
  ) {
    this.fetchSettings();
  }

  async save() {
    this.loading = true;
    try {
      const ownerSettings = this.gatherSettings({ compare: this.settingsCompare, level: 'owner' });
      const repoSettings = this.gatherSettings({ compare: this.settingsCompare, level: 'repo' });
      await firstValueFrom(
        this._deploySettingsSetGQL.mutate({
          ownerInput: {
            owner: this._repoManager.owner,
            settings: ownerSettings
          },
          repoInput: {
            owner: this._repoManager.owner,
            repo: this._repoManager.repo,
            settings: repoSettings
          }
        })
      );

      this._notificationManager.show('Settings save complete.');
    } catch (error) {
      this._notificationManager.show('Error saving settings', 'error');
      this._loggingService.error(error, `Error saving settings.`);
    }
  }

  async resetForm() {
    // Replace current state with that from the server.
    await this.fetchSettings();
    this._changeDetectorRef.detectChanges();
    this._notificationManager.show(`Settings changes reverted.`);
  }

  async fetchSettings() {
    this.loading = true;
    const response = await firstValueFrom(
      this._deploySettingsCompareGQL.fetch({
        input: {
          owner: this._repoManager.owner,
          repo: this._repoManager.repo
        }
      })
    );

    // Avoid read only issues by cloning.
    this.settingsCompare = _.cloneDeep(response.data.deploySettingsCompare);

    this.settingsCompare.environments['owner'].forEach(e => {
      e.automationTest = e.automationTest || { enabled: false };
    });
    this.settingsCompare.environments['repo'].forEach(e => {
      e.automationTest = e.automationTest || { enabled: false };
    });

    this.loading = false;
    this.updateBindingEnvironments();
  }

  async showAddEnvironmentDialog() {
    this.addEnvironmentVisible = true;
  }

  async addEnvironment(name: string) {
    let environments = this.settingsCompare.environments[this._level];
    if (!environments) {
      environments = [];
      this.settingsCompare.environments[this._level] = environments;
    }

    if (environments.find(e => e.name.toLowerCase() === name.toLowerCase())) {
      return;
    }

    environments.push({
      name,
      requireApproval: false,
      requireBranchUpToDate: false,
      automationTest: {
        enabled: false
      }
    } as EnvironmentSettings);

    this._changeDetectorRef.detectChanges();
    this.updateBindingEnvironments();
    this._notificationManager.show(`Environment ${name} added.`);
  }

  async removeEnvironment(name: string) {
    const environments = this.settingsCompare.environments[this._level];
    const index = environments.findIndex(e => e.name === name);
    if (index >= 0) {
      environments.splice(index, 1);
      this.updateBindingEnvironments();
      this._notificationManager.show(`Environment ${name} removed.`);
    }
  }

  private updateBindingEnvironments() {
    if (!this.settingsCompare || !this._level) {
      return;
    }

    this.showOwner = this._level === 'repo';
    this.hasEnvironments = true;
    if (this._level === 'repo' && this.settingsCompare.environments.repo.length === 0) {
      this.hasEnvironments = false;
      this.bindingEnvironments = this.settingsCompare.environments.owner;
    } else {
      this.bindingEnvironments = this.settingsCompare.environments[this._level];
    }
  }

  private gatherSettings(compareValue: SetCompareValue<DeploySettingsCompare>): DeploySettingsInput {
    const input = {} as DeploySettingsInput;

    this.set(input, 'deployWorkflow', compareValue);
    this.set(input, 'prdeployPortalUrl', compareValue);
    this.set(input, 'defaultEnvironment', compareValue);
    this.set(input, 'releaseEnvironment', compareValue);

    const { compare, level } = compareValue;
    if (compare.environments && compare.environments[level].length > 0) {
      input.environments = [];
      compare.environments[level].forEach(e => input.environments.push(this.mapEnvironment(e)));
    }

    if (compare.jira) {
      input.jira = {};
      this.setJira(input.jira, { compare: compare.jira, level });
    }

    if (compare.builds) {
      input.builds = {};
      this.setBuilds(input.builds, { compare: compare.builds, level });
    }

    if (compare.slack) {
      input.slack = {};
      this.setSlack(input.slack, { compare: compare.slack, level });
    }

    if (compare.badge) {
      input.badge = {};
      this.setBadge(input.badge, { compare: compare.badge, level });
    }

    return input;
  }

  private mapEnvironment(compare: EnvironmentSettings) {
    const input = {} as EnvironmentSettingsInput;

    this.setObjectValue(input, 'name', compare);
    this.setObjectValue(input, 'queue', compare);
    this.setObjectValue(input, 'url', compare);
    this.setObjectValue(input, 'requireApproval', compare);
    this.setObjectValue(input, 'requireBranchUpToDate', compare);

    if (compare.excludeFromRollback && compare.excludeFromRollback.length > 0) {
      // TODO: ADD excludeFromRollback to UI.
      input.excludeFromRollback = compare.excludeFromRollback;
    }

    if (this.hasObjectValues(compare, 'automationTest')) {
      input.automationTest = {};
      this.setObjectValue(input.automationTest, 'enabled', compare.automationTest);
      this.setObjectValue(input.automationTest, 'workflow', compare.automationTest);
      if (this.hasObjectValues(compare.automationTest, 'inputs')) {
        this.setObjectValue(input.automationTest, 'inputs', compare.automationTest);
      }
    }

    return input;
  }

  private setJira(input: JiraSettingsInput, compareValue: SetCompareValue<JiraSettingsCompare>) {
    this.set(input, 'addIssuesEnabled', compareValue);
    this.set(input, 'host', compareValue);
    this.set(input, 'username', compareValue);
    this.set(input, 'password', compareValue);
  }

  private setBuilds(input: BuildsSettingsInput, compareValue: SetCompareValue<BuildsSettingsCompare>) {
    this.set(input, 'checkPattern', compareValue);
    this.set(input, 'workflowPattern', compareValue);
  }

  private setSlack(input: SlackSettingsInput, compareValue: SetCompareValue<SlackSettingsCompare>) {
    if (this.hasValues(compareValue, 'webhooks')) {
      input.webhooks = {};
      this.set(input.webhooks, 'deployUrl', compareValue);
      this.set(input.webhooks, 'releaseUrl', compareValue);
    }

    this.set(input, 'notificationsEnabled', compareValue);
    this.set(input, 'token', compareValue);
    this.set(input, 'emailDomain', compareValue);

    if (this.hasValues(compareValue, 'emailAliases')) {
      this.set(input, 'emailAliases', compareValue);
    }
  }

  private setBadge(input: BadgeSettingsInput, compareValue: SetCompareValue<BadgeSettingsCompare>) {
    if (this.hasValues(compareValue, 'statusColors')) {
      input.statusColors = {};
      this.set(input.statusColors, 'error', compareValue);
      this.set(input.statusColors, 'warn', compareValue);
      this.set(input.statusColors, 'info', compareValue);
      this.set(input.statusColors, 'success', compareValue);
    }
  }

  private hasObjectValues<T>(compare: T, key: keyof T) {
    return compare[key] && Object.keys(compare[key]).filter(c => _.isNil(c)).length > 0;
  }

  private hasValues<T>(compareValue: SetCompareValue<T>, key: keyof T) {
    return compareValue.compare[key] && Object.keys(compareValue.compare[key]).filter(c => _.isNil(c)).length > 0;
  }

  private setObjectValue<T>(input: T, key: keyof T, compare: any) {
    if (!_.isNil(compare[key])) {
      input[key] = compare[key];
    }
  }

  private set<T>(input: T, key: keyof T, { level, compare }: SetCompareValue<any>) {
    if (!_.isNil(compare[level][key])) {
      input[key] = compare[level][key];
    }
  }
}
