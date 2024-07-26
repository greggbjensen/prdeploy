export interface PullRequest {
  prNumber: number;
  title: string;
  url: string;
  body: string | null;
  head: string;
  base: string;
}
