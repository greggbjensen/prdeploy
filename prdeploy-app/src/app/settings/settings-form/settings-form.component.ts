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
      const ownerSettings = this.gatherSettings(this.settingsCompare, 'owner');
      const repoSettings = this.gatherSettings(this.settingsCompare, 'repo');
      await firstValueFrom(
        this._deploySettingsSetGQL.mutate({
          ownerInput: {
            settings: ownerSettings
          },
          repoInput: {
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

  private gatherSettings(compare: DeploySettingsCompare, level: SettingsLevel): DeploySettingsInput {
    const input = {} as DeploySettingsInput;

    input.deployWorkflow = compare.deployWorkflow[level];
    input.deployManagerSiteUrl = compare.deployManagerSiteUrl[level];
    input.defaultEnvironment = compare.defaultEnvironment[level];
    input.releaseEnvironment = compare.releaseEnvironment[level];
    input.environments = compare.environments[level];

    this.setJira(input.jira, compare.jira, level);
    this.setBuilds(input.builds, compare.builds, level);
    this.setSlack(input.slack, compare.slack, level);
    this.setBadge(input.badge, compare.badge, level);

    return input;
  }

  private setJira(input: JiraSettingsInput, compare: JiraSettingsCompare, level: SettingsLevel) {
    input.addIssuesEnabled = compare.addIssuesEnabled[level];
    input.host = compare.host[level];
    input.username = compare.username[level];
    input.password = compare.password[level];
  }

  private setBuilds(input: BuildsSettingsInput, compare: BuildsSettingsCompare, level: SettingsLevel) {
    input.checkPattern = compare.checkPattern[level];
    input.workflowPattern = compare.workflowPattern[level];
  }

  private setSlack(input: SlackSettingsInput, compare: SlackSettingsCompare, level: SettingsLevel) {
    input.webhooks.deployUrl = compare.webhooks.deployUrl[level];
    input.webhooks.releaseUrl = compare.webhooks.releaseUrl[level];
    input.notificationsEnabled = compare.notificationsEnabled[level];
    input.token = compare.token[level];
    input.emailDomain = compare.emailDomain[level];
    input.emailAliases = compare.emailAliases[level];
  }

  private setBadge(input: BadgeSettingsInput, compare: BadgeSettingsCompare, level: SettingsLevel) {
    input.statusColors.error = compare.statusColors.error[level];
    input.statusColors.warn = compare.statusColors.warn[level];
    input.statusColors.info = compare.statusColors.info[level];
    input.statusColors.success = compare.statusColors.success[level];
  }
}
