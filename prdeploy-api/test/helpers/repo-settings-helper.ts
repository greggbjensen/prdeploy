import { SSM_CLIENT } from '@src/injection-tokens';
import { GetContentParams, RepoSettings } from '@src/models';
import { RepoSettingsService } from '@src/services';
import { SSMClientMock } from '@test/mocks';
import { Octokit } from 'octokit';
import { container } from 'tsyringe';

export class RepoSettingsHelper {
  static async mockCalls(octokit: Octokit): Promise<void> {
    const getContent = octokit.rest.repos.getContent.bind(octokit.rest.repos);

    Object.assign(octokit.rest.repos, {
      getContent: jest.fn().mockImplementation(async (params: GetContentParams) => {
        return params.path === RepoSettingsService.RepoSettingsFile
          ? {
              data: {
                content: ''
              }
            }
          : await getContent(params);
      }),
      get: jest.fn().mockResolvedValueOnce({
        data: {
          default_branch: 'main'
        }
      }),
      getAllEnvironments: jest.fn().mockResolvedValueOnce({
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
      listLabelsForRepo: jest.fn().mockResolvedValueOnce({
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
      })
    });

    container.register(SSM_CLIENT, {
      useFactory: () => new SSMClientMock()
    });

    const settingsService = container.resolve(RepoSettingsService);
    const settings = await settingsService.get();
    container.register(RepoSettings, { useFactory: () => settings });
  }
}
