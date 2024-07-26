import { Lifecycle, scoped } from 'tsyringe';
import { EmailAliases } from '../email-aliases';
import { EnvironmentSettings } from './environments-settings';
import { SlackSettings } from './slack-settings';
import { BuildsSettings } from './builds-settings';
import { BadgeSettings } from './badge-settings';
import { ServiceSettings } from './service-settings';
import { JiraSettings } from './jira-settings';

@scoped(Lifecycle.ContainerScoped)
export class RepoSettings {
  owner: string;
  repo: string;
  deployWorkflow: string;
  syncWorkflow?: string;
  environments: EnvironmentSettings[];
  services: ServiceSettings[];
  defaultEnvironment: string;
  releaseEnvironment: string;
  defaultBranch: string;
  settingsBranch: string;
  jira: JiraSettings;
  builds: BuildsSettings;
  slack: SlackSettings;
  emailAliases: EmailAliases;
  deployManagerSiteUrl: string;
  badge: BadgeSettings;
}
