import { SlackWebhooksSettings } from './slack-webhooks-settings';

export interface SlackSettings {
  token: string;
  emailDomain: string;
  webhooks: SlackWebhooksSettings;
  notificationsEnabled: boolean;
}
