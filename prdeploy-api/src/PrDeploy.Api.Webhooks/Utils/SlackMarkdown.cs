using System.Text;
using System.Text.RegularExpressions;

namespace PrDeploy.Api.Webhooks.Utils;

/// <summary>
/// Converts GitHub flavored markdown to Slack mrkdwn.
/// </summary>
/// <remarks>
/// Replaces the slackify-markdown package used by the original Node.js webhooks.
/// SourceRef: https://api.slack.com/reference/surfaces/formatting
/// </remarks>
public static partial class SlackMarkdown
{
    private const char PlaceholderStart = '\u0002';
    private const char PlaceholderEnd = '\u0003';
    private const char BoldMarker = '\u0001';

    /// <summary>
    /// Performs the convert operation and returns its result.
    /// </summary>
    /// <param name="markdown">Value supplied for markdown.</param>
    /// <returns>The value produced by this operation.</returns>
    public static string Convert(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        var normalized = markdown.Replace("\r\n", "\n").Replace('\r', '\n');
        var result = new StringBuilder();
        var lastIndex = 0;

        // Code blocks are passed through untouched, except for the language hint Slack does not support.
        foreach (Match match in CodeBlockRegex().Matches(normalized))
        {
            result.Append(ConvertText(normalized[lastIndex..match.Index]));
            result.Append("```\n").Append(match.Groups["code"].Value.TrimEnd('\n')).Append("\n```");
            lastIndex = match.Index + match.Length;
        }

        result.Append(ConvertText(normalized[lastIndex..]));

        var text = ExtraBlankLinesRegex().Replace(result.ToString(), "\n\n");
        return text.Trim('\n');
    }

    /// <summary>
    /// Performs the convert Text operation and returns its result.
    /// </summary>
    /// <param name="text">Value supplied for text.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string ConvertText(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        var placeholders = new List<string>();
        /// <summary>
        /// Performs the protect operation and returns its result.
        /// </summary>
        /// <param name="value">Value to store or process.</param>
        /// <returns>The value produced by this operation.</returns>
        string Protect(string value)
        {
            placeholders.Add(value);
            return $"{PlaceholderStart}{placeholders.Count - 1}{PlaceholderEnd}";
        }

        // Inline code is kept as is.
        text = InlineCodeRegex().Replace(text, m => Protect(m.Value));

        // Images and links become Slack links, with their text escaped.
        text = ImageRegex().Replace(text, m => Protect(FormatLink(m.Groups["url"].Value, m.Groups["text"].Value)));
        text = LinkRegex().Replace(text, m => Protect(FormatLink(m.Groups["url"].Value, m.Groups["text"].Value)));
        text = AutoLinkRegex().Replace(text, m => Protect($"<{m.Groups["url"].Value}>"));

        // Block quotes use > which would otherwise be escaped.
        text = BlockQuoteRegex().Replace(text, m => m.Groups["indent"].Value + Protect("> "));

        text = Escape(text);

        // Line level formatting.
        text = HeadingRegex().Replace(text, m => $"{BoldMarker}{StripEmphasis(m.Groups["text"].Value)}{BoldMarker}");
        text = HorizontalRuleRegex().Replace(text, "──────────");
        text = TaskListRegex().Replace(text, m => $"{m.Groups["indent"].Value}{(m.Groups["done"].Value.Trim().Length > 0 ? "☑" : "☐")} ");
        text = BulletRegex().Replace(text, m => $"{m.Groups["indent"].Value}•   ");

        // Inline formatting, bold is marked first so it is not mistaken for italic.
        text = BoldAsteriskRegex().Replace(text, m => $"{BoldMarker}{m.Groups["text"].Value}{BoldMarker}");
        text = BoldUnderscoreRegex().Replace(text, m => $"{BoldMarker}{m.Groups["text"].Value}{BoldMarker}");
        text = ItalicAsteriskRegex().Replace(text, m => $"_{m.Groups["text"].Value}_");
        text = StrikethroughRegex().Replace(text, m => $"~{m.Groups["text"].Value}~");
        text = text.Replace(BoldMarker, '*');

        return PlaceholderRegex().Replace(text, m => placeholders[int.Parse(m.Groups["index"].Value)]);
    }

    /// <summary>
    /// Performs the format Link operation and returns its result.
    /// </summary>
    /// <param name="url">Value supplied for url.</param>
    /// <param name="text">Value supplied for text.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string FormatLink(string url, string text)
    {
        text = StripEmphasis(text).Trim();
        return string.IsNullOrEmpty(text) || text == url ? $"<{url}>" : $"<{url}|{Escape(text)}>";
    }

    /// <summary>
    /// Performs the strip Emphasis operation and returns its result.
    /// </summary>
    /// <param name="text">Value supplied for text.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string StripEmphasis(string text) => EmphasisCharsRegex().Replace(text, string.Empty);

    /// <summary>
    /// Performs the escape operation and returns its result.
    /// </summary>
    /// <param name="text">Value supplied for text.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>
    /// Performs the code Block Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"^[ \t]*```[^\n]*\n(?<code>.*?)^[ \t]*```[ \t]*$", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex CodeBlockRegex();

    /// <summary>
    /// Performs the inline Code Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"`[^`\n]+`")]
    private static partial Regex InlineCodeRegex();

    /// <summary>
    /// Performs the image Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"!\[(?<text>[^\]]*)\]\((?<url>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex ImageRegex();

    /// <summary>
    /// Performs the link Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\((?<url>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex LinkRegex();

    /// <summary>
    /// Performs the auto Link Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"<(?<url>https?://[^>\s]+)>")]
    private static partial Regex AutoLinkRegex();

    /// <summary>
    /// Performs the block Quote Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"^(?<indent>[ \t]*)>[ \t]?", RegexOptions.Multiline)]
    private static partial Regex BlockQuoteRegex();

    /// <summary>
    /// Performs the heading Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"^[ \t]*#{1,6}[ \t]+(?<text>.+?)[ \t]*#*[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    /// <summary>
    /// Performs the horizontal Rule Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"^[ \t]*([-*_])([ \t]*\1){2,}[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex HorizontalRuleRegex();

    /// <summary>
    /// Performs the task List Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"^(?<indent>[ \t]*)[-*+][ \t]+\[(?<done>[ xX])\][ \t]+", RegexOptions.Multiline)]
    private static partial Regex TaskListRegex();

    /// <summary>
    /// Performs the bullet Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"^(?<indent>[ \t]*)[-*+][ \t]+", RegexOptions.Multiline)]
    private static partial Regex BulletRegex();

    /// <summary>
    /// Performs the bold Asterisk Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"\*\*(?<text>[^\n]+?)\*\*")]
    private static partial Regex BoldAsteriskRegex();

    /// <summary>
    /// Performs the bold Underscore Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"(?<![\w_])__(?<text>[^\n]+?)__(?![\w_])")]
    private static partial Regex BoldUnderscoreRegex();

    /// <summary>
    /// Performs the italic Asterisk Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"(?<![\w*])\*(?![\s*])(?<text>[^\n*]+?)(?<!\s)\*(?![\w*])")]
    private static partial Regex ItalicAsteriskRegex();

    /// <summary>
    /// Performs the strikethrough Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"~~(?<text>[^\n]+?)~~")]
    private static partial Regex StrikethroughRegex();

    /// <summary>
    /// Performs the emphasis Chars Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"[*_~]{1,3}")]
    private static partial Regex EmphasisCharsRegex();

    /// <summary>
    /// Performs the placeholder Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex("\u0002(?<index>\\d+)\u0003")]
    private static partial Regex PlaceholderRegex();

    /// <summary>
    /// Performs the extra Blank Lines Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExtraBlankLinesRegex();
}
