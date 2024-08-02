import { Injectable } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { firstValueFrom, Subject } from 'rxjs';
import { OAuthOptions } from '../options';

// SourceRef: https://lucasarcuri.com/blog/angular-load-config-file-before-app-starts/
@Injectable({
  providedIn: 'root'
})
export class AppConfigService {
  private isLoadedSubject$ = new Subject();
  public isLoaded$ = this.isLoadedSubject$.asObservable();

  constructor(
    private _httpBackend: HttpBackend,
    private _oauthOptions: OAuthOptions
  ) {}

  async load(): Promise<any> {
    // We must initialize new to avoid Auth interception before configuration.
    // SourceRef: https://github.com/auth0/auth0-angular/issues/70
    const http = new HttpClient(this._httpBackend);
    const config: { [section: string]: any } = await firstValueFrom(http.get('/assets/app.config.json'));
    Object.assign(this._oauthOptions, config['oauth']);
  }

  setLoaded(isLoaded: boolean) {
    this.isLoadedSubject$.next(isLoaded);
  }
}
