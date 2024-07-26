[![{{environment.name}}](https://badgen.net/badge/{{environment.name}}/Checks%20Incomplete/{{badge.statusColors.warn}}?labelColor={{environment.color}}&icon=github&scale=1.2)]({{deployManagerSiteUrl}}?environment={{environment.name}}&owner={{owner}}&repo={{repo}} 'Open the queue')
The following checks are incomplete:
{{#each incompleteChecks}}

- [{{name}}]({{html_url}})
{{/each}}

Once they finish, the deployment will be retriggered.
