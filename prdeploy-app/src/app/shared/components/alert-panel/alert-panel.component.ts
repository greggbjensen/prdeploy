import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { AlertType } from './alert-type';

@Component({
  selector: 'app-alert-panel',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './alert-panel.component.html',
  styleUrl: './alert-panel.component.scss'
})
export class AlertPanelComponent {
  private static readonly IconSet: { [type: string]: string } = {
    info: 'info',
    success: 'check_circle',
    warning: 'warning',
    danger: 'error'
  };
  icon: string;

  private _type: AlertType = 'info';
  get type() {
    return this._type;
  }

  @Input()
  set type(value: AlertType) {
    this._type = value;
    this.icon = AlertPanelComponent.IconSet[value];
  }
}
