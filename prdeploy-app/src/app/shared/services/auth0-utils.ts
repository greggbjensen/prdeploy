export default class Auth0Utils {
  public static getCachedAccessToken(): any {
    const cachedAuth0State = Auth0Utils.getCachedAuth0State();

    const accessToken = cachedAuth0State?.body?.access_token;

    return accessToken;
  }

  public static getCachedUser(): any {
    const cachedAuth0State = Auth0Utils.getCachedAuth0State();

    const user = cachedAuth0State?.body?.decodedToken?.user;

    return user;
  }

  private static getCachedAuth0State(): any {
    const cachedAuth0State = JSON.parse(Auth0Utils.getLocalStorageEntryWithPrefix('@@auth0spajs@@'));

    return cachedAuth0State;
  }

  private static getLocalStorageEntryWithPrefix(prefix: string): string {
    const keys = Object.keys(localStorage);
    const matchingKey = keys.find(key => key.startsWith(prefix));
    if (matchingKey) {
      return localStorage.getItem(matchingKey);
    }
    return null;
  }
}
