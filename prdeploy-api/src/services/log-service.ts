import { Octokit } from 'octokit';
import { Lifecycle, scoped } from 'tsyringe';
import { LogContext, LogLevelInfo } from './models';

@scoped(Lifecycle.ContainerScoped)
export class LogService {
  private static readonly LogLevelInfo: { [level: string]: LogLevelInfo } = {
    error: {
      name: 'error',
      priority: 1,
      abbreviation: 'ERR',
      colorCode: 91 // https://en.wikipedia.org/wiki/ANSI_escape_code#Colors
    },
    warn: {
      name: 'warn',
      priority: 1,
      abbreviation: 'WRN',
      colorCode: 93
    },
    info: {
      name: 'info',
      priority: 1,
      abbreviation: 'INF',
      colorCode: 36
    },
    debug: {
      name: 'debug',
      priority: 1,
      abbreviation: 'DBG',
      colorCode: 95
    }
  };
  private _enableConsole = false;
  private _enableOctokit = true;
  private _currentPriority: number;

  context: LogContext = { repository: null };

  constructor(private _octokit: Octokit) {
    this._enableConsole = process.env.ENABLE_CONSOLE_LOGGING?.toLowerCase() === 'true' || !!process.env.JEST_WORKER_ID;
    this._enableOctokit = process.env.ENABLE_OCTOKIT_LOGGING?.toLowerCase() === 'true';
    this._currentPriority = LogService.LogLevelInfo[process.env.LOG_LEVEL || 'debug'].priority;
  }

  error(message: string, additionalInfo?: any): void {
    this.log('error', message, additionalInfo);
  }

  warn(message: string, additionalInfo?: any): void {
    this.log('warn', message, additionalInfo);
  }

  info(message: string, additionalInfo?: any): void {
    this.log('info', message, additionalInfo);
  }

  debug(message: string, additionalInfo?: any): void {
    this.log('debug', message, additionalInfo);
  }

  private log(level: string, message: string, additionalInfo: any = ''): void {
    const levelInfo = LogService.LogLevelInfo[level];
    if (levelInfo.priority < this._currentPriority) {
      return;
    }

    const formattedMessage = this.formatMessage(levelInfo, message);
    switch (level) {
      case 'error':
        if (this._enableOctokit) {
          this._octokit.log.error(formattedMessage, additionalInfo);
        }
        if (this._enableConsole) {
          console.error(formattedMessage, additionalInfo);
        }
        break;

      case 'warn':
        if (this._enableOctokit) {
          this._octokit.log.warn(formattedMessage, additionalInfo);
        }
        if (this._enableConsole) {
          console.warn(formattedMessage, additionalInfo);
        }
        break;

      case 'info':
        if (this._enableOctokit) {
          this._octokit.log.info(formattedMessage, additionalInfo);
        }
        if (this._enableConsole) {
          console.info(formattedMessage, additionalInfo);
        }
        break;

      default:
        if (this._enableOctokit) {
          this._octokit.log.debug(formattedMessage, additionalInfo);
        }
        if (this._enableConsole) {
          console.debug(formattedMessage, additionalInfo);
        }
        break;
    }
  }

  formatMessage(levelInfo: LogLevelInfo, message: string): string {
    const pullText = this.context.pullRequest?.number ? ` #${this.context.pullRequest.number}` : '';
    return `\x1b[90m[${this.context.repository?.owner?.login}/${this.context.repository?.name}${pullText}]\x1b[0m \x1b[${levelInfo.colorCode}m${levelInfo.abbreviation}\x1b[0m ${message}`;
  }
}
