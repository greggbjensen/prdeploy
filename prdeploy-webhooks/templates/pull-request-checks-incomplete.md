[![{{environment.name}}](https://badgen.net/badge/{{environment.name}}/Checks%20Incomplete/{{color badge.statusColors.warn}}?labelColor={{color environment.color}}&icon=github&scale=1.2)]({{prdeployPortalUrl}}/{{owner}}/{{repo}}?environment={{environment.name}} 'Open the queue')
The following checks are incomplete:
{{#each incompleteChecks}}

- [{{name}}]({{html_url}})
{{/each}}

Once they finish, the deployment will be retriggered.
