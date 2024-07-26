![Add](https://badgen.net/badge/Add/Services%20Not%20Found/{{badge.statusColors.error}}?labelColor=000000&icon=github&scale=1.2)
The following services are not configured:
{{#each services}}

- {{this}}
{{/each}}

Add to your .prdeploy.yaml `services` section to configure them.
