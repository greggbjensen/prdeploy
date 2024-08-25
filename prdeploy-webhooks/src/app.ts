import 'reflect-metadata';
import fs from 'fs';
import http from 'http';
import { App } from 'octokit';
import { createNodeMiddleware } from '@octokit/webhooks';
import { CommentCommandManager, JiraManager } from './managers';
import { webhookFlow } from './webhook-flow';
import './app-registrations';
import { StatusManager } from './managers/status-manager';

// Set configured values
console.log('Loading app info.');
const appId = process.env.APP_ID;
const privateKeyPath = process.env.PRIVATE_KEY_PATH;
if (!privateKeyPath) {
  throw new Error('Make sure you have copied .env.sample as .env and added all the settings.');
}

const privateKey = fs.readFileSync(privateKeyPath, 'utf8');
const secret = process.env.WEBHOOK_SECRET;

// Create an authenticated Octokit client authenticated as a GitHub App
console.log('Initializing app.');
const app = new App({
  appId,
  privateKey,
  webhooks: {
    secret
  }
});

app.webhooks.on(
  ['pull_request.opened', 'pull_request.edited'],
  async ({ octokit, payload: { action, repository, pull_request } }) => {
    await webhookFlow(octokit, repository, pull_request, async (scope, settings, log) => {
      log.debug(`Received a pull request ${action} for #${(pull_request as any).number}.`);

      if (!settings.jira.addIssuesEnabled) {
        log.debug(`Add JIRA issues is disabled.`);
        return;
      }

      const manager = scope.resolve(JiraManager);
      await manager.addJiraIssuesToPullRequest(pull_request as any);
    });
  }
);

app.webhooks.on(['pull_request.closed'], async ({ octokit, payload: { action, repository, pull_request } }) => {
  await webhookFlow(octokit, repository, pull_request, async (scope, settings, log) => {
    log.debug(`Received a pull request ${action} for #${(pull_request as any).number}.`);

    const manager = scope.resolve(StatusManager);
    await manager.processPullRequestClosed(pull_request as any);
  });
});

app.webhooks.on(['issue_comment.created'], async ({ octokit, payload: { action, repository, comment } }) => {
  await webhookFlow(octokit, repository, null, async (scope, settings, log) => {
    log.debug(`Received a comment ${action} (${comment.id}):\n${comment.body}`);

    const manager = scope.resolve(CommentCommandManager);
    await manager.processComment(comment);
  });
});

app.webhooks.on(
  ['workflow_run.requested', 'workflow_run.completed'],
  async ({ octokit, payload: { action, repository, workflow_run } }) => {
    await webhookFlow(octokit, repository, workflow_run.pull_requests[0], async (scope, settings, log) => {
      log.debug(
        `Received a workflow run ${action} for ${workflow_run.name}: ${workflow_run.display_title} ` +
          `(${workflow_run.id}) - ${workflow_run.pull_requests[0]?.number || 0}.`
      );

      const manager = scope.resolve(StatusManager);
      await manager.processWorkflowRun(workflow_run, action);
    });
  }
);

app.webhooks.onError(error => {
  if (error.event) {
    // Log Secret verification errors
    console.error(`Error processing request: ${error.event}`, error);
  } else {
    console.error('Webhooks unhandled error.', error);
  }
});

// Launch a web server to listen for GitHub webhooks
console.log('Binding server.');
const port = process.env.PORT || 3000;
const path = '/webhooks';
const localWebhookUrl = `http://localhost:${port}${path}`;

const middleware = createNodeMiddleware(app.webhooks, { path });

http.createServer(middleware).listen(port, () => {
  console.log(`Server is listening for events at: ${localWebhookUrl}`);
  console.log('Press Ctrl + C to quit.');
});
