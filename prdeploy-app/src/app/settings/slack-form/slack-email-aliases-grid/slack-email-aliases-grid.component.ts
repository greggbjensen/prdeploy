import { ChangeDetectorRef, Component, Input } from '@angular/core';
import { SlackSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../../models';
import { AddSlackEmailAliasDialogComponent } from './add-slack-email-alias-dialog/add-slack-email-alias-dialog.component';
import _ from 'lodash';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { firstValueFrom } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { KeyValuePipe } from '@angular/common';

@Component({
  selector: 'app-slack-email-aliases-grid',
  standalone: true,
  imports: [
    AddSlackEmailAliasDialogComponent,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatInputModule,
    FormsModule,
    KeyValuePipe
  ],
  templateUrl: './slack-email-aliases-grid.component.html',
  styleUrl: './slack-email-aliases-grid.component.scss'
})
export class SlackEmailAliasesGridComponent {
  emails: { email: string }[] = [];
  bindingLevel: SettingsLevel;
  hasAliases = false;
  displayColumns = ['email', 'alias', 'remove'];

  constructor(
    private _dialog: MatDialog,
    private _changeDectectorRef: ChangeDetectorRef
  ) {}

  async showAddDialog() {
    const dialogRef = this._dialog.open<AddSlackEmailAliasDialogComponent, void, string>(
      AddSlackEmailAliasDialogComponent,
      {
        width: '450px',
        height: '210px'
      }
    );

    const email = await firstValueFrom(dialogRef.afterClosed());
    if (email) {
      this.add(email);
    }
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

  add(email: string) {
    // Must use level and not binding level here to all override on repo.
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
    delete this.slack.emailAliases[this.bindingLevel][email];
    this.updateEmailList();
  }

  private updateEmailList() {
    this.hasAliases = this._slack.emailAliases[this._level] && this.getValidEmails(this._level).length > 0;
    this.bindingLevel = this.hasAliases ? this._level : 'owner';

    if (this._slack.emailAliases[this.bindingLevel] && this.bindingLevel) {
      this.emails = this.getValidEmails(this.bindingLevel).map(email => ({
        email
      }));
    } else {
      this.emails = [];
    }

    this._changeDectectorRef.detectChanges();
  }

  private getValidEmails(level: SettingsLevel) {
    return Object.keys(this.slack.emailAliases[level]).filter(email => !_.isNil(this.slack.emailAliases[level][email]));
  }
}
