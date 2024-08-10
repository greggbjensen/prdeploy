import { Component, Input } from '@angular/core';
import { DxButtonModule, DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { SlackSettingsCompare } from 'src/app/shared/graphql';
import { SettingsLevel } from '../models';
import { KeyValuePipe } from '@angular/common';
import { SlackEmailAliasesGridComponent } from './slack-email-aliases-grid/slack-email-aliases-grid.component';
import { DxButtonTypes } from 'devextreme-angular/ui/button';
import { DxTextBoxTypes } from 'devextreme-angular/ui/text-box';

@Component({
  selector: 'app-slack-form',
  standalone: true,
  imports: [SlackEmailAliasesGridComponent, DxTextBoxModule, DxCheckBoxModule, KeyValuePipe, DxButtonModule],
  templateUrl: './slack-form.component.html',
  styleUrl: './slack-form.component.scss'
})
export class SlackFormComponent {
  @Input() slack: SlackSettingsCompare;

  tokenPasswordMode: DxTextBoxTypes.TextBoxType = 'password';
  tokenButton: DxButtonTypes.Properties = {
    icon: 'eyeopen',
    stylingMode: 'text',
    onClick: () => {
      this.tokenPasswordMode = this.tokenPasswordMode === 'text' ? 'password' : 'text';
    }
  };
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
