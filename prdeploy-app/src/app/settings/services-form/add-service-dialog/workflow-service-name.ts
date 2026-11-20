/**
 * Derives a service name from a workflow file path by applying the builds workflow pattern.
 *
 * The pattern is matched case-insensitively, mirroring how the API extracts build names, and the first capture
 * group is the service name. When the pattern is missing, invalid, or does not match, the file name without its
 * extension and without a trailing `-ci` or `-build` suffix is used so the user always gets a usable starting value.
 *
 * @param workflowPath Workflow file path, such as `.github/workflows/api-build.yaml`.
 * @param pattern Builds workflow pattern, such as `/([^/]+?)-build.ya?ml`.
 * @returns The extracted service name.
 */
export function workflowServiceName(workflowPath: string, pattern?: string | null): string {
  if (pattern) {
    try {
      const match = new RegExp(pattern, 'i').exec(workflowPath);
      if (match && match[1]) {
        return match[1];
      }
    } catch {
      // An invalid pattern falls through to the file name.
    }
  }

  const fileName = workflowPath.split('/').pop() ?? workflowPath;
  return fileName.replace(/\.ya?ml$/i, '').replace(/-(ci|build)$/i, '');
}
