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

    private static string ConvertText(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        var placeholders = new List<string>();
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

    private static string FormatLink(string url, string text)
    {
        text = StripEmphasis(text).Trim();
        return string.IsNullOrEmpty(text) || text == url ? $"<{url}>" : $"<{url}|{Escape(text)}>";
    }

    private static string StripEmphasis(string text) => EmphasisCharsRegex().Replace(text, string.Empty);

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    [GeneratedRegex(@"^[ \t]*```[^\n]*\n(?<code>.*?)^[ \t]*```[ \t]*$", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"`[^`\n]+`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"!\[(?<text>[^\]]*)\]\((?<url>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\((?<url>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"<(?<url>https?://[^>\s]+)>")]
    private static partial Regex AutoLinkRegex();

    [GeneratedRegex(@"^(?<indent>[ \t]*)>[ \t]?", RegexOptions.Multiline)]
    private static partial Regex BlockQuoteRegex();

    [GeneratedRegex(@"^[ \t]*#{1,6}[ \t]+(?<text>.+?)[ \t]*#*[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^[ \t]*([-*_])([ \t]*\1){2,}[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"^(?<indent>[ \t]*)[-*+][ \t]+\[(?<done>[ xX])\][ \t]+", RegexOptions.Multiline)]
    private static partial Regex TaskListRegex();

    [GeneratedRegex(@"^(?<indent>[ \t]*)[-*+][ \t]+", RegexOptions.Multiline)]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"\*\*(?<text>[^\n]+?)\*\*")]
    private static partial Regex BoldAsteriskRegex();

    [GeneratedRegex(@"(?<![\w_])__(?<text>[^\n]+?)__(?![\w_])")]
    private static partial Regex BoldUnderscoreRegex();

    [GeneratedRegex(@"(?<![\w*])\*(?![\s*])(?<text>[^\n*]+?)(?<!\s)\*(?![\w*])")]
    private static partial Regex ItalicAsteriskRegex();

    [GeneratedRegex(@"~~(?<text>[^\n]+?)~~")]
    private static partial Regex StrikethroughRegex();

    [GeneratedRegex(@"[*_~]{1,3}")]
    private static partial Regex EmphasisCharsRegex();

    [GeneratedRegex("\u0002(?<index>\\d+)\u0003")]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExtraBlankLinesRegex();
}
