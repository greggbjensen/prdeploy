import { Component, Input } from '@angular/core';
import { SlackSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { KeyValuePipe } from '@angular/common';
import { SlackEmailAliasesGridComponent } from './slack-email-aliases-grid/slack-email-aliases-grid.component';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { RevealPasswordDirective } from 'src/app/shared/directives';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-slack-form',
  standalone: true,
  imports: [
    SlackEmailAliasesGridComponent,
    MatInputModule,
    MatCheckboxModule,
    MatIconModule,
    MatButtonModule,
    RevealPasswordDirective,
    FormsModule,
    KeyValuePipe
  ],
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
