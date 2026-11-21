import * as core from '@actions/core';

/** A workflow file and the service name it is mapped to in the PR Deploy settings. */
interface BuildNameResponse {
  workflow: string;
  name: string;
}

/** Looks up the service names mapped to build workflows in the PR Deploy settings. */
export class ServiceNameService {
  /**
   * @param _apiUrl Base URL of the PR Deploy API; when blank, no lookup is made.
   * @param _token GitHub token sent as the bearer token, which the API uses to confirm access to the repository.
   */
  constructor(
    private _apiUrl: string,
    private _token: string
  ) {}

  /**
   * Gets the mapped service names keyed by lowercase workflow file name. A failed lookup only warns and returns an
   * empty map, so builds fall back to names derived from the workflow file name instead of failing the deploy.
   */
  async list(owner: string, repo: string): Promise<Record<string, string>> {
    const apiUrl = this._apiUrl.trim().replace(/\/+$/, '');
    if (!apiUrl) {
      return {};
    }

    try {
      const url = `${apiUrl}/api/build-names/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}`;
      core.info(`Getting service names from ${url}`);
      const response = await fetch(url, { headers: { Authorization: `Bearer ${this._token}` } });
      if (!response.ok) {
        throw new Error(`The PR Deploy API returned ${response.status}.`);
      }

      const names = (await response.json()) as BuildNameResponse[];
      return Object.fromEntries(names.map(n => [n.workflow.toLowerCase(), n.name]));
    } catch (err: any) {
      core.warning(`Unable to get service names from the PR Deploy API, using workflow file names: ${err.message}`);
      return {};
    }
  }
}
