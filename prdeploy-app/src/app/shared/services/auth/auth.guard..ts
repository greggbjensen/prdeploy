import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { filter, switchMap, tap } from 'rxjs/operators';

import { AuthService } from './auth.service';

@Injectable()
export class AuthGuard {
  constructor(private _authService: AuthService) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
    return this._authService.isDoneLoading$.pipe(
      filter(isDone => isDone),
      switchMap(_ => this._authService.isAuthenticated$),
      tap(isAuthenticated => isAuthenticated || this._authService.login(state.url))
    );
  }
}
