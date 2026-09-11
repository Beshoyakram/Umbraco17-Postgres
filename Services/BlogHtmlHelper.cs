using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace Centrocdx.Services;

/// <summary>
/// Renders production-mirrored blog HTML embeds without breaking the host page/sidebar.
/// Keeps original &lt;style&gt; and &lt;script&gt; so interactive articles match centrocdx.com.
/// </summary>
public static partial class BlogHtmlHelper
{
    [GeneratedRegex(@"</?\s*p\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LooseParagraphRegex();

    [GeneratedRegex(@"<!DOCTYPE[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DoctypeRegex();

    [GeneratedRegex(@"</?\s*html[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"<head\b[^>]*>([\s\S]*?)</head>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeadBlockRegex();

    [GeneratedRegex(@"</?\s*body[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BodyTagRegex();

    [GeneratedRegex(@"<style\b[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StyleBlockRegex();

    [GeneratedRegex(@"<script\b[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScriptBlockRegex();

    public static IHtmlContent ToHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return HtmlString.Empty;
        }

        var raw = html.Trim();

        // WordPress often wraps custom HTML in bare <p>…</p>; scrape can leave orphan tags.
        // Only strip bare <p>/< /p> — never classed paragraphs (real content).
        for (var i = 0; i < 8; i++)
        {
            var next = Regex.Replace(raw, @"^</?p>\s*", string.Empty, RegexOptions.IgnoreCase).Trim();
            next = Regex.Replace(next, @"\s*</?p>$", string.Empty, RegexOptions.IgnoreCase).Trim();
            if (string.Equals(next, raw, StringComparison.Ordinal))
            {
                break;
            }

            raw = next;
        }

        // Production sometimes wraps the whole custom document in a single <p>
        if (raw.StartsWith("<p>", StringComparison.OrdinalIgnoreCase)
            && (raw.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                || raw.Contains("<html", StringComparison.OrdinalIgnoreCase)
                || raw.Contains("<style", StringComparison.OrdinalIgnoreCase)))
        {
            raw = LooseParagraphRegex().Replace(raw, string.Empty).Trim();
        }

        var htmlClose = raw.LastIndexOf("</html>", StringComparison.OrdinalIgnoreCase);
        if (htmlClose >= 0)
        {
            raw = raw[..(htmlClose + "</html>".Length)];
        }
        else
        {
            foreach (var marker in new[] { "<div class=\"col-lg-4\">", "blog_right_sidebar", "single_sidebar_widget" })
            {
                var idx = raw.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx > 500)
                {
                    raw = raw[..idx].TrimEnd();
                    break;
                }
            }
        }

        raw = DoctypeRegex().Replace(raw, string.Empty);
        raw = HtmlTagRegex().Replace(raw, string.Empty);

        var styles = new StringBuilder();
        var scripts = new StringBuilder();

        var headMatch = HeadBlockRegex().Match(raw);
        if (headMatch.Success)
        {
            var headInner = headMatch.Groups[1].Value;
            foreach (Match style in StyleBlockRegex().Matches(headInner))
            {
                styles.AppendLine(style.Value);
            }
            foreach (Match script in ScriptBlockRegex().Matches(headInner))
            {
                if (!IsStructuredDataScript(script.Value))
                {
                    scripts.AppendLine(script.Value);
                }
            }
            raw = raw.Remove(headMatch.Index, headMatch.Length);
        }

        raw = BodyTagRegex().Replace(raw, string.Empty);

        foreach (Match style in StyleBlockRegex().Matches(raw))
        {
            styles.AppendLine(style.Value);
        }
        foreach (Match script in ScriptBlockRegex().Matches(raw))
        {
            if (!IsStructuredDataScript(script.Value))
            {
                scripts.AppendLine(script.Value);
            }
        }

        var markup = StyleBlockRegex().Replace(raw, string.Empty);
        markup = ScriptBlockRegex().Replace(markup, string.Empty);
        markup = Regex.Replace(
            markup,
            @"<(meta|title|link|base)\b[^>]*/?>",
            string.Empty,
            RegexOptions.IgnoreCase);
        markup = markup.Trim();

        var sb = new StringBuilder();
        if (styles.Length > 0)
        {
            sb.AppendLine(styles.ToString().Trim());
        }

        sb.AppendLine("<div class=\"blog-html-embed\">");
        sb.AppendLine(markup);
        sb.AppendLine("</div>");

        if (scripts.Length > 0)
        {
            sb.AppendLine(scripts.ToString().Trim());
        }

        return new HtmlString(sb.ToString());
    }

    private static bool IsStructuredDataScript(string script)
        => script.Contains(
            "application/ld+json",
            StringComparison.OrdinalIgnoreCase);
}
