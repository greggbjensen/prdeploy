import { workflowServiceName } from './workflow-service-name';

/** Verifies service names are derived from workflow paths using the builds workflow pattern. */
describe('workflowServiceName', () => {
  const pattern = '/([^/]+?)-build.ya?ml';

  it('returns the first capture group when the pattern matches', () => {
    expect(workflowServiceName('.github/workflows/prdeploy-api-build.yaml', pattern)).toBe('prdeploy-api');
  });

  it('matches case-insensitively and supports the yml extension', () => {
    expect(workflowServiceName('.github/workflows/Web-BUILD.YML', pattern)).toBe('Web');
  });

  it('falls back to the file name without extension when the pattern does not match', () => {
    expect(workflowServiceName('.github/workflows/combined-deploy.yaml', pattern)).toBe('combined-deploy');
  });

  it('falls back to the file name when no pattern is configured', () => {
    expect(workflowServiceName('.github/workflows/api-build.yml')).toBe('api-build');
    expect(workflowServiceName('.github/workflows/api-build.yml', '')).toBe('api-build');
    expect(workflowServiceName('.github/workflows/api-build.yml', null)).toBe('api-build');
  });

  it('falls back to the file name when the pattern is not a valid expression', () => {
    expect(workflowServiceName('.github/workflows/api-build.yml', '([unclosed')).toBe('api-build');
  });

  it('falls back when the pattern has no capture group', () => {
    expect(workflowServiceName('.github/workflows/api-build.yml', '/api-build.yml')).toBe('api-build');
  });
});
