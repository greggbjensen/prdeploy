import { Component, ViewChild } from '@angular/core';
import { DxTabsModule } from 'devextreme-angular';
import { Tab } from '../shared/models';
import { SettingsFormComponent } from './settings-form/settings-form.component';
import { RepoManager } from '../shared/managers';
import { SettingsLevel } from './models';
import { SelectionChangedEvent } from 'devextreme/ui/tab_panel';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [SettingsFormComponent, MatButtonModule, MatIconModule, DxTabsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {
  @ViewChild(SettingsFormComponent) settingsForm: SettingsFormComponent;

  ownerRepoTabs: Tab[] = [
    {
      id: 'owner',
      text: 'Owner Defaults'
    },
    {
      id: 'repo',
      text: 'Repository Override'
    }
  ];

  level: SettingsLevel = 'owner';

  constructor(public repoManager: RepoManager) {}

  selectedLevelChanged(event: SelectionChangedEvent): void {
    this.level = event.addedItems[0].id;
  }

  updateSettings() {
    this.settingsForm?.fetchSettings();
  }

  save() {
    this.settingsForm.save();
  }

  cancel() {
    this.settingsForm.resetForm();
  }
}
