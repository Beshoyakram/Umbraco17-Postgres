using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Centrocdx.Services;

public sealed record SeoMetadata(
    string Title,
    string Description,
    string Keywords,
    string CanonicalUrl,
    string Robots,
    string OpenGraphType,
    string? ImageUrl,
    string JsonLd);

public interface ISeoMetadataService
{
    string SiteOrigin { get; }
    SeoMetadata Build(IPublishedContent content);
    string GetCanonicalUrl(IPublishedContent content);
    bool IsNoIndex(IPublishedContent content);
    bool IsExcludedFromSitemap(IPublishedContent content);
}

public class SeoMetadataService : ISeoMetadataService
{
    private readonly ISiteInfoService _siteInfo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Uri _siteOrigin;

    public SeoMetadataService(
        ISiteInfoService siteInfo,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _siteInfo = siteInfo;
        _httpContextAccessor = httpContextAccessor;
        var configuredOrigin = configuration["Seo:SiteUrl"] ?? "https://centrocdx.com";
        _siteOrigin = new Uri(configuredOrigin.TrimEnd('/') + "/", UriKind.Absolute);
    }

    public string SiteOrigin => _siteOrigin.GetLeftPart(UriPartial.Authority);

    public SeoMetadata Build(IPublishedContent content)
    {
        var site = _siteInfo.GetSiteMainInformation();
        var siteName = FirstNonEmpty(site?.Value<string>("siteTitle"), "Centro");
        var pageName = FirstNonEmpty(content.Name, siteName);
        var pageTitle = FirstNonEmpty(
            content.Value<string>("sEOTitle"),
            content.Value<string>("postTitle"),
            content.Value<string>("title"),
            content.Value<string>("bannerTitle"),
            pageName);
        var title = pageTitle.Contains(siteName, StringComparison.OrdinalIgnoreCase)
            ? pageTitle
            : $"{pageTitle} | {siteName}";
        var description = StripHtml(FirstNonEmpty(
            content.Value<string>("sEODescription"),
            content.Value<string>("excerpt"),
            content.Value<string>("brief"),
            content.Value<string>("sectionDescription")));
        var keywords = FirstNonEmpty(
            content.Value<string>("sEOKeywords"),
            content.Value<string>("keyWords"));
        var canonicalUrl = GetCanonicalUrl(content);
        var imageUrl = GetAbsoluteMediaUrl(content, "ogImage")
            ?? GetAbsoluteMediaUrl(content, "featuredImage")
            ?? GetAbsoluteMediaUrl(content, "mainImage")
            ?? GetAbsoluteMediaUrl(content, "coverImage")
            ?? GetAbsoluteMediaUrl(site, "siteLogo");
        var isBlogPost = content.ContentType.Alias.Equals(
            BlogFolderUrlProvider.BlogDetailAlias,
            StringComparison.OrdinalIgnoreCase);

        return new SeoMetadata(
            title,
            description,
            keywords,
            canonicalUrl,
            IsNoIndex(content)
                ? "noindex, follow, max-image-preview:large"
                : "index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1",
            isBlogPost ? "article" : "website",
            imageUrl,
            BuildStructuredData(
                content,
                siteName,
                title,
                description,
                canonicalUrl,
                imageUrl,
                isBlogPost));
    }

    public string GetCanonicalUrl(IPublishedContent content)
    {
        var canonicalOverride = content.Value<string>("canonicalOverride")?.Trim();
        if (!string.IsNullOrWhiteSpace(canonicalOverride))
        {
            return MakeAbsolute(canonicalOverride);
        }

        var path = GetPublicPath(content);

        if (string.IsNullOrWhiteSpace(path) || path == "#")
        {
            path = _httpContextAccessor.HttpContext?.Request.Path.Value ?? "/";
        }

        return MakeAbsolute(path);
    }

    public bool IsNoIndex(IPublishedContent content)
        => content.HasProperty("noIndex") && content.Value<bool>("noIndex");

    public bool IsExcludedFromSitemap(IPublishedContent content)
        => content.HasProperty("excludeFromSitemap")
            && content.Value<bool>("excludeFromSitemap");

    private string BuildStructuredData(
        IPublishedContent content,
        string siteName,
        string title,
        string description,
        string canonicalUrl,
        string? imageUrl,
        bool isBlogPost)
    {
        var origin = _siteOrigin.GetLeftPart(UriPartial.Authority);
        var organizationId = $"{origin}/#organization";
        var websiteId = $"{origin}/#website";
        var graph = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["@type"] = "Organization",
                ["@id"] = organizationId,
                ["name"] = siteName,
                ["url"] = origin + "/",
                ["logo"] = imageUrl is null
                    ? null
                    : new Dictionary<string, object?>
                    {
                        ["@type"] = "ImageObject",
                        ["url"] = GetAbsoluteMediaUrl(
                            _siteInfo.GetSiteMainInformation(),
                            "siteLogo") ?? imageUrl
                    }
            },
            new()
            {
                ["@type"] = "WebSite",
                ["@id"] = websiteId,
                ["url"] = origin + "/",
                ["name"] = siteName,
                ["publisher"] = new Dictionary<string, object?> { ["@id"] = organizationId },
                ["inLanguage"] = CultureInfo.CurrentCulture.Name
            }
        };

        var page = new Dictionary<string, object?>
        {
            ["@type"] = isBlogPost ? "BlogPosting" : "WebPage",
            ["@id"] = canonicalUrl + (isBlogPost ? "#article" : "#webpage"),
            ["url"] = canonicalUrl,
            ["name"] = title,
            ["headline"] = isBlogPost ? title : null,
            ["description"] = description,
            ["isPartOf"] = new Dictionary<string, object?> { ["@id"] = websiteId },
            ["inLanguage"] = CultureInfo.CurrentCulture.Name,
            ["image"] = imageUrl,
            ["dateModified"] = content.UpdateDate.ToUniversalTime().ToString("O"),
            ["breadcrumb"] = new Dictionary<string, object?>
            {
                ["@id"] = canonicalUrl + "#breadcrumb"
            }
        };

        if (isBlogPost)
        {
            var publishDate = content.HasValue("publishDate")
                ? content.Value<DateTime>("publishDate")
                : content.CreateDate;
            page["datePublished"] = publishDate.ToUniversalTime().ToString("O");
            page["articleSection"] = content.Value<string>("category");
            page["author"] = new Dictionary<string, object?> { ["@id"] = organizationId };
            page["publisher"] = new Dictionary<string, object?> { ["@id"] = organizationId };
            page["mainEntityOfPage"] = canonicalUrl;
        }

        graph.Add(page);
        graph.Add(BuildBreadcrumbs(content, canonicalUrl));

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = graph
        });
    }

    private Dictionary<string, object?> BuildBreadcrumbs(
        IPublishedContent content,
        string canonicalUrl)
    {
        var items = new List<Dictionary<string, object?>>();
        var home = _siteInfo.GetHomePage();
        if (home is not null)
        {
            items.Add(BreadcrumbItem(1, home.Name, GetCanonicalUrl(home)));
        }

        var ancestorsAndSelf = content
            .AncestorsOrSelf()
            .Reverse()
            .Where(x => x.TemplateId > 0
                && !x.ContentType.Alias.Equals(
                    "contentTreeRoot",
                    StringComparison.OrdinalIgnoreCase)
                && (home is null || x.Key != home.Key));

        foreach (var item in ancestorsAndSelf)
        {
            items.Add(BreadcrumbItem(
                items.Count + 1,
                item.Name,
                item.Key == content.Key ? canonicalUrl : GetCanonicalUrl(item)));
        }

        return new Dictionary<string, object?>
        {
            ["@type"] = "BreadcrumbList",
            ["@id"] = canonicalUrl + "#breadcrumb",
            ["itemListElement"] = items
        };
    }

    private static Dictionary<string, object?> BreadcrumbItem(
        int position,
        string name,
        string url)
        => new()
        {
            ["@type"] = "ListItem",
            ["position"] = position,
            ["name"] = name,
            ["item"] = url
        };

    private string? GetAbsoluteMediaUrl(IPublishedElement? content, string alias)
    {
        if (content is null || !content.HasProperty(alias) || !content.HasValue(alias))
        {
            return null;
        }

        IPublishedContent? media = null;
        try
        {
            media = content.Value<MediaWithCrops>(alias)?.Content
                ?? content.Value<IPublishedContent>(alias);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        var mediaUrl = _siteInfo.GetMediaUrl(media);
        return string.IsNullOrWhiteSpace(mediaUrl) ? null : MakeAbsolute(mediaUrl);
    }

    private string MakeAbsolute(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        var normalizedPath = "/" + url.Trim().TrimStart('/');
        if (normalizedPath != "/" && !Path.HasExtension(normalizedPath))
        {
            normalizedPath = normalizedPath.TrimEnd('/') + "/";
        }

        return new Uri(_siteOrigin, normalizedPath).ToString();
    }

    private static string GetPublicPath(IPublishedContent content)
    {
        var alias = content.ContentType.Alias;
        if (alias.Equals(HomePageUrlProvider.HomePageAlias, StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        if (alias.Equals(BlogFolderUrlProvider.BlogDetailAlias, StringComparison.OrdinalIgnoreCase))
        {
            return "/" + ContentRouteCatalog.GetBlogSlug(content, "en-US") + "/";
        }

        if (alias.Equals(CareersFolderUrlProvider.CareerDetailAlias, StringComparison.OrdinalIgnoreCase))
        {
            return "/" + ContentRouteCatalog.GetCareerSlug(content, "en-US") + "/";
        }

        if (content.Ancestors().Any(x => x.ContentType.Alias.Equals(
                SolutionsFolderUrlProvider.SolutionsHolderAlias,
                StringComparison.OrdinalIgnoreCase)))
        {
            return "/" + GetSegment(content) + "/";
        }

        var segments = content
            .AncestorsOrSelf()
            .Reverse()
            .Where(x => !x.ContentType.Alias.Equals(
                "contentTreeRoot",
                StringComparison.OrdinalIgnoreCase))
            .Select(GetSegment)
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return "/" + string.Join("/", segments) + "/";
    }

    private static string GetSegment(IPublishedContent content)
    {
        var segment = content.UrlSegment("en-US") ?? content.UrlSegment();
        if (!string.IsNullOrWhiteSpace(segment))
        {
            return segment;
        }

        return Regex.Replace(content.Name.ToLowerInvariant(), @"[^a-z0-9]+", "-")
            .Trim('-');
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim()
            ?? string.Empty;

    private static string StripHtml(string value)
        => Regex.Replace(value, "<[^>]+>", " ")
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Trim();
}
