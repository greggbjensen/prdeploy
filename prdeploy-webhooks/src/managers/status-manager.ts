import { Lifecycle, scoped } from 'tsyringe';
import { WorkflowRun } from '@octokit/webhooks-types';
import { DeployService, LogService, PullRequestService, StatusService } from '@src/services';
import { DeploySettings, PullRequest } from '@src/models';
import path from 'path';
import { AutomationTestService } from '@src/services/automation-test-service';
import _ from 'lodash';
import { WorkflowService } from '@src/services/workflow-service';

@scoped(Lifecycle.ContainerScoped)
export class StatusManager {
  constructor(
    private _statusService: StatusService,
    private _deployService: DeployService,
    private _pullRequestService: PullRequestService,
    private _automationTestService: AutomationTestService,
    private _workflowService: WorkflowService,
    private _logService: LogService,
    private _settings: DeploySettings
  ) {}

  async processWorkflowRun(run: WorkflowRun | any, action: string): Promise<void> {
    let pullNumber = run.pull_requests.length > 0 ? run.pull_requests[0].number : 0;
    if (!pullNumber && action === 'requested') {
      // Attempt to get updated workflow name.
      run = await this._workflowService.getRun(run.id);
    }

    if (!pullNumber && this._pullRequestService.isRollback(run)) {
      pullNumber = this._pullRequestService.getRollbackPullNumber(run);
    }

    if (pullNumber) {
      const pullRequest = await this._pullRequestService.get(pullNumber);
      this._logService.context.pullRequest = pullRequest;

      if (this.isDeployWorkflow(run.path) && pullRequest) {
        if (action === 'requested') {
          await this._statusService.postDeployStarted(pullRequest, run);
        } else if (action === 'completed') {
          await this._statusService.postDeployCompleted(pullRequest, run);

          if (run.conclusion === 'success') {
            await this._automationTestService.triggerTestRun(pullRequest, run);
          }
        }
      } else if (action === 'completed' && pullRequest && !this.isExcludedWorkflow(run.path)) {
        await this._deployService.tryRetriggerDeploy(pullRequest);
      }
    }
  }

  async processPullRequestClosed(pullRequest: PullRequest): Promise<void> {
    await this._deployService.releasePullRequest(pullRequest);
  }

  isDeployWorkflow(workflowPath: string): boolean {
    const workflowName = path.basename(workflowPath);
    return workflowName.toLowerCase() === this._settings.deployWorkflow.toLowerCase();
  }

  isExcludedWorkflow(workflowPath: string): boolean {
    const excludedWorkflows = _.uniq(
      this._settings.environments
        .filter(e => e.automationTest.enabled && e.automationTest.workflow)
        .map(e => e.automationTest.workflow.toLowerCase())
    );
    excludedWorkflows.push(this._settings.deployWorkflow.toLowerCase());

    const checkWorkflow = path.basename(workflowPath).toLowerCase();
    return excludedWorkflows.includes(checkWorkflow);
  }
}
