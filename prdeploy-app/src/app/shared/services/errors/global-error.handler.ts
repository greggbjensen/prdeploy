import { ErrorHandler, Injectable } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { LoggingService } from '../logging.service';

@Injectable({
  providedIn: 'root'
})
export class GlobalErrorHandler implements ErrorHandler {
  constructor(
    private _authService: AuthService,
    private _loggingService: LoggingService
  ) {}

  handleError(error: any): void {
    this._loggingService.error(error);
  }
}
