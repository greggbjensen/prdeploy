{{#if isRollback}}[ROLLBACK] {{/if}}[![{{environment.name}}](https://badgen.net/badge/{{environment.name}}/Deploy%20Failed/{{badge.statusColors.error}}?labelColor={{color environment.color}}&icon=github&scale=1.2)](https://github.com/{{owner}}/{{repo}}/actions/runs/{{run.id}}/attempts/{{run.run_attempt}} 'Open the deploy')
{{message}}
