{{#if issues}}
<!-- ISSUES_START:{{branchName}} -->
{{#each issues}}
![{{type}}]({{iconUrl}}) [{{key}}]({{url}}) {{{summary}}}
{{/each}}
<!-- ISSUES_END -->
{{/if}}

{{{pullBody}}}
