import { EmailAliases } from '../email-aliases';
import { SlackWebhooksSettings } from './slack-webhooks-settings';

export interface SlackSettings {
  token: string;
  emailDomain: string;
  emailAliases: EmailAliases;
  webhooks: SlackWebhooksSettings;
  notificationsEnabled: boolean;
}
