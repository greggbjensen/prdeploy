import { WorkflowRun } from '@octokit/webhooks-types';
import { Octokit } from '@octokit/rest';
import { PullRequest, DeploySettings } from '@src/models';
import { Lifecycle, scoped } from 'tsyringe';
import { LogService } from './log-service';
import _ from 'lodash';
import { EnvironmentUtil } from '@src/utils';

@scoped(Lifecycle.ContainerScoped)
export class PullRequestService {
  private static readonly EnvironmentTitleRegex =
    /^\[(?<environment>\w+\d*)(?:\s+(?<rollback>ROLLBACK)(?:\s+#(?<pullNumber>\d+))?(?:\s+(?<rollbackCount>\d+))?)?\]/;
  private static readonly MaxPageSize = 100;
  private static readonly EnvironmentLockRegex = /-lock$/i;

  constructor(
    private _octokit: Octokit,
    private _log: LogService,
    private _settings: DeploySettings,
    private _environmentUtil: EnvironmentUtil
  ) {}

  async get(pullNumber: number): Promise<PullRequest> {
    const pullResponse = await this._octokit.rest.pulls.get({
      owner: this._settings.owner,
      repo: this._settings.repo,
      pull_number: pullNumber
    });

    return pullResponse.data;
  }

  async getByCommit(commit: string): Promise<PullRequest> {
    const pullResponse = await this._octokit.rest.repos.listPullRequestsAssociatedWithCommit({
      owner: this._settings.owner,
      repo: this._settings.repo,
      commit_sha: commit
    });

    const pull = pullResponse.data ? pullResponse.data[0] : null;
    return pull as any;
  }

  async listDeployed(): Promise<PullRequest[]> {
    // Get all environments currently in use.
    const response = await this._octokit.rest.pulls.list({
      owner: this._settings.owner,
      repo: this._settings.repo,
      state: 'all',
      sort: 'updated',
      direction: 'desc',
      per_page: PullRequestService.MaxPageSize
    });

    // Check if pull request is already in an environment.
    const pulls = response.data.filter(p =>
      p.labels.some(l => this._settings.environments.some(e => l.name.toLowerCase() === e.name.toLowerCase()))
    );

    return pulls as any as PullRequest[];
  }

  async progressEnvironment(pullRequest: PullRequest, environment: string, retainLocks: boolean): Promise<string[]> {
    const addEnvironments = [environment.toLowerCase(), `${environment.toLowerCase()}-lock`];
    let removeEnvironments: string[];

    this._log.info(
      `Updating pull request ${pullRequest.number} to deployed for ${environment} with retain ${retainLocks}.`
    );

    if (!retainLocks) {
      removeEnvironments = this.getAllEnvironments(pullRequest)
        .map(e => `${e}-lock`)
        .filter(e => !addEnvironments.includes(e));
    } else {
      removeEnvironments = [];
    }
    return await this.updateEnvironments(pullRequest, addEnvironments, removeEnvironments);
  }

  async free(pullRequest: PullRequest, environment: string = null): Promise<string[]> {
    const addEnvironments: string[] = [];
    let removeEnvironments: string[];

    // If no environment is specified, all are freed.
    const environmentName = environment || 'all environments';
    this._log.info(`Freeing pull request ${pullRequest.number} for ${environmentName}.`);

    if (environment) {
      removeEnvironments = [`${environment.toLowerCase()}-lock`];
    } else {
      removeEnvironments = this._settings.environments.map(e => `${e.name.toLowerCase()}-lock`);
    }

    const availableEnvironments = await this.updateEnvironments(pullRequest, addEnvironments, removeEnvironments);
    return availableEnvironments;
  }

  async removeEnvironment(pullRequest: PullRequest, environment: string): Promise<string[]> {
    const addEnvironments: string[] = [];
    let removeEnvironments: string[];

    // If no environment is specified, all are removed.
    const environmentName = environment || 'all environments';
    this._log.info(`Removing environment from pull request ${pullRequest.number} for ${environmentName}.`);

    if (environment) {
      removeEnvironments = [environment.toLowerCase(), `${environment.toLowerCase()}-lock`];
    } else {
      removeEnvironments = this._settings.environments.flatMap(e => [
        e.name.toLowerCase(),
        `${e.name.toLowerCase()}-lock`
      ]);
    }

    const availableEnvironments = await this.updateEnvironments(pullRequest, addEnvironments, removeEnvironments);
    return availableEnvironments;
  }

  getAllEnvironments(pullRequest: PullRequest, environment: string = null, normalized = false): string[] {
    // If no environment is specified, all are listed, otherwise just the environment specified is listed if it exists.
    const environmentName = environment || 'all environments';
    this._log.info(`List environment from pull request ${pullRequest.number} for ${environmentName}.`);
    const environments = this._settings.environments.map(e => e.name.toLowerCase());
    let lockLabels = pullRequest.labels
      .filter(l => l.name.endsWith('-lock'))
      .map(l => l.name.replace(PullRequestService.EnvironmentLockRegex, '').toLowerCase())
      .filter(e => environments.includes(e));

    if (environment) {
      if (!normalized) {
        lockLabels = lockLabels.filter(l => l === environment.toLowerCase());
      } else {
        const found = lockLabels.find(l => l.startsWith(environment));
        lockLabels = found ? [found] : [];
      }
    }

    return lockLabels;
  }

  listWithEnvironment(pulls: PullRequest[], environment: string, filterPullNumber = 0): PullRequest[] {
    const pullsWithEnvironment = pulls.filter(
      p => p.labels.some(l => l.name.toLowerCase() === environment) && p.number !== filterPullNumber
    );
    return pullsWithEnvironment;
  }

  getUsedEnvironments(pulls: PullRequest[], normalizedEnvironment: string, currentPullNumber: number): string[] {
    const usedEnvironments = pulls
      .filter(p => p.number !== currentPullNumber) // Do not filter out the environment if it is the current pull request.
      .flatMap(p => p.labels.filter(l => l.name.endsWith('-lock')).map(l => this._environmentUtil.getFromLock(l.name))) // Check only for lock labels because others can be overridden.
      .filter(l => l.startsWith(normalizedEnvironment));

    return usedEnvironments;
  }

  async updatePullRequestBody(pullNumber: number, body: string): Promise<void> {
    await this._octokit.rest.pulls.update({
      owner: this._settings.owner,
      repo: this._settings.repo,
      pull_number: pullNumber,
      body
    });

    this._log.info(`Updated pull request ${pullNumber} body.`);
  }

  getEnvironment(pullRequest: PullRequest, run: WorkflowRun = null): string {
    let environment = '';

    if (run) {
      const match = run.name.match(PullRequestService.EnvironmentTitleRegex);
      if (match) {
        environment = match.groups['environment'].toLowerCase();
      }
    }

    if (environment) {
      return environment;
    }

    if (!pullRequest || !pullRequest.labels?.length) {
      return environment;
    }

    const lockLabels = this.getAllEnvironments(pullRequest);
    const lowerRelease = this._settings.releaseEnvironment.toLowerCase();
    if (lockLabels.length > 1) {
      // Give weight to release environment, stage, then dev if there are multiple locks.
      environment = _.sortBy(lockLabels, l => {
        let score: number;
        if (l === lowerRelease) {
          score = 1;
        } else if (l.startsWith('stage')) {
          score = 2;
        } else {
          score = 3;
        }

        return score;
      })[0];
    } else if (lockLabels.length === 1) {
      environment = lockLabels[0];
    }

    return environment;
  }

  isRollback(run: WorkflowRun): boolean {
    let rollback = false;

    const match = run.name.match(PullRequestService.EnvironmentTitleRegex);
    if (match) {
      rollback = match.groups['rollback'] === 'ROLLBACK';
    }

    return rollback;
  }

  getRollbackCount(run: WorkflowRun): number {
    let rollbackCount = 1;

    const match = run.name.match(PullRequestService.EnvironmentTitleRegex);
    if (match) {
      const rollbackValue = match.groups['rollbackCount'];
      if (rollbackValue) {
        rollbackCount = parseInt(rollbackValue, 10);
      }
    }

    return rollbackCount;
  }

  getRollbackPullNumber(run: WorkflowRun): number {
    let pullNumber = 0;

    const match = run.name.match(PullRequestService.EnvironmentTitleRegex);
    if (match) {
      const pullNumberValue = match.groups['pullNumber'];
      if (pullNumberValue) {
        pullNumber = parseInt(pullNumberValue, 10);
      }
    }

    return pullNumber;
  }

  getDefaultedEnvironment(pullRequest: PullRequest, environment: string): string {
    if (environment) {
      return environment;
    }

    // Use the correct dev environment already assigned if there is one.
    let result = '';
    const pullEnvironment = this.getEnvironment(pullRequest);
    if (pullEnvironment && pullEnvironment.toLowerCase().startsWith(this._settings.defaultEnvironment.toLowerCase())) {
      result = pullEnvironment;
    } else {
      result = this._settings.defaultEnvironment;
    }

    return result;
  }

  private async updateEnvironments(
    pullRequest: PullRequest,
    addLabels: string[],
    removeLabels: string[]
  ): Promise<string[]> {
    const existingLabels = pullRequest.labels.map(l => l.name);

    let updateLabels = existingLabels.filter(l => !removeLabels.includes(l.toLowerCase()));
    updateLabels = _.uniq(updateLabels.concat(addLabels));

    this._log.debug(`Updating pull ${pullRequest.number} labels:
    Before: ${existingLabels.join(', ')}
    After: ${updateLabels.join(', ')}`);

    const availableEnvironments = _.uniq(
      existingLabels
        .filter(l => removeLabels.includes(`${l.toLowerCase()}-lock`))
        .map(e => this._environmentUtil.normalize(e))
    );

    await this._octokit.rest.issues.setLabels({
      owner: this._settings.owner,
      repo: this._settings.repo,
      issue_number: pullRequest.number,
      labels: updateLabels
    });

    return availableEnvironments;
  }
}
