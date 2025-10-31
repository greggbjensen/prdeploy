{{#if issues}}
<!-- ISSUES_START:{{branchName}} -->
{{#each issues}}
{{type}}: [{{key}}]({{url}}) {{{summary}}}
{{/each}}
<!-- ISSUES_END -->
{{/if}}

{{{pullBody}}}
