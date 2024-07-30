import { ApplicationConfig, Injector } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { BrowserModule } from '@angular/platform-browser';
import { MarkdownModule } from 'ngx-markdown';
import { HttpErrorInterceptor } from './shared/interceptors';
import { HTTP_INTERCEPTORS, withInterceptorsFromDi, provideHttpClient, HttpClientModule } from '@angular/common/http';
import { ApiOptions, OAuthOptions } from './shared/options';
import { APP_INITIALIZER, importProvidersFrom } from '@angular/core';
import { AppConfigService, authConfig, AuthGuard, authModuleConfig, AuthService } from './shared/services';
import { APOLLO_NAMED_OPTIONS, Apollo, NamedOptions } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';
import { ApolloClientOptions, InMemoryCache } from '@apollo/client/core';
import { AuthConfig, OAuthModule, OAuthModuleConfig, OAuthStorage } from 'angular-oauth2-oidc';
import { DOCUMENT } from '@angular/common';

export function storageFactory(): OAuthStorage {
  return localStorage;
}

export function createApollo(httpLink: HttpLink, uri: string): ApolloClientOptions<any> {
  return {
    link: httpLink.create({ uri }),
    cache: new InMemoryCache(),
    defaultOptions: {
      query: {
        fetchPolicy: 'network-only' // Only cache when specified.
      }
    }
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    importProvidersFrom(BrowserModule, MarkdownModule.forRoot(), HttpClientModule, OAuthModule.forRoot()),
    AppConfigService,
    {
      provide: APP_INITIALIZER,
      useFactory: (appConfigService: AppConfigService, injector: Injector) => async () => {
        await appConfigService.load();
        await injector.get(AuthService).runInitialLoginSequence();
      },
      multi: true,
      deps: [AppConfigService, Injector]
    },
    { provide: OAuthModuleConfig, useValue: authModuleConfig },
    { provide: OAuthStorage, useFactory: storageFactory },
    // app.config.json sections.
    {
      provide: ApiOptions,
      useClass: ApiOptions
    },
    {
      provide: OAuthOptions,
      useClass: OAuthOptions
    },
    {
      provide: AuthConfig,
      useFactory: (options: OAuthOptions, document: Document) => {
        const config = authConfig(options, document);
        return config;
      },
      deps: [OAuthOptions, DOCUMENT]
    },
    AuthGuard,
    Apollo,
    {
      provide: APOLLO_NAMED_OPTIONS,
      useFactory(httpLink: HttpLink, apiOptions: ApiOptions): NamedOptions {
        return {
          deploy: createApollo(httpLink, apiOptions.prdeployApiUrl)
        };
      },
      deps: [HttpLink, ApiOptions]
    },
    // Interceptors.
    { provide: HTTP_INTERCEPTORS, useClass: HttpErrorInterceptor, multi: true }
  ]
};
