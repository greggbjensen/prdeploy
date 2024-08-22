import { Component, ViewChild } from '@angular/core';
import { SettingsFormComponent } from './settings-form/settings-form.component';
import { RepoManager } from '../shared/managers';
import { SettingsLevel } from './models';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabChangeEvent, MatTabsModule } from '@angular/material/tabs';
import { AlertPanelComponent } from '../shared/components';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [SettingsFormComponent, MatButtonModule, MatIconModule, MatTabsModule, AlertPanelComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {
  @ViewChild(SettingsFormComponent) settingsForm: SettingsFormComponent;

  level: SettingsLevel = 'owner';

  constructor(public repoManager: RepoManager) {}

  selectedLevelChanged(event: MatTabChangeEvent): void {
    this.level = event.index === 0 ? 'owner' : 'repo';
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
