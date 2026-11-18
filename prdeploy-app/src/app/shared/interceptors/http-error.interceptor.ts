import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpErrorResponse,
  HttpHandler,
  HttpEvent,
  HttpStatusCode
} from '@angular/common/http';

import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services';

/**
 * Logs the user out when the API rejects the session (401) or access (403).
 *
 * The original error is always re-emitted so downstream consumers, such as the Apollo link chain,
 * never see a stream that completes without emitting a value.
 */
@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  constructor(private _authService: AuthService) {}

  /**
   * Passes the request on and handles authentication failures by redirecting to login.
   * @param request Outgoing request.
   * @param next Next handler in the chain.
   * @returns The response events; errors are propagated after any logout side effect.
   */
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === HttpStatusCode.Unauthorized) {
          this._authService.logout();
        } else if (err.status == HttpStatusCode.Forbidden) {
          this._authService.logout(true);
        }
        return throwError(() => err);
      })
    );
  }
}
