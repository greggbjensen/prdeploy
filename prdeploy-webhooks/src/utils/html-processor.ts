/**
 * Processes HTML content in markdown strings to convert it to Slack-compatible format.
 * Handles HTML tables, line breaks, comments, and other HTML tags.
 *
 * Based on the logic from greggbjensen/slackify-markdown fork (html-tag-handling branch).
 */
export class HtmlProcessor {
  /**
   * Processes HTML content recursively, converting HTML tags to Slack-compatible text.
   * @param currentTag - The current HTML tag being processed (or 'root' for top-level)
   * @param currentValue - The HTML content to process
   * @returns Processed text with HTML converted to Slack format
   */
  private static processHtml(currentTag: string, currentValue: string): string {
    if (!currentTag) {
      return '';
    }

    // Remove HTML comments first
    let text = currentValue.replace(/<!--.*?-->/gms, '');

    // Format closed HTML tags (e.g., <table>...</table>)
    // Use a more robust regex that handles nested tags better
    text = text.replace(/<([^>\s]+)(?:\s[^>]*)?>(.*?)(<\/\1>)(\s*)/gms, (match, tag, value, endTag, endWhitespace) => {
      const tagName = tag.toLowerCase().trim();
      let result = '';

      switch (tagName) {
        case 'table':
          result = HtmlProcessor.processHtml(tag, value.trim());
          result = `\n${result}`;
          break;

        case 'tr':
          result = HtmlProcessor.processHtml(tag, value.trim());
          result = `${result.trim()}\n`;
          break;

        case 'td':
        case 'th':
          result = `${value.trim()}  `;
          break;

        default:
          // For other tags, process recursively and preserve content
          result = `${HtmlProcessor.processHtml(tag, value)}${endWhitespace}`;
          break;
      }

      return result;
    });

    // Format unclosed and self-closing tags (e.g., <br>, <br/>)
    // Only process tags that weren't already handled above
    text = text.replace(/<([^>\s]+)(?:\s[^>]*)?\/?>/g, (match, tag) => {
      const tagName = tag.trim().toLowerCase();
      // Only handle self-closing tags that weren't matched by the previous regex
      if (tagName === 'br') {
        return '\n';
      }
      // For other self-closing or unclosed tags, remove the tag but keep any content
      return '';
    });

    return text;
  }

  /**
   * Processes HTML content in a markdown string, converting HTML tags to Slack-compatible format.
   * @param markdown - The markdown string that may contain HTML
   * @returns The markdown string with HTML processed for Slack compatibility
   */
  static processHtmlInMarkdown(markdown: string): string {
    if (!markdown) {
      return markdown;
    }

    // Process HTML content recursively
    return HtmlProcessor.processHtml('root', markdown);
  }
}
