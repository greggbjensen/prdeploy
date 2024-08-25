import Handlebars, { TemplateDelegate } from 'handlebars';
import fs from 'fs/promises';
import { Lifecycle, scoped } from 'tsyringe';

Handlebars.registerHelper('jsonEncode', (text: string) => {
  return JSON.stringify(text);
});

export type TemplateNames =
  | 'deploy-started.md'
  | 'deploy-completed.json'
  | 'deploy-completed.md'
  | 'deploy-nothing-found.json'
  | 'deploy-nothing-found.md'
  | 'deploy-failed.md'
  | 'deploy-released.json'
  | 'environment-available.json'
  | 'environment-approval-required.json'
  | 'pull-request-checks-incomplete.md'
  | 'pull-request-checks-redeploy.md'
  | 'pull-request-enqueued.md'
  | 'pull-request-issues.md'
  | 'pull-request-merge-conflicts.json'
  | 'pull-request-merge-conflicts.md'
  | 'pull-request-updated.md'
  | 'rollback-completed.md'
  | 'rollback-started.md'
  | 'services-not-found.md';

Handlebars.registerHelper('color', hexValue => (hexValue ? hexValue.replace(/^#/, '') : ''));

@scoped(Lifecycle.ContainerScoped)
export class TemplateService {
  private static readonly templates = new Map<string, TemplateDelegate<any>>();

  async render(templateName: TemplateNames, context: any = null): Promise<string> {
    let template = TemplateService.templates.get(templateName);
    if (!template) {
      let content = await fs.readFile(`./templates/${templateName}`, 'utf8');
      content = content.replace(/\r/g, '');
      template = Handlebars.compile(content);
      TemplateService.templates.set(templateName, template);
    }

    const result = template(context);
    return result;
  }

  renderQueueTable(
    owner: string,
    repo: string,
    normalizedEnvironment: string,
    queuePullNumbers: number[],
    repositoryUrl: string,
    prdeployPortalUrl: string
  ): string {
    // This is dynamic horizontally, so cannot be handled by normal template engine.
    let p = 0;
    let positions = '| Position |';
    let columns = '|----------|';
    let pulls = `| [${normalizedEnvironment} queue](${prdeployPortalUrl}/${owner}/${repo}?environment=${normalizedEnvironment}) |`;
    for (const pr of queuePullNumbers) {
      p++;
      positions += ` ${p}       |`;
      columns += `---------|`;
      pulls += ` [${pr}](${repositoryUrl}/pull/${pr}) |`;
    }

    return `${positions}\n${columns}\n${pulls}`;
  }
}
