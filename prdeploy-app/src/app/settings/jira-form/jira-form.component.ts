import { Component, Input } from '@angular/core';
import { DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { JiraSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { DxTextBoxTypes } from 'devextreme-angular/ui/text-box';
import { DxButtonTypes } from 'devextreme-angular/ui/button';

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

  passwordMode: DxTextBoxTypes.TextBoxType = 'password';
  passwordButton: DxButtonTypes.Properties = {
    icon: 'eyeopen',
    stylingMode: 'text',
    onClick: () => {
      this.passwordMode = this.passwordMode === 'text' ? 'password' : 'text';
    }
  };
}
