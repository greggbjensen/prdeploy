import { Component, Input } from '@angular/core';
import { BadgeSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../../models';
import { MtxColorpickerModule } from '@ng-matero/extensions/colorpicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-badge-form',
  standalone: true,
  imports: [MtxColorpickerModule, MatFormFieldModule, FormsModule, MatInputModule],
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
