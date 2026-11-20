import { workflowServiceName } from './workflow-service-name';

/** Verifies service names are derived from workflow file paths. */
describe('workflowServiceName', () => {
  it('strips the folder, extension, and -build suffix', () => {
    expect(workflowServiceName('.github/workflows/prdeploy-api-build.yaml')).toBe('prdeploy-api');
  });

  it('strips a -ci suffix and supports the yml extension in any case', () => {
    expect(workflowServiceName('.github/workflows/Web-CI.YML')).toBe('Web');
  });

  it('keeps a name that has no suffix', () => {
    expect(workflowServiceName('.github/workflows/combined-deploy.yaml')).toBe('combined-deploy');
  });

  it('keeps ci or build when it is not a suffix', () => {
    expect(workflowServiceName('.github/workflows/ci-tools.yaml')).toBe('ci-tools');
    expect(workflowServiceName('.github/workflows/build-api.yaml')).toBe('build-api');
  });

  it('handles a bare file name without a folder', () => {
    expect(workflowServiceName('api-build.yml')).toBe('api');
  });
});
