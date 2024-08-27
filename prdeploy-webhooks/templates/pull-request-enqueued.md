[![{{environment.name}}](https://badgen.net/badge/{{environment.name}}/No%20Environment%20Available/{{color badge.statusColors.warn}}?labelColor={{color environment.color}}&icon=github&scale=1.2)]({{prdeployPortalUrl}}/{{owner}}/{{repo}}/deployments?environment={{environment.name}} 'Open the queue')
{{#if alreadyInQueue}}
Your pull request is already in the queue at position {{queuePosition}}.
{{else}}
Your position in the queue is {{queuePosition}}.
{{/if}}

{{queueTable}}

You will be notified when your deployment has started.
