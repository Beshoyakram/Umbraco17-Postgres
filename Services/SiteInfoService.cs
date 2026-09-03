using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace Centrocdx.Services;

public interface ISiteInfoService
{
    IPublishedContent? GetGlobalSettingsHolder();
    IPublishedContent? GetSiteMainInformation();
    IPublishedContent? GetFooter();
    IPublishedContent? GetHomePage();
    IPublishedContent? GetAboutPage();
    IPublishedContent? GetGetInTouchPage();
    IPublishedContent? GetSolutionPage(string urlSegment);
    IEnumerable<IPublishedContent> GetSolutionPages();
    string? GetMediaUrl(IPublishedContent? media);
    Link? GetFirstLink(IPublishedElement? content, string alias);
    IReadOnlyList<Link> GetLinks(IPublishedElement? content, string alias);
}

public class SiteInfoService : ISiteInfoService
{
    private readonly UmbracoHelper _umbracoHelper;

    public SiteInfoService(UmbracoHelper umbracoHelper)
    {
        _umbracoHelper = umbracoHelper;
    }

    public IPublishedContent? GetGlobalSettingsHolder()
        => _umbracoHelper.ContentAtRoot()
            .FirstOrDefault(x => x.ContentType.Alias == "globalSiteSettingHolder");

    public IPublishedContent? GetSiteMainInformation()
        => GetGlobalSettingsHolder()?
            .Children()
            .FirstOrDefault(x => x.ContentType.Alias == "siteMainInformation")
            ?? GetGlobalSettingsHolder()?
                .DescendantsOrSelfOfType("siteMainInformation")
                .FirstOrDefault();

    public IPublishedContent? GetFooter()
        => GetGlobalSettingsHolder()?
            .Children()
            .FirstOrDefault(x => x.ContentType.Alias == "footer")
            ?? _umbracoHelper.ContentAtRoot()
                .SelectMany(x => x.DescendantsOrSelfOfType("footer"))
                .FirstOrDefault();

    public IPublishedContent? GetHomePage()
        => _umbracoHelper.ContentAtRoot()
            .SelectMany(x => x.DescendantsOrSelfOfType("homePage"))
            .FirstOrDefault();

    public IPublishedContent? GetAboutPage()
        => _umbracoHelper.ContentAtRoot()
            .SelectMany(x => x.DescendantsOrSelfOfType("aboutPage"))
            .FirstOrDefault();

    public IPublishedContent? GetGetInTouchPage()
        => _umbracoHelper.ContentAtRoot()
            .SelectMany(x => x.DescendantsOrSelfOfType("getInTouchPage"))
            .FirstOrDefault();

    public IPublishedContent? GetSolutionPage(string urlSegment)
        => GetSolutionPages()
            .FirstOrDefault(x =>
                string.Equals(x.UrlSegment, urlSegment, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Name, urlSegment, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IPublishedContent> GetSolutionPages()
        => _umbracoHelper.ContentAtRoot()
            .SelectMany(x => x.DescendantsOrSelfOfType("solutionPage"))
            .OrderBy(x => x.SortOrder);

    public string? GetMediaUrl(IPublishedContent? media)
    {
        if (media == null)
        {
            return null;
        }

        try
        {
            var url = media.Url();
            if (!string.IsNullOrWhiteSpace(url) && url != "#")
            {
                return NormalizePath(url);
            }
        }
        catch (ArgumentException)
        {
            // Fall through to raw umbracoFile parsing
        }

        return NormalizePath(ExtractPathFromUmbracoFile(media.Value<string>(Constants.Conventions.Media.File)));
    }

    public Link? GetFirstLink(IPublishedElement? content, string alias)
        => GetLinks(content, alias).FirstOrDefault();

    public IReadOnlyList<Link> GetLinks(IPublishedElement? content, string alias)
    {
        if (content == null || string.IsNullOrWhiteSpace(alias) || !content.HasProperty(alias) || !content.HasValue(alias))
        {
            return Array.Empty<Link>();
        }

        try
        {
            var many = content.Value<IEnumerable<Link>>(alias);
            if (many != null)
            {
                return many.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Url)).ToList();
            }
        }
        catch (Exception)
        {
            // Single-link configured data types return Link instead of IEnumerable<Link>
        }

        try
        {
            var one = content.Value<Link>(alias);
            if (one != null && !string.IsNullOrWhiteSpace(one.Url))
            {
                return new[] { one };
            }
        }
        catch (Exception)
        {
            // Ignore invalid stored values
        }

        return Array.Empty<Link>();
    }

    private static string? ExtractPathFromUmbracoFile(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        raw = raw.Trim();
        if (raw.StartsWith('/'))
        {
            return raw;
        }

        if (raw.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("src", out var src))
                {
                    return src.GetString();
                }
            }
            catch (JsonException)
            {
                var start = raw.IndexOf("/media", StringComparison.OrdinalIgnoreCase);
                if (start >= 0)
                {
                    var end = raw.IndexOf('"', start);
                    if (end > start)
                    {
                        return raw[start..end];
                    }
                }
            }
        }

        return null;
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "#")
        {
            return null;
        }

        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return path.StartsWith('/') ? path : "/" + path.TrimStart('/');
    }
}
