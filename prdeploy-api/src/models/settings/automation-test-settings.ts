import { WorkflowInputs } from './workflow-inputs';

export interface AutomationTestSettings {
  enabled: boolean;
  workflow?: string;
  inputs?: WorkflowInputs;
}
