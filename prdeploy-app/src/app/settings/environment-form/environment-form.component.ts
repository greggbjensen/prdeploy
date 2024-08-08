import { Component, Input } from '@angular/core';
import { DxButtonModule, DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { EnvironmentSettings } from 'src/app/shared/graphql';
import { AddAutomationInputDialogComponent } from '../add-automation-input-dialog/add-automation-input-dialog.component';
import { KeyValuePipe } from '@angular/common';

@Component({
  selector: 'app-environment-form',
  standalone: true,
  imports: [DxTextBoxModule, DxCheckBoxModule, DxButtonModule, AddAutomationInputDialogComponent, KeyValuePipe],
  templateUrl: './environment-form.component.html',
  styleUrl: './environment-form.component.scss'
})
export class EnvironmentFormComponent {
  @Input() environment: EnvironmentSettings;

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

  removeAutomationInput(name: any) {
    delete this.environment.automationTest.inputs[name];
  }
}
