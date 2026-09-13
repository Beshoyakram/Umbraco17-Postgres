using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;

namespace Centrocdx.Services;

public static class BlogRichTextHelper
{
    public static IHtmlContent Render(IPublishedElement content, string alias)
    {
        if (content == null || string.IsNullOrWhiteSpace(alias) || !content.HasProperty(alias))
        {
            return HtmlString.Empty;
        }

        // Prefer raw/JSON markup — seeded block RTE values render reliably this way.
        try
        {
            var raw = content.Value<string>(alias);
            var markup = ExtractMarkup(raw);
            if (!string.IsNullOrWhiteSpace(markup))
            {
                if (LooksLikeEscapedHtml(markup))
                {
                    markup = WebUtility.HtmlDecode(markup);
                }

                return new HtmlString(markup);
            }
        }
        catch
        {
            // Fall through
        }

        try
        {
            var encoded = content.Value<IHtmlEncodedString>(alias);
            if (encoded != null)
            {
                var html = encoded.ToHtmlString() ?? string.Empty;
                if (LooksLikeEscapedHtml(html))
                {
                    html = WebUtility.HtmlDecode(html);
                }

                return string.IsNullOrWhiteSpace(html) ? HtmlString.Empty : new HtmlString(html);
            }
        }
        catch
        {
            // ignore
        }

        return HtmlString.Empty;
    }

    public static string ExtractMarkup(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.TryGetProperty("markup", out var markupProp))
                {
                    return markupProp.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // Not JSON
            }
        }

        return trimmed;
    }

    private static bool LooksLikeEscapedHtml(string html)
        => html.Contains("&lt;", StringComparison.Ordinal)
           && html.Contains("&gt;", StringComparison.Ordinal)
           && !html.Contains('<');
}
