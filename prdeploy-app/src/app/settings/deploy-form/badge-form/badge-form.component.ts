import { Component, Input } from '@angular/core';
import { BadgeSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { DxColorBoxModule } from 'devextreme-angular';

@Component({
  selector: 'app-badge-form',
  standalone: true,
  imports: [DxColorBoxModule],
  templateUrl: './badge-form.component.html',
  styleUrl: './badge-form.component.scss'
})
export class BadgeFormComponent {
  @Input() badge: BadgeSettingsCompare;

  showOwner = true;

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
  }

  get level() {
    return this._level;
  }
}
