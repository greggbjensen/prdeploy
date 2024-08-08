import { KeyValue, KeyValuePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { EnvironmentSettings } from 'src/app/shared/graphql';

@Component({
  selector: 'app-environment-form',
  standalone: true,
  imports: [DxTextBoxModule, DxCheckBoxModule, KeyValuePipe],
  templateUrl: './environment-form.component.html',
  styleUrl: './environment-form.component.scss'
})
export class EnvironmentFormComponent {
  @Input() environment: EnvironmentSettings;

  castToStringDictionary(list: any): object {
    return list as object;
  }
}
