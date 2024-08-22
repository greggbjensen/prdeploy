import { ErrorHandler, Injectable } from '@angular/core';
import { LoggingService } from '../logging.service';
import { NotificationManager } from '../../managers';

@Injectable({
  providedIn: 'root'
})
export class GlobalErrorHandler implements ErrorHandler {
  constructor(
    private _loggingService: LoggingService,
    private _notificationManager: NotificationManager
  ) {}

  handleError(error: any): void {
    this._loggingService.error(error);
    this._notificationManager.show(error.message, 'danger');
  }
}
