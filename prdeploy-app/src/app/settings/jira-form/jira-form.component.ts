import { Component, Input } from '@angular/core';
import { JiraSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { RevealPasswordDirective } from 'src/app/shared/directives';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-jira-form',
  standalone: true,
  imports: [
    MatCheckboxModule,
    MatIconModule,
    MatInputModule,
    MatButtonModule,
    MatFormFieldModule,
    RevealPasswordDirective,
    FormsModule
  ],
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
