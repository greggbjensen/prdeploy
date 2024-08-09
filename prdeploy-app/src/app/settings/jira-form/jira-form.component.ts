import { Component, Input } from '@angular/core';
import { DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { JiraSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';

@Component({
  selector: 'app-jira-form',
  standalone: true,
  imports: [DxTextBoxModule, DxCheckBoxModule],
  templateUrl: './jira-form.component.html',
  styleUrl: './jira-form.component.scss'
})
export class JiraFormComponent {
  @Input() jira: JiraSettingsCompare;

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
