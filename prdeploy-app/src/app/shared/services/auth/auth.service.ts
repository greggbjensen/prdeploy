import { Injectable } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthConfig, OAuthErrorEvent, OAuthService } from 'angular-oauth2-oidc';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { filter } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private static readonly DefaultUrl = '/deployments/queue';

  private isAuthenticatedSubject$ = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject$.asObservable();

  private isDoneLoadingSubject$ = new BehaviorSubject<boolean>(false);
  public isDoneLoading$ = this.isDoneLoadingSubject$.asObservable();

  private navigateToLoginPage() {
    // TODO: Remember current URL
    this._router.navigateByUrl('/should-login');
  }

  constructor(
    private _oauthService: OAuthService,
    private _authConfig: AuthConfig,
    private _router: Router,
    private _activatedRoute: ActivatedRoute
  ) {
    this.initialize();
  }

  public async handleCallbackRedirect(): Promise<void> {
    this.updateIsAuthenticated();

    const queryParamMap = await firstValueFrom(this._activatedRoute.queryParamMap);
    const state = queryParamMap.get('state');
    const values = state.split(';');
    let redirectUrl = '/';
    if (values.length > 1) {
      redirectUrl = decodeURIComponent(values[1]);
    }
    this._router.navigate([redirectUrl], { replaceUrl: true });
  }

  public login(targetUrl?: string) {
    // Note: before version 9.1.0 of the library you needed to
    // call encodeURIComponent on the argument to the method.
    this._oauthService.initLoginFlow(targetUrl || this._router.url);
  }

  public async runInitialLoginSequence(): Promise<void> {
    await this._oauthService.tryLogin();
    this.updateIsAuthenticated();

    if (this._oauthService.state) {
      let stateUrl = this._oauthService.state;
      if (stateUrl.startsWith('/') === false) {
        stateUrl = decodeURIComponent(stateUrl);
      }
      console.log(`There was state of ${this._oauthService.state}, so we are sending you to: ${stateUrl}`);
      this._router.navigateByUrl(stateUrl);
    } else {
      this._router.navigate([AuthService.DefaultUrl]);
    }

    this.isDoneLoadingSubject$.next(true);
  }

  public logout() {
    this._oauthService.logOut();
  }
  public hasValidToken() {
    return this._oauthService.hasValidAccessToken();
  }

  private initialize(): void {
    this._oauthService.configure(this._authConfig);

    // Useful for debugging:
    this._oauthService.events.subscribe(event => {
      if (event instanceof OAuthErrorEvent) {
        console.error('OAuthErrorEvent Object:', event);
      } else {
        console.warn('OAuthEvent Object:', event);
      }
    });

    // The following cross-tab communication of fresh access tokens works usually in practice,
    // but if you need more robust handling the community has come up with ways to extend logic
    // in the library which may give you better mileage.
    //
    // See: https://github.com/jeroenheijmans/sample-angular-oauth2-oidc-with-auth-guards/issues/2
    //
    // Until then we'll stick to this:
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
      // const user = (await this._oauthService.loadUserProfile()) as User;
      // this.userSubject$.next(user);
      this.updateIsAuthenticated();
    });

    this._oauthService.events
      .pipe(filter(e => ['session_terminated', 'session_error'].includes(e.type)))
      .subscribe(() => this.navigateToLoginPage());
  }

  private updateIsAuthenticated(): void {
    this.isAuthenticatedSubject$.next(this._oauthService.hasValidAccessToken());
  }
}
