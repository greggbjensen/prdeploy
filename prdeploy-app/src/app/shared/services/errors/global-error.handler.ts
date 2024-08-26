import { ErrorHandler, Injectable } from '@angular/core';
import { LoggingService } from '../logging.service';
import { NotificationManager } from '../../managers';

@Injectable({
  providedIn: 'root'
})
export class GlobalErrorHandler implements ErrorHandler {
  private static readonly ChunkFailedMessage = /Loading chunk [\d]+ failed/;

  constructor(
    private _loggingService: LoggingService,
    private _notificationManager: NotificationManager
  ) {}

  handleError(error: any): void {
    if (GlobalErrorHandler.ChunkFailedMessage.test(error.message)) {
      window.location.reload();
      return;
    }

    this._loggingService.error(error);
    this._notificationManager.show(error.message, 'danger');
  }
}
