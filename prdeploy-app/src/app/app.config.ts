import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { BrowserModule } from '@angular/platform-browser';
import { MarkdownModule } from 'ngx-markdown';
import { HttpErrorInterceptor } from './shared/interceptors';
import { AuthHttpInterceptor, AuthModule as Auth0Module } from '@auth0/auth0-angular';
import { HTTP_INTERCEPTORS, withInterceptorsFromDi, provideHttpClient } from '@angular/common/http';
import { ApiOptions, Auth0Options } from './shared/options';
import { APP_INITIALIZER, importProvidersFrom } from '@angular/core';
import { AppConfigService } from './shared/services';
import { APOLLO_NAMED_OPTIONS, Apollo, NamedOptions } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';
import { ApolloClientOptions, InMemoryCache } from '@apollo/client/core';

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
    importProvidersFrom(BrowserModule, MarkdownModule.forRoot(), Auth0Module.forRoot()),
    AppConfigService,
    {
      provide: APP_INITIALIZER,
      useFactory: (appConfigService: AppConfigService) => () => appConfigService.load(),
      multi: true,
      deps: [AppConfigService]
    },
    // app.config.json sections.
    {
      provide: ApiOptions,
      useClass: ApiOptions
    },
    {
      provide: Auth0Options,
      useClass: Auth0Options
    },
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
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: HttpErrorInterceptor, multi: true }
  ]
};
