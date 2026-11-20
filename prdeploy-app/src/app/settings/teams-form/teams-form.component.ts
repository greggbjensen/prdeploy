import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { TeamsSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatCheckboxModule } from '@angular/material/checkbox';

/** Form for the Microsoft Teams notification settings: an enabled flag and the deploy and release webhook URLs. */
@Component({
  selector: 'app-teams-form',
  imports: [MatInputModule, MatCheckboxModule, FormsModule],
  templateUrl: './teams-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './teams-form.component.scss'
})
export class TeamsFormComponent {
  /** Owner and repo values of the Microsoft Teams settings being edited. */
  @Input() teams: TeamsSettingsCompare;

  /** Whether owner values are shown as placeholders, which is the case when editing a repo. */
  showOwner = true;

  private _level: SettingsLevel;

  /** Settings level being edited; at repo level the owner values are shown as placeholders. */
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
  }

  get level() {
    return this._level;
  }
}
