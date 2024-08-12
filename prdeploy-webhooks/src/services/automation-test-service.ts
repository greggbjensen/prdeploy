import { PullRequest, DeploySettings, WorkflowInputs } from '@src/models';
import { WorkflowRun } from '@octokit/webhooks-types';
import { EnvironmentUtil } from '@src/utils';
import { Octokit } from '@octokit/rest';
import { Lifecycle, scoped } from 'tsyringe';
import { LogService } from './log-service';
import _ from 'lodash';
import { PullRequestService } from './pull-request-service';

@scoped(Lifecycle.ContainerScoped)
export class AutomationTestService {
  private static readonly TokenRegex = /\$\{(.*?)\}/g;

  constructor(
    private _pullRequestService: PullRequestService,
    private _environmentUtil: EnvironmentUtil,
    private _octokit: Octokit,
    private _settings: DeploySettings,
    private _log: LogService
  ) {}

  async triggerTestRun(pullRequest: PullRequest, run: WorkflowRun): Promise<void> {
    this._log.debug(`Evaluating automation test dispatch for pull #${pullRequest.number}.`);
    const environment = this._pullRequestService.getEnvironment(pullRequest, run);
    const environmentSettings = this._environmentUtil.getSettings(environment);

    const automationSettings = environmentSettings.automationTest;
    if (automationSettings.enabled) {
      const context = {
        environment,
        pull_number: pullRequest.number,
        run_name: run.name
      };
      const inputs = this.replaceAutomationTokens(automationSettings.inputs, context);
      this._log.info(
        `Dispatching automation test workflow: ${automationSettings.workflow}: ${JSON.stringify(inputs, null, 2)}`
      );
      await this._octokit.rest.actions.createWorkflowDispatch({
        owner: this._settings.owner,
        repo: this._settings.repo,
        workflow_id: automationSettings.workflow,
        ref: pullRequest.head.ref,
        inputs
      });
    }
  }

  private replaceAutomationTokens(inputs: WorkflowInputs, context: { [name: string]: unknown }): WorkflowInputs {
    const result = Object.assign({}, inputs);
    const names = Object.keys(inputs);
    for (const name of names) {
      result[name] = inputs[name].toString().replace(AutomationTestService.TokenRegex, (match, key) => {
        let text = match;
        const value = context[key];
        if (!_.isNil(value)) {
          text = value as string;
        } else {
          this._log.warn(`Automation workflow inputs, no context match found for ${key}.`);
        }

        return text;
      });
    }

    return result;
  }
}
