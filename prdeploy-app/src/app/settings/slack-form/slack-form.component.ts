import { Component, Input } from '@angular/core';
import { DxButtonModule, DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { SlackSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { KeyValuePipe } from '@angular/common';
import { ValueChangedEvent } from 'devextreme/ui/text_box';

@Component({
  selector: 'app-slack-form',
  standalone: true,
  imports: [DxTextBoxModule, DxCheckBoxModule, KeyValuePipe, DxButtonModule],
  templateUrl: './slack-form.component.html',
  styleUrl: './slack-form.component.scss'
})
export class SlackFormComponent {
  @Input() slack: SlackSettingsCompare;

  addEmailAliasVisible = false;
  showOwner = true;

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
  }

  get level() {
    return this._level;
  }

  addEmailAlias() {
    if (!this.slack.emailAliases) {
      this.slack.emailAliases[this._level] = {};
    }

    let newNumber = 1;
    const emails = Object.keys(this.slack.emailAliases[this._level]);
    while (emails.includes(`new-${newNumber}@mydomain.com`)) {
      newNumber++;
    }

    this.slack.emailAliases[this._level][`new-${newNumber}@mydomain.com`] = '';
  }

  updateEmailAlias(e: ValueChangedEvent, name: any) {
    this.slack.emailAliases[this._level][name] = e.value;
  }

  removeEmailAlias(name: any) {
    delete this.slack.emailAliases[this._level][name];
  }
}
