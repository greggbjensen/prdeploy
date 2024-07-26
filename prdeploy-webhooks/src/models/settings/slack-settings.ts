import { SlackChannelSettings } from './slack-channel-settings';

export interface SlackSettings {
  token: string;
  emailDomain: string;
  channels: SlackChannelSettings;
  notificationsEnabled: boolean;
}
