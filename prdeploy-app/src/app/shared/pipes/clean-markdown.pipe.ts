import { Pipe, PipeTransform } from '@angular/core';
import { Maybe } from 'graphql/jsutils/Maybe';

@Pipe({
  standalone: true,
  name: 'cleanMarkdown'
})
export class CleanMarkdownPipe implements PipeTransform {
  private static readonly CleanRegex = /!\[.*?\]\(.*?\)/gi;

  transform(value: Maybe<string> | undefined): string {
    // Trim out images as they are too large and the links cannot be displayed.
    return value ? (value as string).replace(CleanMarkdownPipe.CleanRegex, '') : '';
  }
}
