# prdeploy

GitHub App that allows the entire build-deploy lifecycle to happen within a feature branch.  If any problems are found in the deployment, a new commit is done and rolled through without needing multiple PRs.

# Documentation

Review the prdeploy documentation at https://prdeploy.readthedocs.io.

## Repository Guides

- [prdeploy API](./prdeploy-api/README.md)
- [prdeploy web application](./prdeploy-app/README.md)
- [Local development scripts](./scripts/README.md)
- GitHub Actions:
  - [build-details](./.github/actions/build-details/README.md)
  - [date-version](./.github/actions/date-version/README.md)
  - [deploy-tags](./.github/actions/deploy-tags/README.md)
  - [get-build-info](./.github/actions/get-build-info/README.md)

# prdeploy portal

Use the prdeploy portal dashboard to overview releases and deployment queues and act on them.

![prdeploy deployments](/docs/assets/images/screenshots/prdeploy-portal.png)


# Comment Commands

Post comment commands to manage your deployments without leaving your pull request.

![Deploy comment command](/docs/assets/images/screenshots/prcomment-deploy.png)

| Command                                              | Description                                                                                                                         |
| ---------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `/deploy`<br>`/deploy dev`                           | Deploy to first available dev environment or get added to queue.                                                                    |
| `/deploy stage`                                      | Deploy to stage environment or get added to queue.                                                                                  |
| `/deploy prod`                                       | Deploy to prod environment or get added to queue.                                                                                   |
| `/deploy dev2 --force`<br>`/deploy stage --force`    | Override deploy to a specific dev, stage, or prod environment and skip the queue.                                                   |
| `/deploy stage --retain`<br>`/deploy prod --retain`  | Promote pull request to next environment, but retain the lock on the current one as well. Can also be used with --force.            |
| `/free`                                              | Remove lock on all deployed environments for others to use it.                                                                      |
| `/free stage`                                        | Remove lock on specified environment only for others to use it.                                                                     |
| `/rollback`                                          | Rollback current locked environment to the previous version. If there are multiple environments locked, you must specify which one. |
| `/rollback prod`<br>`/rollback stage`                | Rollback a specific environment to the previous version. You must have a lock on an environment to roll it back.                    |
| `/rollback prod 2`                                   | Rollback a specific environment multiple versions in history.                                                                       |
| `/add mydomain-api-service mydomain-app-main`        | Add services to be built and deployed with the pull request that have no code changes.                                              |

## Deployment Practices

1. Each pull request should be deployed as ready in the CD workflow.
2. Only once deployed and verified the pull request should be merged to main.
3. This application is deployed to a kubernetes cluster using the helm chart.

# Contributing

1. For API and webhooks development and debugging, see [prdeploy-api Development](/prdeploy-api/README.md#development).
2. For web application setup and development, see the [prdeploy web application guide](./prdeploy-app/README.md).
