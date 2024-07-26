import { PullRequest } from '@src/models';
import { JiraService, LogService, PullRequestService, TemplateService } from '@src/services';
import _ from 'lodash';
import { Lifecycle, scoped } from 'tsyringe';

@scoped(Lifecycle.ContainerScoped)
export class JiraManager {
  static readonly issuesTextRegex = /<!-- ISSUES_START(?::(?<branchName>.*?))? .*? ISSUES_END -->/gms;

  constructor(
    private _jiraService: JiraService,
    private _pullRequestService: PullRequestService,
    private _templateService: TemplateService,
    private _log: LogService
  ) {}

  async addJiraIssuesToPullRequest(pullRequest: PullRequest): Promise<void> {
    const branchName = pullRequest.head.ref;
    const matches = pullRequest.body?.matchAll(JiraManager.issuesTextRegex);

    // Make backward compatible before branch was in body.
    if (matches) {
      for (const match of matches) {
        if (match && (!match?.groups['branchName'] || match.groups['branchName'] === branchName)) {
          this._log.info('No body changes found.');
          return;
        }
      }
    }

    const issues = await this._jiraService.listAssociatedIssues(branchName);
    let pullBody = pullRequest.body?.replace(JiraManager.issuesTextRegex, '').trimStart();
    if (_.isNil(pullBody)) {
      pullBody = '';
    }

    let body = await this._templateService.render('pull-request-issues.md', { issues, pullBody, branchName });
    body = body.trim();
    await this._pullRequestService.updatePullRequestBody(pullRequest.number, body);
  }
}
