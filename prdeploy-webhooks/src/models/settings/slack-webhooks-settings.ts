export type SlackChannelTypes = 'deploy' | 'release';

export type SlackWebhooksSettings = {
  [key in SlackChannelTypes]: string;
};
