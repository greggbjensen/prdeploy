import { Repository } from '@octokit/webhooks-types';
import { PullRequest, DeploySettings, ServiceState } from '@src/models';
import { Lifecycle, inject, scoped } from 'tsyringe';
import { LogService } from './log-service';
import { Octokit } from '@octokit/rest';
import { TemplateService } from './template-service';
import _ from 'lodash';
import { SlackService } from './slack-service';
import { REPOSITORY } from '@src/injection-tokens';
import { CheckService } from './check-service';
import { EnvironmentUtil } from '@src/utils/environment-util';
import { ParameterService } from './parameter-service';
import { PullRequestService } from './pull-request-service';
import { DeployStateService } from './deploy-state-service';

@scoped(Lifecycle.ContainerScoped)
export class DeployService {
  constructor(
    private _octokit: Octokit,
    private _checkService: CheckService,
    private _templateService: TemplateService,
    private _slackService: SlackService,
    private _parameterService: ParameterService,
    private _pullRequestService: PullRequestService,
    private _deployStateService: DeployStateService,
    private _log: LogService,
    private _settings: DeploySettings,
    private _environmentUtil: EnvironmentUtil,
    @inject(REPOSITORY) private _repository: Repository
  ) {}

  async tryRetriggerDeploy(pullRequest: PullRequest): Promise<void> {
    const incompleteChecks = await this._checkService.listIncompletePullChecks(pullRequest);
    if (incompleteChecks.length === 0) {
      let environment = this._pullRequestService.getEnvironment(pullRequest);
      if (environment) {
        environment = environment.toLowerCase();
        this._log.info(`Pull request ${pullRequest.number} already deployed to ${environment}, redeploying.`);

        const environmentSettings = this._environmentUtil.getSettings(environment);
        const body = await this._templateService.render('pull-request-checks-redeploy.md', {
          owner: this._settings.owner,
          repo: this._settings.repo,
          environment: environmentSettings,
          badge: this._settings.badge,
          deployManagerSiteUrl: this._settings.prdeployPortalUrl
        });

        await this._octokit.rest.issues.createComment({
          owner: this._settings.owner,
          repo: this._settings.repo,
          issue_number: pullRequest.number,
          body
        });

        await this.triggerDeploy(pullRequest, environment, true);
      }
    }
  }

  async requestDeploy(
    pullRequest: PullRequest,
    environment: string,
    commentId: number,
    force: boolean,
    retainLocks: boolean
  ): Promise<void> {
    await this.addCommentReaction(commentId, 'rocket');

    const defaultedEnvironment = this._pullRequestService.getDefaultedEnvironment(pullRequest, environment);
    if (force) {
      this._log.info(`Force deploying PR ${pullRequest.number} to ${defaultedEnvironment}.`);
      await this.triggerDeploy(pullRequest, defaultedEnvironment, retainLocks);
    } else {
      const normalizedEnvironment = this._environmentUtil.normalize(defaultedEnvironment);
      this._log.info(`Trying queue deploy PR ${pullRequest.number} to ${normalizedEnvironment}.`);

      const availableEnvironment = await this.findAvailableEnvironmentForQueue(
        normalizedEnvironment,
        pullRequest.number
      );
      if (availableEnvironment) {
        await this.triggerDeploy(pullRequest, availableEnvironment, retainLocks);
      } else {
        await this.enquePullRequest(pullRequest, normalizedEnvironment, retainLocks);
      }
    }
  }

  async releasePullRequest(pullRequest: PullRequest): Promise<void> {
    this._log.info(`Freeing pull request ${pullRequest.number} after close.`);

    const availableEnvironments = await this._pullRequestService.free(pullRequest);
    await this.removePullRequestFromQueues(pullRequest);
    await this.dequeuePullRequests(availableEnvironments);

    const environment = this._pullRequestService.getEnvironment(pullRequest);
    if (pullRequest.merged && environment === this._settings.releaseEnvironment) {
      // Promote deploy state from released to stable.
      const deployState = await this._deployStateService.get(environment);
      await this._deployStateService.update('stable', pullRequest.number, deployState.services);
    }
  }

  async rollbackDeploy(
    pullRequest: PullRequest,
    environment: string,
    commentId: number,
    rollbackCount: number
  ): Promise<void> {
    await this.addCommentReaction(commentId, 'confused');

    // If no environment is specified, rollback all assigned environments.
    const rollbackEnvironments = this._pullRequestService.getAllEnvironments(pullRequest, environment);
    if (rollbackEnvironments.length === 1) {
      this._log.info('Rolling back pull request.');

      const rollbackEnvironment = rollbackEnvironments[0];
      await this._pullRequestService.free(pullRequest, rollbackEnvironment);
      await this.triggerDeploy(pullRequest, rollbackEnvironment, false, true, rollbackCount);
    } else {
      this._log.warn('Cannot rollback multiple environments.');
      await this.postDeployFailed(
        pullRequest.number,
        environment,
        true,
        'You cannot rollback multiple environments.  You must specify an environment to rollback.'
      );
    }
  }

  async freeDeploy(pullRequest: PullRequest, environment: string, commentId: number): Promise<void> {
    await this.addCommentReaction(commentId, 'eyes');

    const availableEnvironments = await this._pullRequestService.free(pullRequest, environment);
    await this.removePullRequestFromQueues(pullRequest);
    await this.dequeuePullRequests(availableEnvironments);
  }

  async addServices(pullRequest: PullRequest, services: string[], commentId: number): Promise<void> {
    await this.addCommentReaction(commentId, '+1');

    // Add a file to the service folder and append a datestamp and branch name to trigger a build.
    const servicesNotFound: string[] = [];
    for (const service of services) {
      // Do not run in parallel to avoid GitHub limits.
      const serviceLower = service.toLowerCase();
      const serviceSettings = this._settings.services.find(s => s.name.toLowerCase() === serviceLower);
      if (!serviceSettings) {
        servicesNotFound.push(service);
        continue;
      }

      const path = `${serviceSettings.path}/.deploy`;

      // Get content to update it.
      let content = '';
      let sha: string = undefined;
      let isUpdate = true;
      try {
        const response = await this._octokit.rest.repos.getContent({
          owner: this._settings.owner,
          repo: this._settings.repo,
          path,
          ref: this._settings.defaultBranch
        });

        const data = response?.data as any;
        if (data.content) {
          content = Buffer.from(data.content, 'base64').toString();
        }

        // We have to have the branch sha for an update.
        isUpdate = content.length > 0;
        if (isUpdate) {
          const branchResponse = await this._octokit.rest.repos.getContent({
            owner: this._settings.owner,
            repo: this._settings.repo,
            path,
            ref: pullRequest.head.ref
          });

          const branchData = branchResponse?.data as any;
          sha = branchData.sha;
        }
      } catch {
        // Do nothing.
        isUpdate = false;
      }

      content = content.trim();
      if (content.length > 0) {
        content += '\n';
      }

      const dateValue = new Date().toISOString();
      content += `${dateValue} ${pullRequest.head.ref}\n`;
      const base64Content = Buffer.from(content).toString('base64');

      this._log.info(`${isUpdate ? 'Updating' : 'Creating'} file to add to deploy: ${path}`);

      await this._octokit.rest.repos.createOrUpdateFileContents({
        owner: this._settings.owner,
        repo: this._settings.repo,
        path,
        message: `Adding ${service} to pull request build.`,
        content: base64Content,
        branch: pullRequest.head.ref,
        sha,
        committer: {
          name: 'Gregg B. Jensen',
          email: 'greggbjensen@users.noreply.github.com'
        }
      });
    }

    if (servicesNotFound.length > 0) {
      const body = await this._templateService.render('services-not-found.md', {
        badge: this._settings.badge,
        services: servicesNotFound
      });

      await this._octokit.rest.issues.createComment({
        owner: this._settings.owner,
        repo: this._settings.repo,
        issue_number: pullRequest.number,
        body
      });
    }
  }

  private async removePullRequestFromQueues(pullRequest: PullRequest, keepEnvironment = ''): Promise<void> {
    const queueGroups = _.groupBy(this._settings.environments, e => e.queue);
    const promises = Object.keys(queueGroups).map(async key => {
      const group = queueGroups[key];
      const normalizedEnvironment = this._environmentUtil.normalize(group[0].name);
      if (normalizedEnvironment === keepEnvironment) {
        return;
      }

      const pullNumbers = await this.getQueuePullNumbers(normalizedEnvironment);
      const updatedPullNumbers = pullNumbers.filter(n => n !== pullRequest.number);

      // If queue has changed, update it.
      if (updatedPullNumbers.length !== pullNumbers.length) {
        this._log.info(`Removing pull request ${pullRequest.number} from ${normalizedEnvironment} queue.`);
        await this.setQueuePullNumbers(normalizedEnvironment, updatedPullNumbers);
      }
    });

    await Promise.all(promises);
  }

  private async enquePullRequest(
    pullRequest: PullRequest,
    normalizedEnvironment: string,
    retainLocks: boolean
  ): Promise<void> {
    const pullNumber = pullRequest.number;
    this._log.info(`Enquing pull request ${pullNumber} for ${normalizedEnvironment}.`);

    let queuePosition = 0;
    let updateQueue = true;
    let alreadyInQueue = false;
    const queuePullNumbers = await this.getQueuePullNumbers(normalizedEnvironment);
    if (queuePullNumbers.length === 0) {
      // Nothing in the queue yet.
      queuePosition = 1;
      queuePullNumbers.push(pullNumber);
    } else {
      const index = queuePullNumbers.findIndex(n => n === pullNumber);
      if (index === -1) {
        queuePosition = queuePullNumbers.length + 1;
        queuePullNumbers.push(pullNumber);
      } else {
        queuePosition = index + 1;
        this._log.info(`Pull request already in queue at postion ${queuePosition}.`);
        updateQueue = false;
        alreadyInQueue = true;
      }
    }

    if (updateQueue) {
      await this.setQueuePullNumbers(normalizedEnvironment, queuePullNumbers);

      // Make sure the pull request is not in any other queues.
      await this.removePullRequestFromQueues(pullRequest, normalizedEnvironment);
    }

    if (!retainLocks) {
      // Unlock environments since it is in the queue now for the next environment.
      const availableEnvironments = await this._pullRequestService.free(pullRequest);
      await this.dequeuePullRequests(availableEnvironments);
    }

    const queueTable = this._templateService.renderQueueTable(
      this._settings.owner,
      this._settings.repo,
      normalizedEnvironment,
      queuePullNumbers,
      this._repository.html_url,
      this._settings.prdeployPortalUrl
    );

    const environmentSettings = this._environmentUtil.getSettings(normalizedEnvironment);
    const body = await this._templateService.render('pull-request-enqueued.md', {
      owner: this._settings.owner,
      repo: this._settings.repo,
      environment: environmentSettings,
      deployManagerSiteUrl: this._settings.prdeployPortalUrl,
      queuePosition,
      queueTable,
      alreadyInQueue
    });

    await this._octokit.rest.issues.createComment({
      owner: this._settings.owner,
      repo: this._settings.repo,
      issue_number: pullNumber,
      body
    });
  }

  private async triggerDeploy(
    pullRequest: PullRequest,
    environment: string,
    retainLocks: boolean,
    rollback = false,
    rollbackCount = 1
  ): Promise<void> {
    this._log.info(`Triggering deployment for ${pullRequest.number} in ${environment}.  Rollback: ${rollback}`);

    let deployPullRequest = pullRequest;
    if (rollback) {
      const deployState = await this._deployStateService.get('stable', rollbackCount - 1);
      this._log.info(`Rolling back to pull request ${deployState.pullNumber} for ${environment}.`);
      if (!deployState.pullNumber) {
        await this.postDeployFailed(
          pullRequest.number,
          environment,
          rollback,
          `Could not find stable state with a previous pull request number.  You must have at least one successful deploy.`
        );

        return;
      }

      deployPullRequest = await this._pullRequestService.get(deployState.pullNumber);
    }

    this._log.debug(`Removing pull request from queues for ${environment}.`);
    const availableEnvironments = await this._pullRequestService.progressEnvironment(
      deployPullRequest,
      environment,
      retainLocks
    );
    await this.removePullRequestFromQueues(pullRequest);

    // Remove any other pull requests in this environment.
    this._log.debug(`Removing other pull requests in ${environment}.`);
    const pulls = await this._pullRequestService.listDeployed();
    const pullsWithEnvironment = this._pullRequestService.listWithEnvironment(
      pulls,
      environment,
      deployPullRequest.number
    );
    for (const environmentPull of pullsWithEnvironment) {
      this._log.debug(`Removing pull request ${environmentPull.number} from ${environment}.`);
      await this._pullRequestService.removeEnvironment(environmentPull, environment);
    }

    if (!rollback) {
      await this.dequeuePullRequests(availableEnvironments);
    }

    // Allow labels to be set even if checks fail, so it can be retriggered.
    if (!(await this._checkService.verifyDeployReady(pullRequest, environment))) {
      return;
    }

    this._log.debug(`Building pull request title.`);
    const response = await this._octokit.rest.pulls.listCommits({
      owner: this._settings.owner,
      repo: this._settings.repo,
      pull_number: pullRequest.number,
      per_page: 1
    });

    const firstCommitLine = response.data[0].commit.message.split('\n');
    const environmentText = rollback
      ? rollbackCount === 1
        ? `${environment} ROLLBACK #${pullRequest.number}`
        : `${environment} ROLLBACK #${pullRequest.number} ${rollbackCount}`
      : environment;
    const deployName = `[${environmentText}] ${pullRequest.head.ref}: ${firstCommitLine}`;

    await this.triggerDeployWorkflow(deployName, environment, pullRequest, deployPullRequest, rollback, rollbackCount);
  }

  private async triggerDeployWorkflow(
    deployName: string,
    environment: string,
    triggeringPullRequest: PullRequest,
    deployPullRequest: PullRequest,
    rollback: boolean,
    rollbackCount: number
  ): Promise<void> {
    this._log.debug(`Triggering deploy: ${deployName}`);

    // If PR brand does not exit, use default.
    // This is for rollback on closed PRs.
    let branch = deployPullRequest?.head?.ref;
    if (branch) {
      try {
        const response = await this._octokit.rest.repos.getBranch({
          owner: this._settings.owner,
          repo: this._settings.repo,
          branch
        });

        if (response.status !== 200) {
          branch = this._settings.defaultBranch;
        }
      } catch (error) {
        branch = this._settings.defaultBranch;
      }
    } else {
      branch = this._settings.defaultBranch;
    }

    let deployServices: ServiceState[];
    let syncServices: ServiceState[];

    if (!rollback) {
      if (deployPullRequest) {
        const builds = await this._checkService.listBuilds(
          this._settings.owner,
          this._settings.repo,
          deployPullRequest
        );
        deployServices = builds.map(
          b =>
            ({
              name: b.name,
              runId: b.runId,
              version: b.version
            }) as ServiceState
        );
      } else {
        deployServices = [];
      }

      // Stable is a special environment for merged production releases.
      const diff = await this._deployStateService.diff('stable', environment);

      // Filter out things already being deployed.
      syncServices = diff.services.filter(s => deployServices.every(d => d.name !== s.name));
    } else {
      // If this is a rollback we want to show all differences as deploys.
      const diff = await this._deployStateService.diff('stable', environment, rollbackCount - 1);

      const environmentSettings = this._environmentUtil.getSettings(environment);
      const excludedServices = diff.services
        .filter(s => environmentSettings.excludeFromRollback.includes(s.name))
        .map(s => s.name);
      this._log.info(`Services excluded from rollback: ${excludedServices.join(', ')}`);

      deployServices = diff.services.filter(s => !environmentSettings.excludeFromRollback.includes(s.name));
      syncServices = [];
    }

    const deployRunIds = deployServices.map(s => s.runId);
    const syncRunIds = syncServices.map(s => s.runId);
    this._log.info(`Triggering ${this._settings.deployWorkflow} with:
    Deploy Run IDs: ${deployRunIds}
    Sync Run IDs:   ${syncRunIds}
`);

    if (deployRunIds.length === 0) {
      await this.postNoDeploymentsFound(triggeringPullRequest, environment);
      return;
    }

    try {
      await this._octokit.rest.actions.createWorkflowDispatch({
        owner: this._settings.owner,
        repo: this._settings.repo,
        workflow_id: this._settings.deployWorkflow,
        ref: branch,
        inputs: {
          pull_number: deployPullRequest?.number?.toString(),
          environment,
          deploy_name: deployName,
          deploy_run_ids: deployRunIds.join(','),
          sync_run_ids: syncRunIds.join(',')
        }
      });
    } catch (error: any) {
      await this.postDeployFailed(triggeringPullRequest.number, environment, rollback, error?.message);
    }
  }

  private async postDeployFailed(
    pullNumber: number,
    environment: string,
    rollback: boolean,
    message: string
  ): Promise<void> {
    const environmentSettings = this._environmentUtil.getSettings(environment);
    const body = await this._templateService.render('deploy-failed.md', {
      environment: environmentSettings,
      badge: this._settings.badge,
      owner: this._settings.owner,
      repo: this._settings.repo,
      run: {},
      rollback,
      message
    });

    await this._octokit.rest.issues.createComment({
      owner: this._settings.owner,
      repo: this._settings.repo,
      issue_number: pullNumber,
      body
    });
  }

  private async postNoDeploymentsFound(pullRequest: PullRequest, environment: string): Promise<void> {
    const environmentSettings = this._environmentUtil.getSettings(environment);

    const body = await this._templateService.render('deploy-nothing-found.md', {
      environment: environmentSettings,
      badge: this._settings.badge,
      owner: this._settings.owner,
      repo: this._settings.repo
    });

    await this._octokit.rest.issues.createComment({
      owner: this._settings.owner,
      repo: this._settings.repo,
      issue_number: pullRequest.number,
      body
    });

    const userResponse = await this._octokit.rest.users.getByUsername({
      username: pullRequest.user.login
    });
    const slackUser = await this._slackService.lookupUser(userResponse.data);

    const json = await this._templateService.render('deploy-nothing-found.json', {
      environment: environmentSettings,
      owner: this._settings.owner,
      repo: this._settings.repo,
      pull: pullRequest,
      slackUser
    });

    await this._slackService.postMessage('deployUrl', json);
  }

  private async addCommentReaction(
    commentId: number,
    reaction: '+1' | '-1' | 'laugh' | 'confused' | 'heart' | 'hooray' | 'rocket' | 'eyes'
  ): Promise<void> {
    await this._octokit.rest.reactions.createForIssueComment({
      owner: this._settings.owner,
      repo: this._settings.repo,
      comment_id: commentId,
      content: reaction
    });
  }

  private async findAvailableEnvironmentForQueue(normalizedEnvironment: string, pullNumber: number): Promise<string> {
    let availableEnvironment = '';
    const pulls = await this._pullRequestService.listDeployed();

    const existingPull = pulls.find(p => p.number === pullNumber);
    if (existingPull) {
      const existingEnvironment = this._pullRequestService.getAllEnvironments(
        existingPull,
        normalizedEnvironment,
        true
      );
      if (existingEnvironment.length > 0) {
        availableEnvironment = existingEnvironment[0];
        this._log.info(`Pull request ${pullNumber} is already in ${availableEnvironment}, reusing.`);
      }
    }

    // Find new available environment.
    if (!availableEnvironment) {
      const usedEnvironments = this._pullRequestService.getUsedEnvironments(pulls, normalizedEnvironment, pullNumber);

      // Find all environments associated with the same queue.
      const environmentSettings = this._environmentUtil.getSettings(normalizedEnvironment);
      const queueEnvironments = this._settings.environments
        .filter(e => e.queue.toLowerCase() === environmentSettings.queue.toLowerCase())
        .map(e => e.name.toLowerCase());
      const availableEnvironments = queueEnvironments.filter(n => !usedEnvironments.includes(n));

      this._log.info(
        `Environments ${environmentSettings.queue} queue:
    Searched: ${queueEnvironments.join(', ')}
    Available: ${availableEnvironments.join(', ')}`
      );

      if (availableEnvironments.length > 0) {
        availableEnvironment = availableEnvironments[0];
      }
    }

    return availableEnvironment;
  }

  private async dequeuePullRequests(normalizedEnvironments: string[]): Promise<void> {
    if (normalizedEnvironments.length === 0) {
      return;
    }

    const promises = normalizedEnvironments.map(async e => {
      await this.dequeuePullRequest(e);
    });

    await Promise.all(promises);
  }

  private async dequeuePullRequest(normalizedEnvironment: string): Promise<void> {
    const pullNumbers = await this.getQueuePullNumbers(normalizedEnvironment);
    let nextPullRequest: PullRequest;
    if (pullNumbers.length > 0) {
      const response = await this._octokit.rest.pulls.get({
        owner: this._settings.owner,
        repo: this._settings.repo,
        pull_number: pullNumbers[0]
      });

      nextPullRequest = response.data;
      await this.triggerDeploy(nextPullRequest, normalizedEnvironment, true);
    }

    await this.postEnvironmentAvailableMessage(normalizedEnvironment, nextPullRequest);
  }

  private async postEnvironmentAvailableMessage(environment: string, takenPullRequest: PullRequest): Promise<void> {
    const environmentSettings = this._environmentUtil.getSettings(environment);
    const takenMessage = takenPullRequest
      ? ` but taken from queue by <${takenPullRequest.html_url}|#${takenPullRequest.number}>`
      : '';

    const json = await this._templateService.render('environment-available.json', {
      environment: environmentSettings,
      takenMessage
    });
    await this._slackService.postMessage('deployUrl', json);
  }

  private async getQueuePullNumbers(normalizedEnvironment: string): Promise<number[]> {
    let pullNumbers: number[] = [];

    const environmentSettings = this._environmentUtil.getSettings(normalizedEnvironment);
    const queueVariable = environmentSettings.queue;
    pullNumbers = await this._parameterService.getOrCreateObject<number[]>(queueVariable, []);
    return pullNumbers;
  }

  private async setQueuePullNumbers(normalizedEnvironment: string, pullNumbers: number[]): Promise<void> {
    const environmentSettings = this._environmentUtil.getSettings(normalizedEnvironment);
    const queueVariable = environmentSettings.queue;

    await this._parameterService.setObject(queueVariable, pullNumbers);

    this._log.info(`Queue for ${normalizedEnvironment} updated: ${pullNumbers.join(', ')}`);
  }
}
