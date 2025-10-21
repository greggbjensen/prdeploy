import { expect } from '@jest/globals';
import { JiraService } from '@src/services';
import { container } from 'tsyringe';
import { Version3Client } from 'jira.js';
import { ContainerHelper } from '@test/helpers';

const useMocks = !(process.env.JIRA_USERNAME && process.env.JIRA_PASSWORD) || process.env.USE_MOCKS === 'true';
const searchJira = jest.fn().mockResolvedValueOnce({
  issues: [
    {
      key: 'SCRUM-1234',
      self: 'https://greggbjensen.atlassian.net/browse/SCRUM-1234',
      fields: {
        summary: 'Containerize www.myorg.com for the dev environment',
        issuetype: {
          name: 'Story',
          iconUrl:
            'https://greggbjensen.atlassian.net/rest/api/2/universal_avatar/view/type/issuetype/avatar/10315?size=medium'
        }
      }
    }
  ]
});

describe('listAssociatedIssues', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();

    if (useMocks) {
      searchJira.mockClear();

      container.register(Version3Client, {
        useValue: {
          issueSearch: {
            searchForIssuesUsingJqlEnhancedSearch: searchJira
          }
        } as any
      });
    } else {
      container.register(Version3Client, {
        useFactory: () => {
          return new Version3Client({
            host: process.env.JIRA_HOST,
            authentication: {
              basic: {
                email: process.env.JIRA_USERNAME,
                apiToken: process.env.JIRA_PASSWORD
              }
            }
          });
        }
      });
    }
  });

  it('handles no associated issues', async () => {
    const service = container.resolve(JiraService);
    const result = await service.listAssociatedIssues('app-k8s-deploy');
    expect(result).toEqual([]);
  });

  it('lists associated issues', async () => {
    const service = container.resolve(JiraService);
    const issues = await service.listAssociatedIssues('SCRUM-1234-SCRUM-1242-UX-553-app-k8s-deploy');
    expect(issues).toBeTruthy();
    expect(issues.length).toBeGreaterThan(0);
    expect(issues[0].key).toBeTruthy();

    if (useMocks) {
      const search = searchJira.mock.calls[0][0];
      expect(search.jql).toEqual("key in ('SCRUM-1234', 'SCRUM-1242', 'UX-553')");
    }
  });
});
