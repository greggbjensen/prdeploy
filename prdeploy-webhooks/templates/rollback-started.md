[![{{environment.name}}](https://badgen.net/badge/{{environment.name}}/Rollback%20Started/{{color badge.statusColors.warn}}?labelColor={{color environment.color}}&icon=github&scale=1.2)](https://github.com/{{owner}}/{{repo}}/actions/runs/{{run.id}}/attempts/{{run.run_attempt}} 'Open the deploy')
{{#if environment.requireApproval}}
[Approve deployment](https://github.com/{{owner}}/{{repo}}/actions/runs/{{run.id}}/attempts/{{run.run_attempt}}) to continue
{{/if}}
