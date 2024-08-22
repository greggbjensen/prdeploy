import { Component, Input } from '@angular/core';
import { DeploySettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { BadgeFormComponent } from './badge-form/badge-form.component';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-deploy-form',
  standalone: true,
  imports: [BadgeFormComponent, MatInputModule, MatButtonModule, FormsModule, MatSelectModule],
  templateUrl: './deploy-form.component.html',
  styleUrl: './deploy-form.component.scss'
})
export class DeployFormComponent {
  environments: string[];

  showOwner = true;

  private _settingsCompare: DeploySettingsCompare;
  @Input() set settingsCompare(value: DeploySettingsCompare) {
    this._settingsCompare = value;
    this.showOwner = this.level == 'repo';
    this.updateEnvironmentSelection();
  }

  get settingsCompare() {
    return this._settingsCompare;
  }

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
    this.updateEnvironmentSelection();
  }

  get level() {
    return this._level;
  }

  updateEnvironmentSelection() {
    if (this.settingsCompare && this.settingsCompare.environments[this._level]) {
      this.environments = this.settingsCompare.environments[this._level].map(e => e.name);
    } else {
      this.environments = [];
    }
  }
}
