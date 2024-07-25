import JiraApi from 'jira-client';
import _ from 'lodash';
import { JiraIssue } from '@src/models';
import { LogService } from './log-service';
import { Lifecycle, scoped } from 'tsyringe';

@scoped(Lifecycle.ContainerScoped)
export class JiraService {
  private static readonly JiraIssueRegex = /[a-z]{2,6}-\d+/gi;

  constructor(
    private _log: LogService,
    private _jiraApi: JiraApi
  ) {}

  async listAssociatedIssues(branchName: string): Promise<JiraIssue[]> {
    this._log.info(`Parsing branch: ${branchName}`);
    const matches = branchName.match(JiraService.JiraIssueRegex);
    if (!matches || matches.length === 0) {
      this._log.info('No matching issues found from branch.');
      return [];
    }

    const issueIds = _.uniq(matches.map(m => m?.toUpperCase()));
    this._log.info(`Found issue IDs: ${issueIds.join(', ')}`);
    const jql = `key in ('${issueIds.join("', '")}')`;

    let issues: JiraIssue[];
    try {
      const response = await this._jiraApi.searchJira(jql);

      issues = response.issues.map((i: any) => {
        const baseUrl = i.self.split('/').slice(0, 3).join('/');
        const url = `${baseUrl}/browse/${i.key}`;
        return {
          key: i.key,
          summary: i.fields.summary,
          url,
          type: i.fields.issuetype.name,
          iconUrl: i.fields.issuetype.iconUrl
        } as JiraIssue;
      });
    } catch (err) {
      this._log.warn('Unable to list associated issues.', err);
      issues = [];
    }

    return issues;
  }
}
