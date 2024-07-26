import { ServiceState } from './service-state';

export interface DeployState {
  pullNumber: number;
  services: ServiceState[];
}
