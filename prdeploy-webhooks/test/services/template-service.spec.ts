import { expect } from '@jest/globals';
import { TemplateService } from '@src/services';
import { JiraIssue } from '@src/models';
import { container } from 'tsyringe';

describe('renderQueueTable', () => {
  it('renders dynamic pull request queue table', async () => {
    const service = container.resolve(TemplateService);
    const result = service.renderQueueTable(
      'greggbjensen',
      'prdeploy-example-repo',
      'dev',
      [2987, 2549, 2391],
      'https://github.com/greggbjensen/prdeploy-example-repo',
      'https://awssite/deployments'
    );
    expect(result).not.toBeFalsy();
    expect(result).toMatch(`| Position | 1       | 2       | 3       |
|----------|---------|---------|---------|
| [dev queue](https://awssite/deployments?environment=dev&owner=greggbjensen&repo=prdeploy-example-repo) | [2987](https://github.com/greggbjensen/prdeploy-example-repo/pull/2987) | [2549](https://github.com/greggbjensen/prdeploy-example-repo/pull/2549) | [2391](https://github.com/greggbjensen/prdeploy-example-repo/pull/2391) |`);
  });
});

describe('render', () => {
  it('renders static template from file', async () => {
    const service = container.resolve(TemplateService);
    const result = await service.render('pull-request-issues.md');
    expect(result).not.toBeFalsy();
  });

  it('renders dynamic template from file with context', async () => {
    const service = container.resolve(TemplateService);
    const issues: JiraIssue[] = [
      {
        type: 'story',
        iconUrl: 'https://icons.test/story.png',
        key: 'SCRUM-16478',
        url: 'https://greggbjensen.atlassian.net/browse/SCRUM-16478',
        summary: 'Additional Property types dont appear to be cached on a new load of the sku pane'
      },
      {
        type: 'bug',
        iconUrl: 'https://icons.test/bug.png',
        key: 'SCRUM-16636',
        url: 'https://greggbjensen.atlassian.net/browse/SCRUM-16636',
        summary: 'Images are not getting augmented'
      }
    ];
    const pullBody = `
Pull request with fix and feature.
`;
    const result = await service.render('pull-request-issues.md', {
      issues,
      pullBody,
      branchName: 'SCRUM-16478-SCRUM-16636-feature-and-bug'
    });

    expect(result).not.toBeFalsy();
    console.log(result);
    expect(result).toMatch(
      `<!-- ISSUES_START:SCRUM-16478-SCRUM-16636-feature-and-bug -->
![story](https://icons.test/story.png) [SCRUM-16478](https://greggbjensen.atlassian.net/browse/SCRUM-16478) Additional Property types dont appear to be cached on a new load of the sku pane
![bug](https://icons.test/bug.png) [SCRUM-16636](https://greggbjensen.atlassian.net/browse/SCRUM-16636) Images are not getting augmented
<!-- ISSUES_END -->


Pull request with fix and feature.
`
    );
  });

  it('renders JSON template with escaping', async () => {
    const service = container.resolve(TemplateService);
    const slackPullBody = `
This pull request body includes multiple
new lines and " these things...
Oh and another one"
`;

    const result = await service.render('deploy-released.json', {
      slackPullBody
    });

    expect(() => JSON.parse(result)).not.toThrow();
  });
});
