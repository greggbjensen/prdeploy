[![{{environment.name}}](https://badgen.net/badge/{{environment.name}}/Deploy%20Started/{{color badge.statusColors.info}}?labelColor={{color environment.color}}&icon=github&scale=1.2)](https://github.com/{{owner}}/{{repo}}/actions/runs/{{run.id}}/attempts/{{run.run_attempt}} 'Open the deploy')
{{#if environment.requireApproval}}
[Approve deployment](https://github.com/{{owner}}/{{repo}}/actions/runs/{{run.id}}/attempts/{{run.run_attempt}}) to continue
{{/if}}
