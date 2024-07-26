import { RepoSettings, SlackChannelTypes, SlackUser, User } from '@src/models';
import slack from 'slack';
import { Lifecycle, scoped } from 'tsyringe';
import { LogService } from './log-service';
import slackifyMarkdown from 'slackify-markdown';

@scoped(Lifecycle.ContainerScoped)
export class SlackService {
  private static readonly WhitespaceRegex = /\s+/;

  constructor(
    private _settings: RepoSettings,
    private _log: LogService
  ) {}

  async postMessage(channelType: SlackChannelTypes, json: string): Promise<void> {
    if (!this._settings.slack.notificationsEnabled) {
      this._log.info('Slack notifications disabled');
      return;
    }

    const webhookUrl = this._settings.slack.channels[channelType];
    if (!webhookUrl) {
      this._log.warn(`No ${channelType} Slack channel specified.`);
      return;
    }

    // TODO: PROD Waiting for approval SLACK.  COLORED logs. Fix JSON after.
    this._log.debug(`Sending Slack message to ${channelType} channel:\n${json}`);
    try {
      JSON.parse(json);
    } catch (error) {
      this._log.warn(`Slack message is not valid json.  `, error);
    }

    try {
      const response = await fetch(webhookUrl, {
        headers: {
          Accept: 'application/json'
        },
        method: 'post',
        body: json
      });

      const resultText = await response.text();
      this._log.info(`Slack ${channelType} messaged posted with response (${response.status}):\n${resultText}`);
    } catch (error) {
      this._log.error(`Unable to post Slack message to ${channelType}.`, error);
    }
  }

  async lookupUser(user: User): Promise<SlackUser> {
    const result: SlackUser = {
      name: user.name || user.login,
      email: user.email
    };
    this._log.info(`Slack name: ${result.name}`);
    this._log.info(`Slack email: ${result.email}`);

    // If there is no email, try and build one from name.
    let lookupEmail = result.email;
    if (!lookupEmail) {
      if (!result.name) {
        this._log.warn(`One input of name or email is required.`);
        return null;
      }

      lookupEmail = this.buildEmailFromName(result.name, this._settings.slack.emailDomain);
    }

    if (!lookupEmail) {
      this._log.warn('Unable to find user email.');
      return null;
    }

    if (this._settings.emailAliases) {
      const emailAlias = this._settings.emailAliases[lookupEmail];
      if (emailAlias) {
        this._log.info(`alias: ${emailAlias}`);
        lookupEmail = emailAlias;
      }
    }

    try {
      const response = await slack.users.lookupByEmail({
        token: this._settings.slack.token,
        email: lookupEmail
      });

      if (response.ok) {
        result.id = response.user.id;
        result.username = response.user.name;
        this._log.info(`Found slack user: ${response.user.name}`);
      }
    } catch (error) {
      this._log.warn(`Unable to find user for email ${lookupEmail}: ${error}`);
    }

    return result;
  }

  translateGitHubMarkdown(markdown: string): string {
    const cleaned = slackifyMarkdown(markdown);
    return cleaned;
  }

  private buildEmailFromName(name: string, emailDomain: string): string {
    let email = '';

    // Get only first and last name, skipping middle if present.
    const nameParts = name.split(SlackService.WhitespaceRegex);
    const firstName = nameParts[0].toLocaleLowerCase();
    const lastName = nameParts.length > 1 ? nameParts[nameParts.length - 1].toLocaleLowerCase() : '';

    if (firstName && lastName) {
      email = `${firstName}.${lastName}@${emailDomain}`;
    } else {
      email = `${firstName}${lastName}@${emailDomain}`;
    }

    this._log.info(`Email address not passed, so built email is: ${email}`);

    return email;
  }
}
