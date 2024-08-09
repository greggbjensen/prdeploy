import { Component } from '@angular/core';
import { DxButtonModule, DxTabsModule } from 'devextreme-angular';
import { Tab } from '../shared/models';
import { SettingsFormComponent } from './settings-form/settings-form.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [SettingsFormComponent, DxButtonModule, DxTabsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {
  tabs: Tab[] = [
    {
      id: 'owner',
      text: 'Owner Defaults'
    },
    {
      id: 'repo',
      text: 'Repository Override'
    }
  ];

  updateSettings() {}
}
