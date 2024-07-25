{{#if issues}}

<!-- ISSUES_START:{{branchName}} --><table><tr><td>

{{#each issues}}
![{{type}}]({{iconUrl}}) [{{key}}]({{url}}) {{{summary}}}
{{/each}}

</td></tr></table><!-- ISSUES_END -->
{{/if}}

{{{pullBody}}}
