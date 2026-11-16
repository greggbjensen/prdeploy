using System.Text.RegularExpressions;

namespace PrDeploy.Api.Webhooks.Utils;

/// <summary>
/// Processes HTML content in markdown strings to convert it to Slack-compatible format.
/// Handles HTML tables, line breaks, comments, and other HTML tags.
/// </summary>
/// <remarks>
/// Based on the logic from greggbjensen/slackify-markdown fork (html-tag-handling branch).
/// </remarks>
public static partial class HtmlProcessor
{
    /// <summary>
    /// Processes HTML content in a markdown string, converting HTML tags to Slack-compatible format.
    /// </summary>
    public static string ProcessHtmlInMarkdown(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return markdown ?? string.Empty;
        }

        return ProcessHtml("root", markdown);
    }

    /// <summary>
    /// Processes the HTML and applies the associated behavior.
    /// </summary>
    /// <param name="currentTag">Text used as the current tag.</param>
    /// <param name="currentValue">Text used as the current value.</param>
    /// <returns>Text produced or selected by the operation.</returns>
    private static string ProcessHtml(string currentTag, string currentValue)
    {
        if (string.IsNullOrEmpty(currentTag))
        {
            return string.Empty;
        }

        // Remove HTML comments first.
        var text = HtmlCommentRegex().Replace(currentValue, string.Empty);

        // Format closed HTML tags (e.g., <table>...</table>).
        text = ClosedTagRegex().Replace(text, match =>
        {
            var tag = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            var endWhitespace = match.Groups[4].Value;

            switch (tag.Trim().ToLowerInvariant())
            {
                case "table":
                    return $"\n{ProcessHtml(tag, value.Trim())}";

                case "tr":
                    return $"{ProcessHtml(tag, value.Trim()).Trim()}\n";

                case "td":
                case "th":
                    return $"{value.Trim()}  ";

                default:
                    // For other tags, process recursively and preserve content.
                    return $"{ProcessHtml(tag, value)}{endWhitespace}";
            }
        });

        // Format unclosed and self-closing tags (e.g., <br>, <br/>), removing all but line breaks.
        text = OpenTagRegex().Replace(text, match =>
            string.Equals(match.Groups[1].Value.Trim().TrimEnd('/'), "br", StringComparison.OrdinalIgnoreCase)
                ? "\n"
                : string.Empty);

        return text;
    }

    /// <summary>
    /// Performs the HTML comment regex operation and returns its result.
    /// </summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex("<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlCommentRegex();

    /// <summary>
    /// Performs the closed tag regex operation and returns its result.
    /// </summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"<([^>\s]+)(?:\s[^>]*)?>(.*?)(</\1>)(\s*)", RegexOptions.Singleline)]
    private static partial Regex ClosedTagRegex();

    // Excludes : so markdown autolinks like <https://example.com> are kept.
    /// <summary>
    /// Performs the open tag regex operation and returns its result.
    /// </summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"<([^>\s:]+)(?:\s[^>]*)?/?>")]
    private static partial Regex OpenTagRegex();
}
