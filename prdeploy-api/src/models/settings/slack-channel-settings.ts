export type SlackChannelTypes = 'deploy' | 'release';

export type SlackChannelSettings = {
  [key in SlackChannelTypes]: string;
};
