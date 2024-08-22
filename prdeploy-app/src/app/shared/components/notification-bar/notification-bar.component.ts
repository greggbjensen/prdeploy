import { Component, Inject } from '@angular/core';
import { MAT_SNACK_BAR_DATA } from '@angular/material/snack-bar';
import { NotificationData } from '../../managers';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-notification-bar',
  standalone: true,
  imports: [MatIcon],
  templateUrl: './notification-bar.component.html',
  styleUrl: './notification-bar.component.scss'
})
export class NotificationBarComponent {
  private static readonly IconSet: { [type: string]: string } = {
    info: 'info',
    success: 'check_circle',
    warning: 'warning',
    danger: 'error'
  };
  icon: string;

  constructor(@Inject(MAT_SNACK_BAR_DATA) public notification: NotificationData) {
    this.icon = NotificationBarComponent.IconSet[notification.type];
  }
}
