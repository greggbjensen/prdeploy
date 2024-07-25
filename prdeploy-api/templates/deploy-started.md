[![{{environment.name}}](https://badgen.net/badge/{{environment.name}}/Deploy%20Started/{{badge.statusColors.info}}?labelColor={{environment.color}}&icon=github&scale=1.2)](https://github.com/{{owner}}/{{repo}}/actions/runs/{{run.id}}/attempts/{{run.run_attempt}} 'Open the deploy')
{{#if environment.requireApproval}}
[Approve deployment](https://github.com/{{owner}}/{{repo}}/actions/runs/{{run.id}}/attempts/{{run.run_attempt}}) to continue
{{/if}}
