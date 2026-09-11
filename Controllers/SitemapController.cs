using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;
using Centrocdx.Services;

namespace Centrocdx.Controllers;

public class SitemapController : Controller
{
    private readonly UmbracoHelper _umbracoHelper;
    private readonly ISeoMetadataService _seo;
    private readonly ISiteInfoService _siteInfo;
    private readonly IVariationContextAccessor _variationContextAccessor;

    public SitemapController(
        UmbracoHelper umbracoHelper,
        ISeoMetadataService seo,
        ISiteInfoService siteInfo,
        IVariationContextAccessor variationContextAccessor)
    {
        _umbracoHelper = umbracoHelper;
        _seo = seo;
        _siteInfo = siteInfo;
        _variationContextAccessor = variationContextAccessor;
    }

    [HttpGet("/sitemap.xml")]
    public IActionResult Index()
    {
        _variationContextAccessor.VariationContext ??= new VariationContext("en-US");
        var urls = new List<XElement>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allowedBlogKeys = _siteInfo.GetBlogPosts()
            .Select(x => x.Key)
            .ToHashSet();

        foreach (var root in _umbracoHelper.ContentAtRoot())
        {
            CollectUrls(root, urls, seenUrls, allowedBlogKeys);
        }

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "urlset", urls));

        Response.Headers.CacheControl = "public, max-age=900";
        return Content(document.ToString(), "application/xml", Encoding.UTF8);
    }

    private void CollectUrls(
        IPublishedContent content,
        List<XElement> urls,
        HashSet<string> seenUrls,
        HashSet<Guid> allowedBlogKeys)
    {
        // ContentAtRoot/Children expose the published cache only. Avoid IsPublished()
        // here because sitemap requests do not have a routed page culture context.
        var isExcludedLegacyBlog = content.ContentType.Alias.Equals(
                BlogFolderUrlProvider.BlogDetailAlias,
                StringComparison.OrdinalIgnoreCase)
            && !allowedBlogKeys.Contains(content.Key);

        if (!isExcludedLegacyBlog
            && content.TemplateId > 0
            && !content.ContentType.Alias.Equals(
                "contentTreeRoot",
                StringComparison.OrdinalIgnoreCase)
            && !_seo.IsNoIndex(content)
            && !_seo.IsExcludedFromSitemap(content))
        {
            var canonicalUrl = _seo.GetCanonicalUrl(content);
            if (seenUrls.Add(canonicalUrl))
            {
                XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                urls.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", canonicalUrl),
                    new XElement(ns + "lastmod", content.UpdateDate.ToString("yyyy-MM-dd"))));
            }
        }

        foreach (var child in content.Children())
        {
            CollectUrls(child, urls, seenUrls, allowedBlogKeys);
        }
    }
}
