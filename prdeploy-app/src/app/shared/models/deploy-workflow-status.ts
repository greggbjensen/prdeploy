/** Where the deployment workflow for an environment currently stands. */
export type DeployWorkflowState = 'InProgress' | 'Successful' | 'Failure';

/** Latest deployment workflow status for one environment, as streamed by the API. */
export interface DeployWorkflowStatus {
  owner: string;
  repo: string;
  environment: string;
  state: DeployWorkflowState;
  pullNumber: number;
  runId: number;
  runUrl?: string | null;
  updatedAt: string;
}
