import { Component, Input } from '@angular/core';
import { ApolloQueryResult } from '@apollo/client/core';
import { DxAccordionModule, DxLoadIndicatorModule, DxTabsModule } from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
import {
  DeploySettingsCompare,
  DeploySettingsCompareGQL,
  DeploySettingsCompareQuery,
  EnvironmentSettings
} from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';
import { EnvironmentFormComponent } from '../environment-form/environment-form.component';
import { JiraFormComponent } from '../jira-form/jira-form.component';
import { SettingsLevel } from '../models';
import { SlackFormComponent } from '../slack-form/slack-form.component';
import { DeployFormComponent } from '../deploy-form/deploy-form.component';
import { Tab } from 'src/app/shared/models';
@Component({
  selector: 'app-settings-form',
  standalone: true,
  imports: [
    DeployFormComponent,
    EnvironmentFormComponent,
    JiraFormComponent,
    SlackFormComponent,
    DxLoadIndicatorModule,
    DxAccordionModule,
    DxTabsModule
  ],
  templateUrl: './settings-form.component.html',
  styleUrl: './settings-form.component.scss'
})
export class SettingsFormComponent {
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
    this.showOwner = this._level === 'repo';
    this.hasEnvironments = true;
    if (this._level === 'repo' && this.settingsCompare.environments.repo.length === 0) {
      this.hasEnvironments = false;
      this.bindingEnvironments = this.settingsCompare.environments.owner;
    } else {
      this.bindingEnvironments = this.settingsCompare.environments[this.level];
    }
  }

  get level() {
    return this._level;
  }

  constructor(
    private _deploySettingsCompareGQL: DeploySettingsCompareGQL,
    private _repoManager: RepoManager
  ) {
    this.fetchSettings();
  }

  async fetchSettings() {
    this.loading = true;

    const responseValue = sessionStorage.getItem('response');
    let response: ApolloQueryResult<DeploySettingsCompareQuery>;
    if (responseValue) {
      response = JSON.parse(responseValue);
    } else {
      response = await firstValueFrom(
        this._deploySettingsCompareGQL.fetch({
          input: {
            owner: this._repoManager.owner,
            repo: this._repoManager.repo
          }
        })
      );
    }

    sessionStorage.setItem('response', JSON.stringify(response));
    this.settingsCompare = response.data.deploySettingsCompare;
    this.settingsCompare.environments['owner'].forEach(e => {
      e.automationTest = e.automationTest || { enabled: false };
    });
    this.settingsCompare.environments['repo'].forEach(e => {
      e.automationTest = e.automationTest || { enabled: false };
    });

    this.loading = false;
  }
}
