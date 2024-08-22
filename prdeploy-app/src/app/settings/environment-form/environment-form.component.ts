import { Component, Input } from '@angular/core';
import { EnvironmentSettings } from 'src/app/shared/graphql';
import { KeyValuePipe } from '@angular/common';
import { SettingsLevel } from '../models';
import { AddAutomationInputDialogComponent } from './add-automation-input-dialog/add-automation-input-dialog.component';
import { AddExcludeRollbackServiceDialogComponent } from './add-exclude-rollback-service-dialog/add-exclude-rollback-service-dialog.component';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-environment-form',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatTableModule,
    MatChipsModule,
    FormsModule,
    MatCheckboxModule,
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
  Object = Object;

  automationInputColumns = ['name', 'value', 'remove'];

  private _level: SettingsLevel;
  @Input() set level(value: SettingsLevel) {
    this._level = value;
    this.showOwner = this.level == 'repo';
  }

  get level() {
    return this._level;
  }

  constructor(private _dialog: MatDialog) {}

  automationInputChange(event: Event, inputName: string) {
    const input = event.target as HTMLInputElement;
    this.environment.automationTest.inputs[inputName] = input.value;
  }

  async showAddExcludeRollbackService() {
    const dialogRef = this._dialog.open<AddExcludeRollbackServiceDialogComponent, void, string>(
      AddExcludeRollbackServiceDialogComponent,
      {
        width: '450px',
        height: '210px'
      }
    );

    const serviceName = await firstValueFrom(dialogRef.afterClosed());
    if (serviceName) {
      this.addExcludeRollbackService(serviceName);
    }
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

  async showAddAutomationInput() {
    const dialogRef = this._dialog.open<AddAutomationInputDialogComponent, void, string>(
      AddAutomationInputDialogComponent,
      {
        width: '450px',
        height: '210px'
      }
    );

    const automationInputName = await firstValueFrom(dialogRef.afterClosed());
    if (automationInputName) {
      this.addAutomationInput(automationInputName);
    }
  }

  addAutomationInput(name: string) {
    if (!this.environment.automationTest.inputs) {
      this.environment.automationTest.inputs = {};
    }

    this.environment.automationTest.inputs[name] = `\${${name}}`;
  }

  removeAutomationInput(name: any) {
    delete this.environment.automationTest.inputs[name];
  }
}
