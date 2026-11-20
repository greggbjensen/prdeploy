/**
 * Derives a service name from a workflow file path.
 *
 * The name is the file name without its folder, its `.yml` or `.yaml` extension, and a trailing `-ci` or `-build`
 * suffix, for example `.github/workflows/api-build.yaml` becomes `api`. Services are then mapped to the workflow by
 * file name on the Services tab, so no extra pattern is needed.
 *
 * @param workflowPath Workflow file path, such as `.github/workflows/api-build.yaml`.
 * @returns The extracted service name.
 */
export function workflowServiceName(workflowPath: string): string {
  const fileName = workflowPath.split('/').pop() ?? workflowPath;
  return fileName.replace(/\.ya?ml$/i, '').replace(/-(ci|build)$/i, '');
}
