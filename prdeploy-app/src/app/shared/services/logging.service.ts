import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LoggingService {
  public error(error: any, customData: any = null): void {
    if (error instanceof Error) {
      // TODO GBJ: Add additional logging.
      if (customData) {
        console.error(error.message, customData);
      } else {
        console.error(error.message);
      }
    }
  }
}
