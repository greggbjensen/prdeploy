import { Lifecycle, scoped } from 'tsyringe';
import { EnvironmentSettings } from './environments-settings';
import { SlackSettings } from './slack-settings';
import { BuildsSettings } from './builds-settings';
import { BadgeSettings } from './badge-settings';
import { ServiceSettings } from './service-settings';
import { JiraSettings } from './jira-settings';

@scoped(Lifecycle.ContainerScoped)
export class DeploySettings {
  owner: string;
  repo: string;
  deployWorkflow: string;
  environments: EnvironmentSettings[];
  services: ServiceSettings[];
  defaultEnvironment: string;
  releaseEnvironment: string;
  defaultBranch: string;
  jira: JiraSettings;
  builds: BuildsSettings;
  slack: SlackSettings;
  prdeployPortalUrl: string;
  badge: BadgeSettings;
}
