The prdeploy app is able to post deployment and release notifications to Slack as well as tag the person they are for to keep development moving quickly.

- Deployments - A quick summary for any environment a deployment completes in.
- Releases - The full detail of a pull request when it is release to production.

## Configure Slack
1. Create a new Slack App
2. Enable Webhooks.
3. Copy the `Bot User OAuth Token` into `SLACK_TOKEN` in your AWS Parameters.
3. Add the following scopes:
    ```
    incoming-webhook
    users:read
    users:read.email
    ```
4. Add a webhook for your deploy and release channels.