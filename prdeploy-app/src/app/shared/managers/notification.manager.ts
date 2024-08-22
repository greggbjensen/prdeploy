import { Injectable } from '@angular/core';
import { NotificationData } from './models/notification-data';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationType } from './models/notification-type';
import { NotificationBarComponent } from '../components/notification-bar/notification-bar.component';

@Injectable({
  providedIn: 'root'
})
export class NotificationManager {
  constructor(private _snackBar: MatSnackBar) {}

  show(message: string, type: NotificationType = 'success'): void {
    this._snackBar.openFromComponent(NotificationBarComponent, {
      duration: 3000,
      panelClass: [`snackbar-${type}`],
      data: {
        message,
        type
      } as NotificationData
    });
  }
}
