import { AuthConfig } from 'angular-oauth2-oidc';
import { OAuthOptions } from '../../options';

export const authConfig = (options: OAuthOptions, document: Document) => {
  const isDev = location.hostname === 'localhost' || location.hostname === '127.0.0.1';

  return {
    issuer: 'https://github.com',
    loginUrl: 'https://github.com/login/oauth/authorize',
    tokenEndpoint: '/api/oauth/access_token',
    userinfoEndpoint: '/api/oauth/userinfo',
    clientId: options.clientId, // The "Auth Code + PKCE" client
    responseType: 'code',
    redirectUri: document.location.origin + '/login/callback',
    scope: 'repo read:org',
    oidc: false,
    sessionChecksEnabled: true,
    showDebugInformation: true, // Also requires enabling "Verbose" level in devtools
    clearHashAfterLogin: false, // https://github.com/manfredsteyer/angular-oauth2-oidc/issues/457#issuecomment-431807040,
    requireHttps: !isDev
  } as AuthConfig;
};
