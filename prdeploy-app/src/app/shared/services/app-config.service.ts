import { Injectable } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthClientConfig } from '@auth0/auth0-angular';
import { ApiOptions, Auth0Options } from '../options';

// SourceRef: https://lucasarcuri.com/blog/angular-load-config-file-before-app-starts/
@Injectable({
  providedIn: 'root'
})
export class AppConfigService {
  constructor(
    private _httpBackend: HttpBackend,
    private _authClientConfig: AuthClientConfig,
    private _apiOptions: ApiOptions,
    private _auth0Options: Auth0Options
  ) {}

  async load(): Promise<any> {
    // We must initialize new to avoid Auth interception before configuration.
    // SourceRef: https://github.com/auth0/auth0-angular/issues/70
    const http = new HttpClient(this._httpBackend);
    const config: { [section: string]: any } = await firstValueFrom(http.get('/assets/app.config.json'));
    Object.assign(this._apiOptions, config['api']);
    Object.assign(this._auth0Options, config['auth0']);

    this._authClientConfig.set({
      domain: this._auth0Options.domain,
      clientId: this._auth0Options.clientId,
      authorizationParams: {
        audience: this._auth0Options.audience,
        redirect_uri: window.location.origin + '/login'
      },
      httpInterceptor: {
        allowedList: [
          this._apiOptions.prdeployApiUrl,
          'api/authentication/loginWithAuth0'
        ]
      },
      cacheLocation: 'localstorage',
      skipRedirectCallback: this.shouldSkipRedirect()
    });
  }

  shouldSkipRedirect(): boolean {
    const path = window.location.pathname;

    if (path.startsWith('/redirect')) {
      return true;
    }

    return false;
  }
}
