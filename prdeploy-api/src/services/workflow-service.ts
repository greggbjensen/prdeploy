import { Lifecycle, scoped } from 'tsyringe';
import { LogService } from './log-service';
import { RepoSettings } from '@src/models';
import { WorkflowRun } from '@octokit/webhooks-types';
import { Octokit } from 'octokit';

@scoped(Lifecycle.ContainerScoped)
export class WorkflowService {
  constructor(
    private _octokit: Octokit,
    private _log: LogService,
    private _settings: RepoSettings
  ) {}

  async getRun(runId: number): Promise<WorkflowRun> {
    const runReponse = await this._octokit.rest.actions.getWorkflowRun({
      owner: this._settings.owner,
      repo: this._settings.repo,
      run_id: runId
    });

    return runReponse.data as WorkflowRun;
  }
}
