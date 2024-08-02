import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpErrorResponse, HttpHandler, HttpEvent } from '@angular/common/http';

import { EMPTY, Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services';

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  constructor(private _authService: AuthService) {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 401) {
          this._authService.logout();
          return EMPTY;
        }
        throw err;
      })
    );
  }
}
