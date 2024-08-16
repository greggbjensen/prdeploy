# deploy tags action

Gets a set of release tags for deployment of Docker images and helm charts, this included `latest` and multiple versions like `1.2.15`, `1.2`, and `1`.

## Outputs

### `tags`

The comma separated list of tags to use.

## Example usage with 3-digit pre-release tag

```yaml
- name: Deploy tags
  id: deploy_tags
  uses: greggbjensen/prdeploy/.github/actions/deploy-tags@main

- name: Docker build and push
  uses: docker/build-push-action@v5
  with:
    push: ${{ github.event_name == 'release' && github.event.action == 'created' }}
    tags: ${{ steps.deploy_tags.output.tags }}
    file: ${{ inputs.service_name }}/Dockerfile
    context: ${{ inputs.service_name }}
```

### Output

```
1.2.15, 1.2, 1, latest
```