using System.Text.Json;
using Umbraco.Cms.Core;
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
    string? GetMediaUrl(IPublishedContent? media);
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
