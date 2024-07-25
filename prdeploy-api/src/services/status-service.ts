import { Lifecycle, scoped } from 'tsyringe';
import { WorkflowRun } from '@octokit/webhooks-types';
import { Octokit } from '@octokit/rest';
import { TemplateService } from './template-service';
import { LogService } from './log-service';
import { PullRequest, RepoSettings, ServiceState } from '@src/models';
import { SlackService } from './slack-service';
import { CheckService } from './check-service';
import cache from 'memory-cache';
import { EnvironmentUtil } from '@src/utils/environment-util';
import { DeployStateService } from './deploy-state-service';
import { PullRequestService } from './pull-request-service';
import _ from 'lodash';

@scoped(Lifecycle.ContainerScoped)
export class StatusService {
  private static readonly BuildNameMaxLength = 25;
  private static readonly PostReleaseMessageDelay = 14400000; // 4h until reposting a release message for the same PR.
  private static readonly ReportedConclusions: (
    | 'success'
    | 'failure'
    | 'neutral'
    | 'cancelled'
    | 'timed_out'
    | 'action_required'
    | 'stale'
    | 'skipped'
  )[] = ['success', 'failure'];

  constructor(
    private _octokit: Octokit,
    private _templateService: TemplateService,
    private _slackService: SlackService,
    private _checkService: CheckService,
    private _deployStateService: DeployStateService,
    private _settings: RepoSettings,
    private _pullRequestService: PullRequestService,
    private _environmentUtil: EnvironmentUtil,
    private _log: LogService
  ) {}

  async postDeployCompleted(pullRequest: PullRequest, run: WorkflowRun): Promise<void> {
    const environment = this._pullRequestService.getEnvironment(pullRequest, run);
    this._log.info(`Posting deployment complete message for ${environment} and pull ${pullRequest.number}.`);

    const environmentSettings = this._environmentUtil.getSettings(environment);
    if (environmentSettings && StatusService.ReportedConclusions.includes(run.conclusion)) {
      const isRollback = this._pullRequestService.isRollback(run);
      const body = await this._templateService.render(
        run.conclusion === 'failure'
          ? 'deploy-failed.md'
          : !isRollback
            ? 'deploy-completed.md'
            : 'rollback-completed.md',
        {
          environment: environmentSettings,
          badge: this._settings.badge,
          owner: this._settings.owner,
          repo: this._settings.repo,
          run
        }
      );

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

      const builds = await this._checkService.listBuilds(this._settings.owner, this._settings.repo, pullRequest);

      // Update what was deployed.
      const deployedServices = builds.map(
        b =>
          ({
            name: b.name,
            runId: b.runId,
            version: b.version
          }) as ServiceState
      );

      if (!isRollback) {
        // If this was successful, assume a sync has completed as well.
        await this._deployStateService.sync(environment);
        await this._deployStateService.update(environment, pullRequest.number, deployedServices);
      } else {
        const rollbackCount = this._pullRequestService.getRollbackCount(run);
        const deployState = await this._deployStateService.get('stable', rollbackCount - 1);
        await this._deployStateService.update(environment, deployState.pullNumber, deployState.services);
      }

      // Pad build names to line up for display.
      for (const build of builds) {
        build.name = build.name.padEnd(StatusService.BuildNameMaxLength);
      }

      const status = run.conclusion === 'failure' ? '`FAILED`' : 'complete';

      // Ensure alphabetical order.
      const orderedBuilds = _.orderBy(builds, b => b.name);
      let json = await this._templateService.render('deploy-completed.json', {
        environment: environmentSettings,
        owner: this._settings.owner,
        repo: this._settings.repo,
        run,
        pull: pullRequest,
        slackUser,
        status,
        builds: orderedBuilds,
        isRollback
      });

      await this._slackService.postMessage('deploy', json);

      // Send release message when it is for a release environment.
      if (environment.toLocaleLowerCase() === this._settings.releaseEnvironment.toLowerCase()) {
        // Only post message for release if it is the first one for this pull request today.
        this._log.info(`The pull request ${pullRequest.number} release message posting to slack.`);
        const cacheKey = this.getReleasedCacheKey(pullRequest.number);
        if (cache.get(cacheKey) !== true) {
          const slackPullBody = this._slackService.translateGitHubMarkdown(pullRequest.body);
          json = await this._templateService.render('deploy-released.json', {
            environment: environmentSettings,
            owner: this._settings.owner,
            repo: this._settings.repo,
            run,
            pull: pullRequest,
            slackUser,
            slackPullBody,
            status,
            builds: orderedBuilds,
            isRollback
          });
          await this._slackService.postMessage('release', json);
          cache.put(cacheKey, true, StatusService.PostReleaseMessageDelay, (key: string) => {
            this._log.debug(`Release ${key} removed from cache.`);
          });
        } else {
          this._log.info(`The pull request ${pullRequest.number} release has already been messaged.`);
        }
      }
    } else if (run.conclusion) {
      this._log.warn(`No deploy environment found for ${environment}.`);
    }
  }

  async postDeployStarted(pullRequest: PullRequest, run: WorkflowRun): Promise<void> {
    // We need to retrieve the workflow run to get the correct title as it was not set yet.
    const response = await this._octokit.rest.actions.getWorkflowRun({
      owner: this._settings.owner,
      repo: this._settings.repo,
      run_id: run.id
    });
    const updatedRun = response.data as WorkflowRun;
    const environment = this._pullRequestService.getEnvironment(pullRequest, updatedRun);
    this._log.info(`Posting deployment started message for ${environment} and pull ${pullRequest.number}.
    Run: ${updatedRun.name}
`);

    const environmentSettings = this._environmentUtil.getSettings(environment);
    if (environmentSettings) {
      const isRollback = this._pullRequestService.isRollback(updatedRun);
      const body = await this._templateService.render(!isRollback ? 'deploy-started.md' : 'rollback-started.md', {
        environment: environmentSettings,
        badge: this._settings.badge,
        owner: this._settings.owner,
        repo: this._settings.repo,
        run
      });

      await this._octokit.rest.issues.createComment({
        owner: this._settings.owner,
        repo: this._settings.repo,
        issue_number: pullRequest.number,
        body
      });

      if (environmentSettings.requireApproval) {
        const userResponse = await this._octokit.rest.users.getByUsername({
          username: pullRequest.user.login
        });
        const slackUser = await this._slackService.lookupUser(userResponse.data);

        const slackBody = await this._templateService.render('environment-approval-required.json', {
          environment: environmentSettings,
          owner: this._settings.owner,
          repo: this._settings.repo,
          run,
          slackUser,
          isRollback
        });

        await this._slackService.postMessage('deploy', slackBody);
      }
    } else {
      this._log.warn(`No deploy environment found for ${environment}.`);
    }
  }

  private getReleasedCacheKey(pullNumber: number): string {
    return `${this._settings.owner}:${this._settings.repo}:released:${pullNumber}`;
  }
}
