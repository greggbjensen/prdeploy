import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { EnvironmentSettings, EnvironmentUrl, EnvironmentUrlInput } from 'src/app/shared/graphql';
import { KeyValuePipe, NgStyle } from '@angular/common';
import { SettingsLevel } from '../models';
import { AddAutomationInputDialogComponent } from './add-automation-input-dialog/add-automation-input-dialog.component';
import { AddEnvironmentUrlDialogComponent } from './add-environment-url-dialog/add-environment-url-dialog.component';
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
import { MtxColorpickerModule } from '@ng-matero/extensions/colorpicker';

@Component({
  selector: 'app-environment-form',
  imports: [
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatTableModule,
    MatChipsModule,
    FormsModule,
    MatCheckboxModule,
    MtxColorpickerModule,
    NgStyle,
    KeyValuePipe
  ],
  templateUrl: './environment-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './environment-form.component.scss'
})
export class EnvironmentFormComponent {
  @Input() environmentName: string;
  private _environment: EnvironmentSettings;

  /** Environment being edited; a legacy single `url` is moved into the URLs list so it can be edited or removed. */
  @Input() set environment(value: EnvironmentSettings) {
    this._environment = value;
    if (value && !value.urls?.length && value.url?.trim()) {
      value.urls = [{ label: 'URL', url: value.url.trim() }];
    }
    if (value) {
      value.url = null;
    }
  }

  get environment(): EnvironmentSettings {
    return this._environment;
  }

  @Input() hasEnvironments: boolean;

  showOwner = true;
  Object = Object;

  automationInputColumns = ['name', 'value', 'remove'];
  urlColumns = ['label', 'url', 'remove'];

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

  /** Opens the add URL dialog and appends the entered label and URL to the environment. */
  async showAddUrl() {
    const dialogRef = this._dialog.open<AddEnvironmentUrlDialogComponent, void, EnvironmentUrlInput | null>(
      AddEnvironmentUrlDialogComponent,
      {
        width: '450px'
      }
    );

    const url = await firstValueFrom(dialogRef.afterClosed());
    if (url) {
      this.addUrl(url.label, url.url);
    }
  }

  /**
   * Appends a labelled URL. A new array is assigned because the URLs table only re-renders when its data source
   * reference changes.
   */
  addUrl(label: string, url: string) {
    this.environment.urls = [...(this.environment.urls ?? []), { label, url }];
  }

  /** Removes a labelled URL from the environment, assigning a new array so the URLs table re-renders. */
  removeUrl(item: EnvironmentUrl) {
    const index = this.environment.urls?.indexOf(item) ?? -1;
    if (index >= 0) {
      this.environment.urls = this.environment.urls!.filter((_, i) => i !== index);
    }
  }

  async showAddExcludeRollbackService() {
    const dialogRef = this._dialog.open<AddExcludeRollbackServiceDialogComponent, void, string>(
      AddExcludeRollbackServiceDialogComponent,
      {
        width: '450px',
        height: '220px'
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
        height: '220px'
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
