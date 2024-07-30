import { AuthConfig } from 'angular-oauth2-oidc';
import { OAuthOptions } from '../../options';

export const authConfig = (options: OAuthOptions, document: Document) => {
  return {
    issuer: 'https://github.com',
    loginUrl: 'https://github.com/login/oauth/authorize',
    tokenEndpoint: '/api/oauth/access_token',
    clientId: options.clientId, // The "Auth Code + PKCE" client
    responseType: 'code',
    redirectUri: document.location.origin + '/login/callback',
    scope: 'openid profile email api', // Ask offline_access to support refresh token refreshes
    sessionChecksEnabled: true,
    showDebugInformation: true, // Also requires enabling "Verbose" level in devtools
    clearHashAfterLogin: false // https://github.com/manfredsteyer/angular-oauth2-oidc/issues/457#issuecomment-431807040,
  } as AuthConfig;
};
