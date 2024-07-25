import { ServiceState, DeployState } from '@src/models';
import { ParameterService } from './parameter-service';
import { Lifecycle, scoped } from 'tsyringe';
import { LogService } from './log-service';
import _ from 'lodash';

@scoped(Lifecycle.ContainerScoped)
export class DeployStateService {
  constructor(
    private _parameterService: ParameterService,
    private _log: LogService
  ) {}

  async sync(environment: string): Promise<void> {
    const stableState = await this.get('stable');
    await this.set(environment, stableState);
  }

  async update(environment: string, pullNumber: number, services: ServiceState[]): Promise<DeployState> {
    this._log.info(`Updating deploy state for ${environment} and pull number ${pullNumber}.`);

    const state = await this.get(environment);
    state.pullNumber = pullNumber;

    // Filter out any existing services that will be added.
    const updatedServices = state.services.filter(s => services.every(i => s.name !== i.name));
    state.services = _.sortBy(updatedServices.concat(services), s => s.name);

    await this.set(environment, state);
    return state;
  }

  async get(environment: string, historical = 0): Promise<DeployState> {
    const name = this.getVariableName(environment);
    let value = await this._parameterService.getObject<DeployState>(name, 'Repo', historical);
    if (!value || !value.services) {
      this._log.info(`Creating deploy state for: ${name}.`);
      value = {
        pullNumber: 0,
        services: []
      };
      await this._parameterService.setObject(name, value);
    }

    return value;
  }

  async diff(sourceEnvironment: string, targetEnvironment: string, sourceHistorical = 0): Promise<DeployState> {
    let diff: DeployState = null;

    const sourceState = await this.get(sourceEnvironment, sourceHistorical);
    const targetState = await this.get(targetEnvironment);

    const targetStateLookup = new Map<string, ServiceState>();
    for (const service of targetState.services) {
      targetStateLookup.set(service.name, service);
    }

    targetState.services = sourceState.services.filter(source => {
      const target = targetStateLookup.get(source.name);
      return !target || target.version !== source.version;
    });

    diff = Object.assign(targetState, {
      pullNumber: sourceState.pullNumber
    });

    return diff;
  }

  private async set(environment: string, state: DeployState): Promise<void> {
    const name = this.getVariableName(environment);
    await this._parameterService.setObject(name, state);
  }

  private getVariableName(environment: string): string {
    return `DEPLOY_STATE_${environment.toUpperCase()}`;
  }
}
