import { ChangeDetectorRef, Component, Input } from '@angular/core';
import { DxButtonModule, DxDataGridModule, DxTextBoxModule } from 'devextreme-angular';
import { SlackSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../../models';
import { AddSlackEmailAliasDialogComponent } from '../add-slack-email-alias-dialog/add-slack-email-alias-dialog.component';
import _ from 'lodash';

@Component({
  selector: 'app-slack-email-aliases-grid',
  standalone: true,
  imports: [AddSlackEmailAliasDialogComponent, DxDataGridModule, DxButtonModule, DxTextBoxModule],
  templateUrl: './slack-email-aliases-grid.component.html',
  styleUrl: './slack-email-aliases-grid.component.scss'
})
export class SlackEmailAliasesGridComponent {
  emails: { email: string }[] = [];
  addEmailAliasDialogVisible = false;

  showAddDialog() {
    this.addEmailAliasDialogVisible = true;
  }

  private _slack: SlackSettingsCompare;
  @Input() set slack(value: SlackSettingsCompare) {
    this._slack = value;
    this.updateEmailList();
  }

  get slack() {
    return this._slack;
  }

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.updateEmailList();
  }

  get level() {
    return this._level;
  }

  constructor(private _changeDetectorRef: ChangeDetectorRef) {}

  add(email: string) {
    if (!this.slack.emailAliases[this._level]) {
      this.slack.emailAliases[this._level] = {};
    }

    const emails = Object.keys(this.slack.emailAliases[this._level]).map(e => e.toLowerCase());
    if (emails.includes(email.toLowerCase())) {
      return;
    }

    this.slack.emailAliases[this._level][email] = '';
    this.updateEmailList();
  }

  remove(email: any) {
    delete this.slack.emailAliases[this._level][email];
    this.updateEmailList();
  }

  private updateEmailList() {
    if (this._slack.emailAliases[this.level] && this._level) {
      this.emails = Object.keys(this.slack.emailAliases[this.level])
        .filter(email => !_.isNil(this.slack.emailAliases[this.level][email]))
        .map(email => ({
          email
        }));
    }
  }
}
