import { AfterViewInit, Component, Input, ViewChild } from '@angular/core';
import { DxAccordionModule, DxTabsModule } from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
import {
  BadgeSettingsCompare,
  BadgeSettingsInput,
  BadgeStatusColorsSettingsCompare,
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
import { AddEnvironmentDialogComponent } from './add-environment-dialog/add-environment-dialog.component';
import _ from 'lodash';
import { LoggingService } from 'src/app/shared/services';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatListModule, MatSelectionList, MatSelectionListChange } from '@angular/material/list';
import { Tab } from 'src/app/shared/models';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatExpansionModule } from '@angular/material/expansion';
import { ServicesFormComponent } from '../services-form/services-form.component';

class SetCompareValue<T> {
  hasValues: boolean = false;

  constructor(
    public compare: T,
    public level: SettingsLevel
  ) {}
}

@Component({
  selector: 'app-settings-form',
  standalone: true,
  imports: [
    DeployFormComponent,
    EnvironmentFormComponent,
    ServicesFormComponent,
    JiraFormComponent,
    SlackFormComponent,
    AddEnvironmentDialogComponent,
    MatProgressSpinnerModule,
    MatExpansionModule,
    DxAccordionModule,
    DxTabsModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    ServicesFormComponent
  ],
  templateUrl: './settings-form.component.html',
  styleUrl: './settings-form.component.scss'
})
export class SettingsFormComponent implements AfterViewInit {
  @ViewChild(MatSelectionList) settingsNav: MatSelectionList;

  settingsCompare: DeploySettingsCompare;
  hasEnvironments = false;
  bindingEnvironments: EnvironmentSettings[];
  loading = true;
  showOwner = true;
  settingsTabs: Tab[] = [
    {
      id: 'environments',
      text: 'Environments',
      icon: 'list_alt',
      selected: true
    },
    {
      id: 'services',
      text: 'Services',
      icon: 'language'
    },
    {
      id: 'slack',
      text: 'Slack',
      icon: 'webhook'
    },
    {
      id: 'jira',
      text: 'JIRA',
      icon: 'workspaces'
    },
    {
      id: 'deployment',
      text: 'Deployment',
      icon: 'cloud'
    }
  ];

  activeTabId: string = 'environments';
  selectedEnvironment: string;

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.updateBindingEnvironments();
  }

  get level() {
    return this._level;
  }

  constructor(
    private _deploySettingsCompareGQL: DeploySettingsCompareGQL,
    private _deploySettingsSetGQL: DeploySettingsSetGQL,
    private _repoManager: RepoManager,
    private _notificationManager: NotificationManager,
    private _dialog: MatDialog,
    private _loggingService: LoggingService
  ) {
    this._repoManager.valueChanged$.pipe(takeUntilDestroyed()).subscribe(() => this.fetchSettings());
  }

  ngAfterViewInit(): void {
    const selectOption = this.settingsNav.options.find(o => o.value === this.activeTabId);
    if (selectOption) {
      this.settingsNav.selectedOptions.select(selectOption);
    }
  }

  selectedEnvironmentChange(environment: EnvironmentSettings) {
    this.selectedEnvironment = environment.name;
  }

  settingsNavChange(event: MatSelectionListChange) {
    this.activeTabId = event.options[0].value;
  }

  isSettingsNavSelected(tab: Tab) {
    return tab.id == this.activeTabId;
  }

  async save() {
    this.loading = true;
    try {
      const ownerSettings = this.gatherSettings(new SetCompareValue(this.settingsCompare, 'owner'));
      const repoSettings = this.gatherSettings(new SetCompareValue(this.settingsCompare, 'repo'));
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
      this._notificationManager.show('Error saving settings', 'danger');
      this._loggingService.error(error, `Error saving settings.`);
    }

    this.loading = false;
  }

  async resetForm() {
    // Replace current state with that from the server.
    await this.fetchSettings();
    this._notificationManager.show(`Settings changes reverted.`);
  }

  async fetchSettings() {
    if (!this._repoManager.isValid) {
      return;
    }

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
      e.automationTest = e.automationTest || { enabled: null };
    });
    this.settingsCompare.environments['repo'].forEach(e => {
      e.automationTest = e.automationTest || { enabled: null };
    });

    const colors = Object.keys(this.settingsCompare.badge.statusColors) as Array<
      keyof Omit<BadgeStatusColorsSettingsCompare, '__typename'>
    >;
    for (const name of colors) {
      const color = this.settingsCompare.badge.statusColors[name];
      if ((name as any) === '__typename') {
        continue;
      }

      if (_.isNil(color.owner)) {
        color.owner = '';
      }

      if (_.isNil(color.repo)) {
        color.repo = '';
      }
    }

    this.loading = false;
    this.updateBindingEnvironments();
  }

  async showAddEnvironmentDialog() {
    const dialogRef = this._dialog.open<AddEnvironmentDialogComponent, void, string>(AddEnvironmentDialogComponent, {
      width: '450px',
      height: '210px'
    });

    const environmentName = await firstValueFrom(dialogRef.afterClosed());
    if (environmentName) {
      this.addEnvironment(environmentName);
    }
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

    if (!this.selectedEnvironment && this.bindingEnvironments.length > 0) {
      this.selectedEnvironment = this.bindingEnvironments[0].name;
    }
  }

  private gatherSettings(compareValue: SetCompareValue<DeploySettingsCompare>): DeploySettingsInput {
    const input = {
      jira: {},
      builds: {},
      badge: {},
      slack: {
        webhooks: {}
      }
    } as DeploySettingsInput;

    this.set(input, 'deployWorkflow', compareValue);
    this.set(input, 'prdeployPortalUrl', compareValue);
    this.set(input, 'defaultEnvironment', compareValue);
    this.set(input, 'releaseEnvironment', compareValue);

    const { compare, level } = compareValue;
    if (compare.environments && compare.environments[level].length > 0) {
      input.environments = [];
      compare.environments[level].forEach(e => input.environments.push(this.mapEnvironment(e)));
    }

    input.jira = {};
    const jiraCompare = new SetCompareValue(compare.jira, level);
    this.setJira(input.jira, jiraCompare);
    if (!jiraCompare.hasValues) {
      delete input.jira;
    }

    const buildsCompare = new SetCompareValue(compare.builds, level);
    this.setBuilds(input.builds, buildsCompare);
    if (!buildsCompare.hasValues) {
      delete input.builds;
    }

    const slackCompare = new SetCompareValue(compare.slack, level);
    this.setSlack(input.slack, slackCompare);
    if (!slackCompare.hasValues) {
      delete input.slack;
    }

    const badgeCompare = new SetCompareValue(compare.badge, level);
    this.setBadge(input.badge, badgeCompare);
    if (!badgeCompare.hasValues) {
      delete input.badge;
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
    const webhooksCompare = new SetCompareValue(compareValue.compare.webhooks, compareValue.level);
    this.set(input.webhooks, 'deployUrl', webhooksCompare);
    this.set(input.webhooks, 'releaseUrl', webhooksCompare);
    if (!webhooksCompare.hasValues) {
      delete input.webhooks;
    }

    this.set(input, 'notificationsEnabled', compareValue);
    this.set(input, 'token', compareValue);
    this.set(input, 'emailDomain', compareValue);

    if (this.hasValues(compareValue, 'emailAliases')) {
      this.setObjectValue(input, 'emailAliases', compareValue.compare.emailAliases);
    }
  }

  private setBadge(input: BadgeSettingsInput, compareValue: SetCompareValue<BadgeSettingsCompare>) {
    if (this.hasValues(compareValue, 'statusColors')) {
      input.statusColors = {};
      this.setObjectValue(input.statusColors, 'error', compareValue.compare.statusColors);
      this.setObjectValue(input.statusColors, 'warn', compareValue.compare.statusColors);
      this.setObjectValue(input.statusColors, 'info', compareValue.compare.statusColors);
      this.setObjectValue(input.statusColors, 'success', compareValue.compare.statusColors);
    }
  }

  private hasObjectValues<T>(compare: T, key: keyof T) {
    return compare[key] && Object.keys(compare[key]).filter(c => !_.isNil(c)).length > 0;
  }

  private hasValues<T>(compareValue: SetCompareValue<T>, key: keyof T) {
    return compareValue.compare[key] && Object.keys(compareValue.compare[key]).filter(c => !_.isNil(c)).length > 0;
  }

  private setObjectValue<T>(input: T, key: keyof T, compare: any) {
    if (!_.isNil(compare[key])) {
      input[key] = compare[key];
    }
  }

  private set<T>(input: T, key: keyof T, compareValue: SetCompareValue<any>) {
    const { compare, level } = compareValue;
    if (!_.isNil(compare[key][level])) {
      compareValue.hasValues = true;
      input[key] = compare[key][level];
    }
  }
}
