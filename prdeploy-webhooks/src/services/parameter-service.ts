import * as YAML from 'yaml';
import { Repository } from '@octokit/webhooks-types';
import { Lifecycle, inject, scoped } from 'tsyringe';
import { REPOSITORY, SSM_CLIENT } from '@src/injection-tokens';
import {
  GetParameterCommand,
  GetParameterHistoryCommand,
  GetParameterHistoryCommandOutput,
  GetParametersByPathCommand,
  PutParameterCommand,
  SSMClient
} from '@aws-sdk/client-ssm';
import { LogService } from './log-service';
import { ParameterLevel } from './models';

@scoped(Lifecycle.ContainerScoped)
export class ParameterService {
  private static readonly PARAMETER_STORE_ROOT = 'prdeploy';
  private static readonly MAX_HISTORY_PER_PAGE = 50;

  constructor(
    @inject(SSM_CLIENT) private _client: SSMClient,
    @inject(REPOSITORY) private _repository: Repository,
    private _log: LogService
  ) {}

  async getAll(): Promise<Map<string, string>> {
    const parameters = new Map<string, string>();
    await this.populateAllParameters('Org', parameters);
    await this.populateAllParameters('Repo', parameters);
    return parameters;
  }

  async getOrCreateObject<T>(name: string, defaultValue: T, level: ParameterLevel = 'Repo'): Promise<T> {
    let value: T = await this.getObject<T>(name, level);
    if (!value) {
      value = defaultValue;
      await this.setObject(name, value, level);
    }

    return value;
  }

  async getObject<T>(name: string, level: ParameterLevel = 'Repo', historical = 0): Promise<T> {
    let value: T = null;

    const stringValue = await this.getString(name, level, historical);
    if (!stringValue) {
      return null;
    }

    try {
      value = YAML.parse(stringValue) as T;
    } catch (error) {
      const fullName = this.getFullName(name, level);
      this._log.error(`Unable to parse parameter object from ${fullName}.`);
    }

    return value;
  }

  async getOrCreateString(name: string, defaultValue: string, level: ParameterLevel = 'Repo'): Promise<string> {
    let value = await this.getString(name, level);
    if (!value) {
      value = defaultValue;
      await this.setString(name, value, level);
    }

    return value;
  }

  async getString(name: string, level: ParameterLevel = 'Repo', historical = 0): Promise<string> {
    let value: string = null;

    const fullName = this.getFullName(name, level);

    try {
      if (!historical) {
        const response = await this._client.send(
          new GetParameterCommand({
            Name: fullName,
            WithDecryption: true
          })
        );

        value = response?.Parameter?.Value || null;
      } else {
        // Because history is in ascending order we have to get to the last page and go back.
        let hasMore = true;
        let response: GetParameterHistoryCommandOutput;
        while (hasMore) {
          response = await this._client.send(
            new GetParameterHistoryCommand({
              Name: fullName,
              WithDecryption: true,
              MaxResults: ParameterService.MAX_HISTORY_PER_PAGE
            })
          );

          hasMore = !!response.NextToken;
        }

        if (response && response.Parameters?.length > historical) {
          value = response.Parameters[response.Parameters.length - (historical + 1)].Value;
        }
      }
    } catch (error: any) {
      this._log.debug(`Parameter ${fullName} not found.  ${error.message}`);
    }

    return value;
  }

  async setObject<T>(name: string, value: T, level: ParameterLevel = 'Repo', isSecret = false): Promise<void> {
    const stringValue = YAML.stringify(value);
    await this.setString(name, stringValue, level, isSecret);
  }

  async setString(name: string, value: string, level: ParameterLevel = 'Repo', isSecret = false): Promise<void> {
    await this._client.send(
      new PutParameterCommand({
        Name: this.getFullName(name, level),
        Value: value,
        Type: !isSecret ? 'String' : 'SecureString',
        Overwrite: true,
        Tier: 'Standard'
      })
    );
  }

  private getPath(level: ParameterLevel): string {
    return level === 'Repo'
      ? `/${ParameterService.PARAMETER_STORE_ROOT}/${this._repository.owner.login}/${this._repository.name}`
      : `/${ParameterService.PARAMETER_STORE_ROOT}/${this._repository.owner.login}`;
  }

  private getFullName(name: string, level: ParameterLevel): string {
    const path = this.getPath(level);
    return `${path}/${name}`;
  }

  private async populateAllParameters(level: ParameterLevel, parameters: Map<string, string>): Promise<void> {
    try {
      const response = await this._client.send(
        new GetParametersByPathCommand({
          Path: this.getPath(level),
          Recursive: false,
          WithDecryption: true
        })
      );

      for (const parameter of response.Parameters) {
        parameters.set(parameter.Name, parameter.Value);
      }
    } catch (error) {
      this._log.error(`Unable to get ${level} parameters.  ${error}`);
    }
  }
}
