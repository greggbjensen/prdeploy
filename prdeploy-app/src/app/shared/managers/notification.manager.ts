import { Injectable } from '@angular/core';
import { NotificationData } from './models/notification-data';
import { ToastType } from 'devextreme/ui/toast';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  notification: NotificationData = {
    visible: false,
    message: '',
    type: 'success'
  };

  show(message: string, type: ToastType = 'success'): void {
    Object.assign(this.notification, {
      message,
      type,
      visible: true
    });
  }
}
