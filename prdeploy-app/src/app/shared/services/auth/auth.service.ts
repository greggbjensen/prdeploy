import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthConfig, OAuthErrorEvent, OAuthService } from 'angular-oauth2-oidc';
import { BehaviorSubject } from 'rxjs';
import { filter } from 'rxjs/operators';
import { User } from '../../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private static readonly LoginUrl = '/login';

  private userSubject$ = new BehaviorSubject<User>(null);
  user$ = this.userSubject$.asObservable();

  private isAuthenticatedSubject$ = new BehaviorSubject<boolean>(false);
  isAuthenticated$ = this.isAuthenticatedSubject$.asObservable();

  private isDoneLoadingSubject$ = new BehaviorSubject<boolean>(false);
  isDoneLoading$ = this.isDoneLoadingSubject$.asObservable();

  constructor(
    private _oauthService: OAuthService,
    private _router: Router
  ) {}

  get user(): User {
    return this.userSubject$.value;
  }

  login(targetUrl?: string) {
    // Note: before version 9.1.0 of the library you needed to
    // call encodeURIComponent on the argument to the method.
    this._oauthService.initLoginFlow(targetUrl || this._router.url);
  }

  async runInitialLoginSequence(): Promise<void> {
    await this._oauthService.tryLogin();
    this.updateIsAuthenticated();
    if (!this.isAuthenticatedSubject$.value) {
      this.isDoneLoadingSubject$.next(true);
      return;
    }

    if (this._oauthService.state) {
      let stateUrl = this._oauthService.state;
      if (stateUrl.startsWith('/') === false) {
        stateUrl = decodeURIComponent(stateUrl);
      }
      console.log(`There was state of ${this._oauthService.state}, so we are sending you to: ${stateUrl}`);
      this._router.navigateByUrl(stateUrl);
    }

    this.isDoneLoadingSubject$.next(true);
  }

  logout(forbidden = false) {
    this._oauthService.logOut();
    this._router.navigate([AuthService.LoginUrl], {
      queryParams: !forbidden ? null : { forbidden }
    });
  }

  hasValidToken() {
    return this._oauthService.hasValidAccessToken();
  }

  initialize(authConfig: AuthConfig): void {
    this._oauthService.configure(authConfig);

    // Useful for debugging:
    this._oauthService.events.subscribe(event => {
      if (event instanceof OAuthErrorEvent) {
        console.error('OAuthErrorEvent Object:', event);
      } else {
        console.warn('OAuthEvent Object:', event);
      }
    });

    window.addEventListener('storage', event => {
      // The `key` is `null` if the event was caused by `.clear()`
      if (event.key !== 'access_token' && event.key !== null) {
        return;
      }

      console.warn('Noticed changes to access_token (most likely from another tab), updating isAuthenticated');
      this.updateIsAuthenticated();

      if (!this._oauthService.hasValidAccessToken()) {
        this.navigateToLoginPage();
      }
    });

    this._oauthService.events.subscribe(_ => {
      this.updateIsAuthenticated();
    });
    this.updateIsAuthenticated();

    this._oauthService.events.pipe(filter(e => ['token_received'].includes(e.type))).subscribe(async () => {
      await this.tryLoadUser();
      this.updateIsAuthenticated();
    });

    this._oauthService.events
      .pipe(filter(e => ['session_terminated', 'session_error'].includes(e.type)))
      .subscribe(() => this.navigateToLoginPage());

    this.isDoneLoading$.subscribe(async done => {
      if (!done) {
        return;
      }
      await this.tryLoadUser();
    });
  }

  private async tryLoadUser() {
    const isAuthenticated = this._oauthService.hasValidAccessToken();
    if (isAuthenticated && !this.user) {
      const userInfo: any = await this._oauthService.loadUserProfile();
      this.userSubject$.next(userInfo.info as User);
    }
  }

  private updateIsAuthenticated(): void {
    this.isAuthenticatedSubject$.next(this._oauthService.hasValidAccessToken());
  }

  private navigateToLoginPage() {
    this._router.navigateByUrl(AuthService.LoginUrl);
  }
}
