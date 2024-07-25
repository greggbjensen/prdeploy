import { PullRequest } from '@src/models';

export interface CommentCommand {
  name: string;
  pattern: RegExp;
  action: (match: RegExpMatchArray, pullRequest: PullRequest, commentId: number) => Promise<void>;
}
