**prdeploy** shows each pull request's deploy state with GitHub's own pull request features, alongside its comments. You can see and act on it from the pull request without opening the portal.

| Feature                                                    | Where it shows                                                         | Requires                                                   |
| ---------------------------------------------------------- | ---------------------------------------------------------------------- | ---------------------------------------------------------- |
| [prdeploy check](#prdeploy-check)                          | The checks list and merge box, with buttons on the check's page.       | **Checks: Read and write**, **Check run** event.           |
| [Deployment cards](#deployment-cards)                      | The pull request timeline, and the repository's **Environments** page. | **Deployments: Read and write**.                           |
| [Deployment protection rule](#deployment-protection-rule) | The deploy workflow run, before an environment's job starts.           | **Deployments: Read and write**, **Deployment protection rule** event, and turning on the rule per environment. |

The permissions and events are listed in [1. GitHub App](./getting-started/1-github-app.md#1-configure-permissions-and-events). If one is missing, prdeploy logs a warning and keeps deploying with comments only.

## prdeploy check

Every pull request gets a check named `prdeploy` on its latest commit. It is updated whenever the pull request is deployed, queued, or freed, and when a new commit is pushed.

| State                                  | Check result     | Buttons                    |
| -------------------------------------- | ---------------- | -------------------------- |
| Not deployed or queued                 | Neutral          | **Deploy to dev**          |
| Waiting in a queue                     | Queued           | **Leave queue**            |
| Deploying to its environment           | In progress      | **Redeploy dev2**, **Free** |
| Its latest deploy succeeded            | Success          | **Redeploy dev2**, **Free** |
| Its latest deploy failed               | Failure          | **Redeploy dev2**, **Free** |
| Holding an environment, status unknown | Neutral          | **Redeploy dev2**, **Free** |

The environment names depend on your settings. Open the check's details to see the following tables:

- **Environments**: each environment the pull request is in, whether it holds the lock, and a link to its latest deploy run.
- **Queues**: its position in each deployment queue.
- **Services**: each configured service, and whether the pull request's commit built it.

Buttons work like the matching [comment commands](./comment-commands.md). **Deploy** is `/deploy`, **Redeploy** is `/deploy` to the environment the pull request holds, and **Free** and **Leave queue** are `/free`. Only users with write access can use them. Pull requests from forks get no deploy buttons.

To stop pull requests from merging while they are queued, deploying, or after a failed deploy, add `prdeploy` as a required status check in a branch protection rule or ruleset. Neutral results count as passing, so pull requests that were never deployed can still merge.

The deploy status comes from the API's memory, like the portal's **Status** column. After the API restarts, a pull request that holds an environment shows **Holding** until its next deploy run.

## Deployment cards

When prdeploy dispatches the deploy workflow, it creates a GitHub deployment for the pull request's head commit in the target environment. The deployment card on the pull request timeline goes from **Queued** to **In progress** when the run starts. It ends as **Active** with a **View deployment** button (the environment's first URL) when the run succeeds, or **Failed** when it doesn't. When another pull request deploys to the environment successfully, GitHub marks the earlier deployment **Inactive**, so the repository's **Environments** page always shows what is in each one.

A rollback is shown on the pull request that requested it, with a description starting with `Rollback:`.

These deployments use the task `deploy:prdeploy`. A deploy workflow job that uses `environment:` also gets a deployment from GitHub Actions for the default branch. Both can appear on the **Environments** page.

## Deployment protection rule

A custom deployment protection rule makes GitHub ask prdeploy before a deploy job can run in an environment. With the rule on, a deploy that bypasses the queue, such as running the deploy workflow by hand, cannot overwrite an environment another pull request is using.

1. Make sure the deploy workflow's jobs use the environment, for example `environment: ${{ inputs.environment }}`.
2. In the repository, open **Settings** → **Environments** and select an environment, such as `dev2`.
3. Under **Deployment protection rules**, check your prdeploy GitHub App and click **Save protection rules**.
4. Repeat for each environment prdeploy should guard.

For each paused job, prdeploy approves the deploy workflow only when:

- the run was started by prdeploy for that environment (its name is `[environment] branch: title`), and
- exactly one pull request holds the environment's lock label, and the run deploys that pull request's branch. prdeploy rollbacks are approved while the environment is locked.

Otherwise the job is rejected with a comment that explains why, such as which pull request holds the lock. Jobs from other workflows that use the environment are approved, because prdeploy only manages its deploy workflow. The rule is checked in addition to environment reviewers, so **Require approval** environments still wait for a reviewer.
