import { OAuthModuleConfig } from 'angular-oauth2-oidc';

export const authModuleConfig: OAuthModuleConfig = {
  resourceServer: {
    sendAccessToken: true,
    allowedUrls: ['/graphql']
  }
};
