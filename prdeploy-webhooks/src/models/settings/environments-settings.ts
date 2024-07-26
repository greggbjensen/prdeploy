import { AutomationTestSettings } from './automation-test-settings';

export interface EnvironmentSettings {
  name: string;
  queue: string;
  color: string;
  url: string;
  requireApproval: boolean; // This is determined and set automatically.
  requireBranchUpToDate: boolean;
  automationTest: AutomationTestSettings;
  excludeFromRollback: string[];
}
