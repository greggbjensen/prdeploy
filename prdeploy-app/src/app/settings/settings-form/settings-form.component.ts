import { Component, Input } from '@angular/core';
import { ApolloQueryResult } from '@apollo/client/core';
import {
  DxAccordionModule,
  DxCheckBoxModule,
  DxFormModule,
  DxLoadIndicatorModule,
  DxSelectBoxModule,
  DxTextBoxModule
} from 'devextreme-angular';
import { firstValueFrom } from 'rxjs';
import { DeploySettingsCompare, DeploySettingsCompareGQL, DeploySettingsCompareQuery } from 'src/app/shared/graphql';
import { RepoManager } from 'src/app/shared/managers';
import { EnvironmentFormComponent } from '../environment-form/environment-form.component';

type SettingsLevel = 'owner' | 'repo';

@Component({
  selector: 'app-settings-form',
  standalone: true,
  imports: [
    EnvironmentFormComponent,
    DxFormModule,
    DxTextBoxModule,
    DxSelectBoxModule,
    DxLoadIndicatorModule,
    DxAccordionModule,
    DxCheckBoxModule
  ],
  templateUrl: './settings-form.component.html',
  styleUrl: './settings-form.component.scss'
})
export class SettingsFormComponent {
  settingsCompare: DeploySettingsCompare;
  loading = true;
  showOwner = true;
  environments: string[];

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
    this.updateEnvironmentSelection();
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
    this.updateEnvironmentSelection();
    this.loading = false;
  }

  updateEnvironmentSelection() {
    if (this.settingsCompare && this.settingsCompare.environments[this._level]) {
      this.environments = this.settingsCompare.environments[this._level].map(e => e.name);
    } else {
      this.environments = [];
    }
  }
}
