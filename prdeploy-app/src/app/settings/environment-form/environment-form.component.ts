import { Component, HostBinding, Input } from '@angular/core';
import { DxAccordionModule, DxButtonModule, DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { EnvironmentSettings } from 'src/app/shared/graphql';
import { AddAutomationInputDialogComponent } from '../add-automation-input-dialog/add-automation-input-dialog.component';
import { KeyValuePipe } from '@angular/common';
import { ValueChangedEvent } from 'devextreme/ui/text_box';
import { SettingsLevel } from '../models';

@Component({
  selector: 'app-environment-form',
  standalone: true,
  imports: [
    DxAccordionModule,
    DxTextBoxModule,
    DxCheckBoxModule,
    DxButtonModule,
    AddAutomationInputDialogComponent,
    KeyValuePipe
  ],
  templateUrl: './environment-form.component.html',
  styleUrl: './environment-form.component.scss'
})
export class EnvironmentFormComponent {
  @Input() environmentName: string;
  @Input() environment: EnvironmentSettings;
  @Input() hasEnvironments: boolean;

  showOwner = true;

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
  }

  get level() {
    return this._level;
  }

  addAutomationInputVisible = false;

  showAddAutomationInput() {
    this.addAutomationInputVisible = true;
  }

  addAutomationInput(name: string) {
    if (!this.environment.automationTest.inputs) {
      this.environment.automationTest.inputs = {};
    }

    this.environment.automationTest.inputs[name] = '';
  }

  updateAutomationInput(e: ValueChangedEvent, name: any) {
    this.environment.automationTest.inputs[name] = e.value;
  }

  removeAutomationInput(name: any) {
    delete this.environment.automationTest.inputs[name];
  }
}
