[![{{environment.name}}](https://badgen.net/badge/{{environment.name}}/No%20Environment%20Available/{{badge.statusColors.warn}}?labelColor={{environment.color}}&icon=github&scale=1.2)]({{deployManagerSiteUrl}}?environment={{environment.name}}&owner={{owner}}&repo={{repo}} 'Open the queue')
{{#if alreadyInQueue}}
Your pull request is already in the queue at position {{queuePosition}}.
{{else}}
Your position in the queue is {{queuePosition}}.
{{/if}}

{{queueTable}}

You will be notified when your deployment has started.
