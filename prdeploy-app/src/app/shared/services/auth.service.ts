import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, Inject, EventEmitter } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { AuthService as Auth0Service, RedirectLoginOptions } from '@auth0/auth0-angular';
import { User } from '../models';
import { UserService } from './user.service';
import { DialogService } from './dialog/dialog.service';
import { firstValueFrom } from 'rxjs';
import { User as Auth0User } from '@auth0/auth0-angular';
import { LoggingService } from './logging.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  userChangedKey = 'userChanged';
  userChange = new EventEmitter<User>();

  private _baseUrl = 'api/authentication';

  constructor(
    private _router: Router,
    private _dialogService: DialogService,
    private _auth0Service: Auth0Service,
    private _http: HttpClient,
    private _userService: UserService,
    private _loggingService: LoggingService,
    @Inject(DOCUMENT) private _document: Document
  ) {
    // DistroyRef not needed in constructors.
    this._auth0Service.user$.pipe(takeUntilDestroyed()).subscribe(user => this.setUserFromResponse(user));
  }

  async ensureAuthenticated(): Promise<boolean> {
    let isAuthenticated = false;
    try {
      isAuthenticated = await firstValueFrom(this._auth0Service.isAuthenticated$);
      if (isAuthenticated) {
        const auth0User = await firstValueFrom(this._auth0Service.user$);
        this.setUserFromResponse(auth0User);
      } else {
        this.setUserFromResponse(null);
        this.redirectToLogin({ returnUrl: this._document.location.href });
      }
    } catch (error) {
      this._loggingService.error(error);
    }

    return isAuthenticated;
  }

  async redirectToLogin(params?: any) {
    let returnUrl = params.returnUrl;
    if (returnUrl && returnUrl.startsWith('http')) {
      returnUrl = 'redirect?returnUrl=' + returnUrl;
    }

    const options: RedirectLoginOptions<any> = {
      authorizationParams: {
        redirect_uri: this._document.location.origin + '/login/callback',
        screen_hint: 'prdeploy'
      },
      appState: { target: returnUrl }
    };

    if (params.email) {
      options.authorizationParams.login_hint = params.email;
    }

    await firstValueFrom(this._auth0Service.loginWithRedirect(options));
  }

  async logout(expired = false) {
    try {
      await firstValueFrom(this._http.post<any>(`${this._baseUrl}/logout`, {}));

      this.changeUser(null);
      this.logoutFrontEnd(expired);
      const isAuth0Authenticated = await firstValueFrom(this._auth0Service.isAuthenticated$);
      if (isAuth0Authenticated) {
        await firstValueFrom(
          this._auth0Service.logout({
            logoutParams: { returnTo: this._document.location.origin }
          })
        );
      } else {
        this._router.navigate(['/login'], { state: { skipUserCheck: true } });
      }
    } catch (error) {
      this._router.navigate(['/login'], { state: { skipUserCheck: true } });
    }
  }

  private setUserFromResponse(response: Auth0User): void {
    console.log(response);
    this._userService.user = {
      email: 'greggbjensen@myorg.com'
    } as User;

    // this._userService.user = response
    //   ? {
    //       email: response.email,
    //       userId: response.sub,
    //       firstName: response.given_name,
    //       lastName: response.family_name
    //     }
    //   : null;
  }

  private async logoutFrontEnd(expired: boolean) {
    if (expired) {
      await firstValueFrom(
        this._dialogService.show('Login Expired', ['Your login has expired. Please log in again.'], ['OK'])
      );
    }
  }

  private changeUser(user: User) {
    const cachedUser = this._userService.user;

    if (!cachedUser && !user) {
      return;
    }

    localStorage.setItem(this.userChangedKey, Date.now().toString());
    this._userService.user = user;
    this.userChange.emit(user);
  }
}
