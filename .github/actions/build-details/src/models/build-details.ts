export interface BuildDetails {
  runId: number;
  version: string;
  head: string;
  base: string;
  commit: string;
  pullRequest: number;
  owner: string;
  repo: string;
  eventName: string;
}
