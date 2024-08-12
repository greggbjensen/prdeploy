import { Octokit } from '@octokit/rest';
import { Build } from '../models/build';
import { BuildDetails } from '../models/build-details';
import AdmZip from 'adm-zip';
import _ from 'lodash';
import { LogService } from './log-service';
import { Lifecycle, scoped } from 'tsyringe';
import { CheckRun, EnvironmentSettings, PullRequest, DeploySettings } from '@src/models';
import { TemplateService } from './template-service';
import { ArrayUtil, EnvironmentUtil } from '@src/utils';
import path from 'path';
import { SlackService } from './slack-service';
import { PullRequestService } from './pull-request-service';

@scoped(Lifecycle.ContainerScoped)
export class CheckService {
  private static readonly RunRegex = /\/runs\/([0-9]+)\/jobs?\//i;
  private static readonly ValidMergableStates = ['clean', 'behind', 'blocked'];
  constructor(
    private _octokit: Octokit,
    private _templateService: TemplateService,
    private _slackService: SlackService,
    private _pullRequestService: PullRequestService,
    private _settings: DeploySettings,
    private _environmentUtil: EnvironmentUtil,
    private _log: LogService
  ) {}

  async verifyDeployReady(pullRequest: PullRequest, environment = ''): Promise<boolean> {
    let valid = true;

    const deployEnviroment = this._pullRequestService.getDefaultedEnvironment(pullRequest, environment);
    this._log.debug(`Checking if pull ${pullRequest.number} is ready for deploy to ${deployEnviroment}.`);

    const environmentSettings = this._environmentUtil.getSettings(deployEnviroment);
    if (environmentSettings.requireBranchUpToDate) {
      valid = await this.verifyBranchUpToDate(pullRequest, environmentSettings);
    }

    if (valid) {
      valid = await this.verifyPullChecksComplete(pullRequest, environmentSettings);
    }

    return valid;
  }

  async listBuilds(
    owner: string,
    repo: string,
    pullRequest: PullRequest,
    overrideBuildRunIds: number[] = []
  ): Promise<Build[]> {
    const workflowRegex = new RegExp(this._settings.builds.workflowPattern, 'i');

    if (overrideBuildRunIds && overrideBuildRunIds.length > 0) {
      const overrideBuilds = await Promise.all(
        overrideBuildRunIds.map(async r => await this.extractBuild(owner, repo, workflowRegex, r))
      );

      return overrideBuilds;
    }

    this._log.info(`Listing checks for ref: ${pullRequest.head.ref}`);
    const result = await this._octokit.rest.checks.listForRef({
      owner,
      repo,
      ref: pullRequest.head.ref,
      per_page: 100
    });

    const checkRegex = new RegExp(this._settings.builds.checkPattern, 'i');
    const buildChecks = result.data.check_runs.filter(c => checkRegex.test(c.name));
    const incompleteChecks = buildChecks.filter(c => c.status !== 'completed').map(c => `${c.name} (${c.status})`);
    if (incompleteChecks.length > 0) {
      this._log.warn(`The following checks are incomplete: ${incompleteChecks.join(', ')}`);
      this._log.warn('Please wait for the checks to pass and try again.');
    }

    // This could be parallel for some batches but we don't want to go over our GitHub API limit.
    let builds = await Promise.all(
      buildChecks.map(async c => {
        const runId = this.parseRunId(c.details_url);
        return await this.extractBuild(owner, repo, workflowRegex, runId);
      })
    );

    // Remove any duplicate builds.
    builds = _.uniqBy(builds, 'name');
    this._log.debug(`Found builds:\n${JSON.stringify(builds)}`);

    return builds;
  }

  async listIncompletePullChecks(pullRequest: PullRequest): Promise<CheckRun[]> {
    this._log.info(`Listing checks for ref: ${pullRequest.head.ref}`);
    const result = await this._octokit.rest.checks.listForRef({
      owner: this._settings.owner,
      repo: this._settings.repo,
      ref: pullRequest.head.ref,
      per_page: 100
    });

    let incompleteChecks = result.data.check_runs.filter(c => c.status !== 'completed' || c.conclusion === 'failure');

    if (incompleteChecks.length === 0) {
      this._log.debug(`All checks are complete.`);
      return incompleteChecks;
    }

    this._log.debug(`Incomplete checks: ${incompleteChecks.map(c => c.name).join(', ')}`);

    // We need to exclude any workflows that could be triggered by PR Deploy directly.
    const excludedWorkflows = _.uniq(
      this._settings.environments
        .filter(e => e.automationTest.enabled && e.automationTest.workflow)
        .map(e => e.automationTest.workflow.toLowerCase())
    );
    excludedWorkflows.push(this._settings.deployWorkflow.toLowerCase());
    this._log.debug(`Filtering out any checks that are from workflows: ${excludedWorkflows.join(', ')}`);

    const retainedChecks = await ArrayUtil.asyncFilter(incompleteChecks, async c => {
      let keep = true;

      try {
        const runId = this.parseRunId(c.details_url);
        const response = await this._octokit.rest.actions.getWorkflowRun({
          owner: this._settings.owner,
          repo: this._settings.repo,
          run_id: runId
        });

        const checkWorkflow = path.basename(response.data.path).toLowerCase();
        keep = !excludedWorkflows.includes(checkWorkflow);
        this._log.debug(`Keep ${c.name} (${checkWorkflow}): ${keep}`);
      } catch (error) {
        this._log.warn(`Unable to retrieve workflow for check: ${c.html_url}`);
      }

      return keep;
    });

    this._log.debug(`Filtered checks: ${retainedChecks.map(c => c.name).join(', ')}
            `);

    incompleteChecks = retainedChecks;

    return incompleteChecks;
  }

  private parseRunId(url: string): number {
    const [, runIdValue] = CheckService.RunRegex.exec(url) ?? [null, '0'];
    const runId = parseInt(runIdValue, 10);

    return runId;
  }

  private async verifyPullChecksComplete(
    pullRequest: PullRequest,
    environmentSettings: EnvironmentSettings
  ): Promise<boolean> {
    const incompleteChecks = await this.listIncompletePullChecks(pullRequest);

    const valid = incompleteChecks.length === 0;
    if (!valid) {
      this._log.info(`Pull request ${pullRequest.number} checks are not complete.`);
      const body = await this._templateService.render('pull-request-checks-incomplete.md', {
        owner: this._settings.owner,
        repo: this._settings.repo,
        environment: environmentSettings,
        badge: this._settings.badge,
        deployManagerSiteUrl: this._settings.prdeployPortalUrl,
        incompleteChecks
      });

      await this._octokit.rest.issues.createComment({
        owner: this._settings.owner,
        repo: this._settings.repo,
        issue_number: pullRequest.number,
        body
      });
    }

    return valid;
  }

  private async verifyBranchUpToDate(
    pullRequest: PullRequest,
    environmentSettings: EnvironmentSettings
  ): Promise<boolean> {
    const compare = await this._octokit.rest.repos.compareCommits({
      owner: this._settings.owner,
      repo: this._settings.repo,
      base: pullRequest.base.ref,
      head: pullRequest.head.ref
    });

    let pull = pullRequest;
    let valid = compare.data.behind_by === 0;
    if (!valid) {
      this._log.info(`Branch ${pull.head.ref} is behind by ${compare.data.behind_by} commits.
    Mergeable: ${pull.mergeable}
    State:     ${pull.mergeable_state}
`);
      valid = false;
      let isUpdatable = await this.tryUpdateBranchWithMain(pull, environmentSettings);
      if (!isUpdatable) {
        this._log.debug(`Attempting to get latest pull request merge status.`);
        pull = await this._pullRequestService.get(pull.number);
        isUpdatable = await this.tryUpdateBranchWithMain(pull, environmentSettings);
      }

      if (!isUpdatable) {
        this._log.warn(`This branch is out-of-date with the base branch and there are merge conflicts.`);

        const body = await this._templateService.render('pull-request-merge-conflicts.md', {
          owner: this._settings.owner,
          repo: this._settings.repo,
          environment: environmentSettings,
          badge: this._settings.badge,
          deployManagerSiteUrl: this._settings.prdeployPortalUrl
        });

        await this._octokit.rest.issues.createComment({
          owner: this._settings.owner,
          repo: this._settings.repo,
          issue_number: pull.number,
          body
        });

        const userResponse = await this._octokit.rest.users.getByUsername({
          username: pull.user.login
        });
        const slackUser = await this._slackService.lookupUser(userResponse.data);

        const slackBody = await this._templateService.render('pull-request-merge-conflicts.json', {
          environment: environmentSettings,
          owner: this._settings.owner,
          repo: this._settings.repo,
          pull,
          slackUser
        });

        await this._slackService.postMessage('deployUrl', slackBody);
      }
    }

    return valid;
  }

  private async tryUpdateBranchWithMain(
    pullRequest: PullRequest,
    environmentSettings: EnvironmentSettings
  ): Promise<boolean> {
    if (!pullRequest.mergeable && !CheckService.ValidMergableStates.includes(pullRequest.mergeable_state)) {
      return false;
    }

    this._log.info(`Updating branch with main.`);
    const updateResponse = await this._octokit.rest.pulls.updateBranch({
      owner: this._settings.owner,
      repo: this._settings.repo,
      pull_number: pullRequest.number
    });

    const updateMessage = updateResponse.data.message;
    const body = await this._templateService.render('pull-request-updated.md', {
      owner: this._settings.owner,
      repo: this._settings.repo,
      environment: environmentSettings,
      badge: this._settings.badge,
      deployManagerSiteUrl: this._settings.prdeployPortalUrl,
      updateMessage
    });

    await this._octokit.rest.issues.createComment({
      owner: this._settings.owner,
      repo: this._settings.repo,
      issue_number: pullRequest.number,
      body
    });

    return true;
  }

  private async extractBuild(owner: string, repo: string, workflowRegex: RegExp, runId: number): Promise<Build> {
    this._log.debug(`Getting the workflow run with ID: ${runId}`);
    const result = await this._octokit.rest.actions.getWorkflowRun({
      owner,
      repo,
      run_id: runId
    });

    const workflowPath = result.data.path;
    const title = result.data.display_title;
    const url = result.data.html_url;
    let [, name] = workflowRegex.exec(workflowPath) ?? [null, ''];
    if (!name) {
      name = workflowPath;
      this._log.warn(`workflow_regex ${workflowRegex} did not match workflow path: ${workflowPath}`);
    }

    const version = await this.extractVersion(owner, repo, runId);
    this._log.debug(`Found version: ${version}`);

    return {
      runId,
      name,
      version,
      title,
      url
    };
  }

  private async extractVersion(owner: string, repo: string, runId: number): Promise<string> {
    let version = '';
    try {
      // SourceRef: https://github.com/dawidd6/action-download-artifact/blob/master/main.js
      this._log.debug(`Listing artifacts from Run ID: ${runId} `);
      const response = await this._octokit.rest.actions.listWorkflowRunArtifacts({
        owner,
        repo,
        run_id: runId
      });

      const detailsArtifact = response.data.artifacts.find(a => a.name === 'build-details');
      if (!detailsArtifact) {
        throw new Error('Artifact build-details not found on triggering workflow.');
      }

      this._log.debug(`Getting artifact with ID: ${detailsArtifact.id}`);
      const zip = await this._octokit.rest.actions.downloadArtifact({
        owner,
        repo,
        artifact_id: detailsArtifact.id,
        archive_format: 'zip'
      });

      const adm = new AdmZip(Buffer.from(zip.data as ArrayBuffer));
      const entry = adm.getEntry('build-details.json');
      if (!entry) {
        throw new Error('Artifact build-details did not contain build-details.json.');
      }

      const json = entry.getData().toString('utf8');
      const build: BuildDetails = JSON.parse(json);
      version = build.version;
    } catch (err: any) {
      this._log.warn('The build-details.json file does not exist, make sure your build uses the build-details action');
      this._log.warn(err.message);
    }

    return version;
  }
}
