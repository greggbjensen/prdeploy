import { vi } from 'vitest';
import { SSM_CLIENT } from '@src/injection-tokens';
import { DeploySettings } from '@src/models';
import { DeploySettingsService } from '@src/services';
import { SSMClientMock } from '@test/mocks';
import { Octokit } from '@octokit/rest';
import { container } from 'tsyringe';

export class RepoSettingsHelper {
  static async mockCalls(octokit: Octokit): Promise<void> {
    Object.assign(octokit.rest.repos, {
      get: vi.fn().mockResolvedValueOnce({
        data: {
          default_branch: 'main'
        }
      }),
      getAllEnvironments: vi.fn().mockResolvedValueOnce({
        data: {
          environments: [
            {
              name: 'dev'
            },
            {
              name: 'dev2'
            },
            {
              name: 'dev3'
            },
            {
              name: 'stage'
            },
            {
              name: 'prod'
            },
            {
              name: 'prod-gated',
              protection_rules: [
                {
                  type: 'required_reviewers'
                }
              ]
            }
          ]
        }
      })
    });

    Object.assign(octokit.rest.issues, {
      listLabelsForRepo: vi.fn().mockResolvedValue({
        data: [
          {
            name: 'dev',
            color: 'yellow'
          },
          {
            name: 'dev-lock',
            color: 'gray'
          },
          {
            name: 'dev2',
            color: 'yellow'
          },
          {
            name: 'dev2-lock',
            color: 'gray'
          },
          {
            name: 'dev3',
            color: 'yellow'
          },
          {
            name: 'dev3-lock',
            color: 'gray'
          },
          {
            name: 'stage',
            color: 'blue'
          },
          {
            name: 'stage-lock',
            color: 'gray'
          },
          {
            name: 'prod',
            color: 'green'
          },
          {
            name: 'prod-lock',
            color: 'gray'
          }
        ]
      }),
      updateLabel: vi.fn().mockResolvedValue({ data: {} }),
      createLabel: vi.fn().mockResolvedValue({ data: {} })
    });

    const ssmClient = new SSMClientMock();
    container.register(SSM_CLIENT, {
      useFactory: () => ssmClient
    });

    // Seed the mock with default settings that include environments
    const { ParameterService } = await import('@src/services');
    const parameterService = container.resolve(ParameterService);
    const defaultSettings: Partial<DeploySettings> = {
      environments: [
        {
          name: 'dev',
          queue: 'dev',
          color: '#d4ac0d',
          url: '',
          requireApproval: false,
          requireBranchUpToDate: false,
          automationTest: { enabled: false },
          excludeFromRollback: []
        },
        {
          name: 'dev2',
          queue: 'dev',
          color: '#d4ac0d',
          url: '',
          requireApproval: false,
          requireBranchUpToDate: false,
          automationTest: { enabled: false },
          excludeFromRollback: []
        },
        {
          name: 'dev3',
          queue: 'dev',
          color: '#d4ac0d',
          url: '',
          requireApproval: false,
          requireBranchUpToDate: false,
          automationTest: { enabled: false },
          excludeFromRollback: []
        },
        {
          name: 'stage',
          queue: 'stage',
          color: '#2e86c1',
          url: '',
          requireApproval: false,
          requireBranchUpToDate: false,
          automationTest: { enabled: false },
          excludeFromRollback: []
        },
        {
          name: 'prod',
          queue: 'prod',
          color: '#1d8348',
          url: '',
          requireApproval: true,
          requireBranchUpToDate: false,
          automationTest: { enabled: false },
          excludeFromRollback: []
        }
      ],
      defaultEnvironment: 'dev',
      releaseEnvironment: 'prod',
      builds: {
        checkPattern: '.*',
        workflowPattern: '.*-build\\.yml$'
      },
      badge: {
        statusColors: {
          error: 'ff0000',
          warn: 'cccc00',
          success: '00ff00',
          info: '0080ff'
        }
      }
    };
    await parameterService.setObject('DEPLOY_SETTINGS', defaultSettings, 'Owner');

    const settingsService = container.resolve(DeploySettingsService);
    const settings = await settingsService.get();
    container.register(DeploySettings, { useFactory: () => settings });
  }
}
