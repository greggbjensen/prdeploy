import { EnvironmentSettings, RepoSettings } from '@src/models';
import { Lifecycle, scoped } from 'tsyringe';

@scoped(Lifecycle.ContainerScoped)
export class EnvironmentUtil {
  private static readonly NormalizeEnvironmentRegex = /(\d+)?(-lock)?$/i;
  private static readonly EnvironmentLockRegex = /(-lock)$/i;

  constructor(private _settings: RepoSettings) {}

  getSettings(environment: string): EnvironmentSettings {
    const lowerEnvironment = environment.toLowerCase();
    const environmentSettings = this._settings.environments.find(e => e.name.toLowerCase() === lowerEnvironment);
    return environmentSettings;
  }

  normalize(environment: string): string {
    let normalEnvironment = environment;
    if (normalEnvironment) {
      normalEnvironment = normalEnvironment.toLowerCase();
    } else {
      normalEnvironment = this._settings.defaultEnvironment?.toLowerCase();
    }

    // Remove ending numbers.
    normalEnvironment = normalEnvironment.replace(EnvironmentUtil.NormalizeEnvironmentRegex, '');

    return normalEnvironment;
  }

  getFromLock(lockLabel: string): string {
    const environment = lockLabel.toLowerCase().replace(EnvironmentUtil.EnvironmentLockRegex, '');
    return environment;
  }
}
