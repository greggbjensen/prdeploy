import { ChangeDetectorRef, Component, ViewChild } from '@angular/core';
import { DxButtonModule, DxTabsModule } from 'devextreme-angular';
import { Tab } from '../shared/models';
import { SettingsFormComponent } from './settings-form/settings-form.component';
import { RepoManager } from '../shared/managers';
import { SettingsLevel } from './models';
import { SelectionChangedEvent } from 'devextreme/ui/tab_panel';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [SettingsFormComponent, DxButtonModule, DxTabsModule],
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

  level: SettingsLevel;

  constructor(
    public repoManager: RepoManager,
    private _changeDetectorRef: ChangeDetectorRef
  ) {}

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
