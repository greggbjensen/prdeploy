using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Tests.Webhooks.Services;

public class SlackMarkdownTest
{
    [Theory]
    [InlineData("**Bold text** and *italic text*", "*Bold text* and _italic text_")]
    [InlineData("__bold__ and _italic_", "*bold* and _italic_")]
    [InlineData("~~gone~~", "~gone~")]
    [InlineData("[a link](https://example.com)", "<https://example.com|a link>")]
    [InlineData("[**bold link**](https://example.com)", "<https://example.com|bold link>")]
    [InlineData("![screenshot](https://example.com/a.png)", "<https://example.com/a.png|screenshot>")]
    [InlineData("<https://example.com>", "<https://example.com>")]
    [InlineData("## Summary ##", "*Summary*")]
    [InlineData("# **Already bold**", "*Already bold*")]
    [InlineData("a < b && c > d", "a &lt; b &amp;&amp; c &gt; d")]
    [InlineData("Use `**not bold**` here", "Use `**not bold**` here")]
    [InlineData("> quoted & text", "> quoted &amp; text")]
    [InlineData("snake_case_name stays", "snake_case_name stays")]
    [InlineData("2 * 3 * 4", "2 * 3 * 4")]
    public void Convert_ConvertsInlineMarkdown(string markdown, string expected)
    {
        Assert.Equal(expected, SlackMarkdown.Convert(markdown));
    }

    [Fact]
    public void Convert_ConvertsLists()
    {
        var result = SlackMarkdown.Convert("- one\n* two\n  + nested\n- [x] done\n- [ ] todo\n1. first");
        Assert.Equal("•   one\n•   two\n  •   nested\n☑ done\n☐ todo\n1. first", result);
    }

    [Fact]
    public void Convert_KeepsCodeBlocksUntouched()
    {
        var result = SlackMarkdown.Convert("Before **x**\n\n```csharp\nvar a = b < c && **d**;\n```\n\nAfter *y*");
        Assert.Equal("Before *x*\n\n```\nvar a = b < c && **d**;\n```\n\nAfter _y_", result);
    }

    [Fact]
    public void Convert_CollapsesExtraBlankLines()
    {
        Assert.Equal("a\n\nb", SlackMarkdown.Convert("\r\n\r\na\r\n\r\n\r\n\r\nb\r\n"));
    }
}
