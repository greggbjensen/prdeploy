import { Component, Input } from '@angular/core';
import { DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { SlackSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';

@Component({
  selector: 'app-slack-form',
  standalone: true,
  imports: [DxTextBoxModule, DxCheckBoxModule],
  templateUrl: './slack-form.component.html',
  styleUrl: './slack-form.component.scss'
})
export class SlackFormComponent {
  @Input() slack: SlackSettingsCompare;

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
