import { PullRequest } from '@src/models';
import { Repository } from '@octokit/webhooks-types';

export interface LogContext {
  repository: Repository;
  pullRequest?: PullRequest;
}
