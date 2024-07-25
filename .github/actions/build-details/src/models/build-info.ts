import { PullRequest } from './pull-request';

export interface BuildInfo {
  runId: number;
  version: string;
  head: string;
  base: string;
  commit: string;
  owner: string;
  repo: string;
  eventName: string;
  pullRequest?: PullRequest;
}
