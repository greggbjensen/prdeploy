import { Component, Input } from '@angular/core';
import { DxAccordionModule, DxButtonModule, DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { EnvironmentSettings } from 'src/app/shared/graphql';
import { KeyValuePipe } from '@angular/common';
import { ValueChangedEvent } from 'devextreme/ui/text_box';
import { SettingsLevel } from '../models';
import { AddAutomationInputDialogComponent } from './add-automation-input-dialog/add-automation-input-dialog.component';
import { AddExcludeRollbackServiceDialogComponent } from './add-exclude-rollback-service-dialog/add-exclude-rollback-service-dialog.component';

@Component({
  selector: 'app-environment-form',
  standalone: true,
  imports: [
    DxAccordionModule,
    DxTextBoxModule,
    DxCheckBoxModule,
    DxButtonModule,
    AddAutomationInputDialogComponent,
    AddExcludeRollbackServiceDialogComponent,
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
  addExcludeRollbackServiceVisible = false;

  showAddExcludeRollbackService() {
    this.addExcludeRollbackServiceVisible = true;
  }

  addExcludeRollbackService(name: string) {
    if (!this.environment.excludeFromRollback) {
      this.environment.excludeFromRollback = [];
    }

    if (!this.environment.excludeFromRollback.find(e => e.toLocaleLowerCase() === name.toLowerCase())) {
      this.environment.excludeFromRollback.push(name);
    }
  }

  removeExcludeRollbackService(name: any) {
    const index = this.environment.excludeFromRollback.indexOf(name);
    this.environment.excludeFromRollback.splice(index, 1);
  }

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
