import { Component, ViewChild } from '@angular/core';
import { SettingsFormComponent } from './settings-form/settings-form.component';
import { RepoManager, RouteManager } from '../shared/managers';
import { SettingsLevel } from './models';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabChangeEvent, MatTabsModule } from '@angular/material/tabs';
import { AlertPanelComponent } from '../shared/components';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
  selectedTabIndex = 0;

  constructor(
    public repoManager: RepoManager,
    private _routeManager: RouteManager,
    private _activatedRoute: ActivatedRoute
  ) {
    this._activatedRoute.params.pipe(takeUntilDestroyed()).subscribe(params => {
      const level = params['level'];
      if (level) {
        this.level = level;
        this.selectedTabIndex = level === 'owner' ? 0 : 1;
      }
    });
  }

  selectedLevelChanged(event: MatTabChangeEvent): void {
    this.level = event.index === 0 ? 'owner' : 'repo';
    this._routeManager.navigate(['/settings', this.level]);
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
