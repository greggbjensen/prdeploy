import * as path from 'path';
import { expect } from '@jest/globals';
import { CheckService } from '@src/services';
import { ContainerHelper } from '@test/helpers';
import AdmZip from 'adm-zip';
import { container } from 'tsyringe';

const useMocks = !process.env.GITHUB_TOKEN || process.env.USE_MOCKS === 'true';

describe('listBuilds', () => {
  beforeEach(async () => {
    const octokit = await ContainerHelper.registerDefaults();
    if (useMocks) {
      Object.assign(octokit.rest.checks, {
        listForRef: jest.fn().mockResolvedValueOnce({
          data: {
            check_runs: [
              {
                name: 'mydomain-app-main',
                status: 'completed',
                details_url:
                  'https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/10014663490/job/27684771116'
              }
            ]
          }
        })
      });

      const zipFilePath = path.resolve(path.join(__dirname, '../mocks/build-details.zip'));
      const zipData = new AdmZip(zipFilePath).toBuffer();

      Object.assign(octokit.rest.actions, {
        getWorkflowRun: jest.fn().mockResolvedValueOnce({
          data: {
            display_title: 'SCRUM-1234 For certain flows, load variants with parent properties',
            path: 'mydomain-app-main-build.yml',
            html_url: 'https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/10014663490'
          }
        }),
        listWorkflowRunArtifacts: jest.fn().mockResolvedValueOnce({
          data: {
            artifacts: [
              {
                id: 654321,
                name: 'build-details'
              }
            ]
          }
        }),
        downloadArtifact: jest.fn().mockResolvedValueOnce({
          data: zipData
        })
      });
    }
  });

  it('lists builds and versions from a reference', async () => {
    const checkService = container.resolve(CheckService);
    const builds = await checkService.listBuilds(
      'greggbjensen',
      'prdeploy-example-repo',
      {
        head: {
          ref: 'SCRUM-16750-my-new-feature'
        }
      } as any, // Update to a branch that exists.
      [8054571563] // Update to build run IDs that exist if not mocked.
    );

    expect(builds).not.toBeNull();
    expect(builds.length).toBeGreaterThan(0);

    const build = builds[0];
    expect(build.name).toBeTruthy();
    expect(build.version).toBeTruthy();
  });
});
