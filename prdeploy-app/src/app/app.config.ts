import { ApplicationConfig, ErrorHandler, Injector } from '@angular/core';
import { provideRouter, RouterModule } from '@angular/router';
import { routes } from './app.routes';
import { BrowserModule } from '@angular/platform-browser';
import { MarkdownModule } from 'ngx-markdown';
import { HttpErrorInterceptor } from './shared/interceptors';
import { HTTP_INTERCEPTORS, withInterceptorsFromDi, provideHttpClient } from '@angular/common/http';
import { OAuthOptions } from './shared/options';
import { APP_INITIALIZER, importProvidersFrom } from '@angular/core';
import {
  AppConfigService,
  authConfig,
  AuthGuard,
  authModuleConfig,
  AuthService,
  GlobalErrorHandler
} from './shared/services';
import { APOLLO_NAMED_OPTIONS, Apollo, NamedOptions } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';
import { ApolloClientOptions, InMemoryCache } from '@apollo/client/core';
import { OAuthModule, OAuthModuleConfig, OAuthStorage } from 'angular-oauth2-oidc';
import { DOCUMENT } from '@angular/common';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';

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
    importProvidersFrom(BrowserModule, MarkdownModule.forRoot(), OAuthModule.forRoot(), RouterModule.forRoot([])),
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { floatLabel: 'always' } },
    AppConfigService,
    {
      provide: APP_INITIALIZER,
      useFactory: (appConfigService: AppConfigService, injector: Injector, document: Document) => async () => {
        await appConfigService.load();
        const options = injector.get(OAuthOptions);
        const config = authConfig(options, document);
        const authService = injector.get(AuthService);
        authService.initialize(config);
        authService.runInitialLoginSequence();
      },
      multi: true,
      deps: [AppConfigService, Injector, DOCUMENT]
    },
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    { provide: OAuthModuleConfig, useValue: authModuleConfig },
    { provide: OAuthStorage, useFactory: storageFactory },
    // app.config.json sections.
    {
      provide: OAuthOptions,
      useClass: OAuthOptions
    },
    AuthGuard,
    Apollo,
    {
      provide: APOLLO_NAMED_OPTIONS,
      useFactory(httpLink: HttpLink): NamedOptions {
        return {
          deploy: createApollo(httpLink, '/graphql')
        };
      },
      deps: [HttpLink]
    },
    // Interceptors.
    { provide: HTTP_INTERCEPTORS, useClass: HttpErrorInterceptor, multi: true },
    provideAnimationsAsync()
  ]
};
