import { IssueComment } from '@octokit/webhooks-types';
import { PullRequest } from '@src/models';
import { DeployService, LogService, PullRequestService } from '@src/services';
import { Lifecycle, scoped } from 'tsyringe';
import { CommentCommand } from './models';

@scoped(Lifecycle.ContainerScoped)
export class CommentCommandManager {
  private static readonly IssueIdRegex = /.*\/(?<issueNumber>\d+)/;
  private _commands: CommentCommand[];

  constructor(
    private _deployService: DeployService,
    private _pullRequestService: PullRequestService,
    private _logService: LogService
  ) {
    this._commands = [
      {
        name: 'deploy',
        pattern:
          /(?:^|\s)\/deploy(?:\s+(?<environment>\w+))?(?:\s+--(?<forceDeploy>force))?(?:\s+--(?<retainLocks>retain))?/,
        action: this.deployCommand.bind(this)
      },
      {
        name: 'free',
        pattern: /(?:^|\s)\/free(?:\s+(?<environment>\w+))?/,
        action: this.freeCommand.bind(this)
      },
      {
        name: 'rollback',
        pattern: /(?:^|\s)\/rollback(?:\s+(?<environment>\w+))?(?:\s+(?<count>\d+))?/,
        action: this.rollbackCommand.bind(this)
      },
      {
        name: 'add',
        pattern: /(?:^|\s)\/add\s+(?<services>[\w-_\s]+)/,
        action: this.addCommand.bind(this)
      }
    ];
  }

  async processComment(comment: IssueComment | any): Promise<void> {
    for (const command of this._commands) {
      const commandMatch = comment.body.match(command.pattern);
      if (commandMatch) {
        const issueMatch = comment.issue_url.match(CommentCommandManager.IssueIdRegex);
        const pullNumber = parseInt(issueMatch.groups['issueNumber'], 10);
        const pullRequest = await this._pullRequestService.get(pullNumber);
        this._logService.context.pullRequest = pullRequest;
        await command.action(commandMatch, pullRequest, comment.id);
        break;
      }
    }
  }

  private async deployCommand(match: RegExpMatchArray, pullRequest: PullRequest, commentId: number): Promise<void> {
    const environment = match.groups['environment'];
    const forceDeploy = match.groups['forceDeploy']?.toLowerCase() === 'force';
    const retainLocks = match.groups['retainLocks']?.toLowerCase() === 'retain';

    await this._deployService.requestDeploy(pullRequest, environment, commentId, forceDeploy, retainLocks);
  }

  private async rollbackCommand(match: RegExpMatchArray, pullRequest: PullRequest, commentId: number): Promise<void> {
    const environment = match.groups['environment'];
    const countValue = match.groups['count'];
    let count = 1;
    if (countValue) {
      count = parseInt(countValue, 10);
    }

    await this._deployService.rollbackDeploy(pullRequest, environment, commentId, count);
  }

  private async freeCommand(match: RegExpMatchArray, pullRequest: PullRequest, commentId: number): Promise<void> {
    const environment = match.groups['environment'];

    await this._deployService.freeDeploy(pullRequest, environment, commentId);
  }

  private async addCommand(match: RegExpMatchArray, pullRequest: PullRequest, commentId: number): Promise<void> {
    let servicesValue = match.groups['services'];

    // Remove multiple spaces.
    servicesValue = servicesValue.replace(/\s\s+/, ' ').trim();
    const services = servicesValue.split(' ');
    await this._deployService.addServices(pullRequest, services, commentId);
  }
}
