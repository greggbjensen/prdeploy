# deploy tags action

Gets a set of release tags for deployment of Docker images and helm charts, this included `latest` and multiple versions like `1.2.15`, `1.2`, and `1`.

## Inputs

### `repository`

The docker repository to generate tags for.  Example: `greggbjensen/prdeploy`

## Outputs

### `tags`

The comma separated list of tags to use.

## Behavior

| Build                                      | Tags                                                      |
| ------------------------------------------ | --------------------------------------------------------- |
| Not a release                              | `temp` (not pushed by the container build).               |
| Stable release, `1.2.15` or `v1.2.15`      | `<repository>:1.2.15`, `:1.2`, `:1`, and `:latest`; a `v` prefix is kept on each version tag. |
| Pre-release or other tag, `1.3.0-beta.1`   | `<repository>:1.3.0-beta.1` only, so it never replaces `latest` or a stable line. |

## Example usage

```yaml
- name: Deploy tags
  id: deploy_tags
  uses: greggbjensen/prdeploy/.github/actions/deploy-tags@main
  with:
    repository: greggbjensen/${{ inputs.service_name }}

- name: Docker build and push
  uses: docker/build-push-action@v5
  with:
    push: ${{ github.event_name == 'release' && github.event.action == 'created' }}
    tags: ${{ steps.deploy_tags.outputs.tags }}
    file: ${{ inputs.service_name }}/Dockerfile
    context: ${{ inputs.service_name }}
```

### Output

```
greggbjensen/prdeploy-api:1.2.15, greggbjensen/prdeploy-api:1.2, greggbjensen/prdeploy-api:1, greggbjensen/prdeploy-api:latest
```

## Testing

The logic is in `deploy-tags.sh`. Run its tests with:

```bash
.github/actions/deploy-tags/__tests__/deploy-tags.test.sh
```

The `actions - test` workflow runs them, and `shellcheck`, on pull requests that change the actions.
