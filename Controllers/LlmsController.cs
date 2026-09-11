using System.Text;
using Centrocdx.Services;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace Centrocdx.Controllers;

public class LlmsController : Controller
{
    private readonly UmbracoHelper _umbracoHelper;
    private readonly ISiteInfoService _siteInfo;
    private readonly ISeoMetadataService _seo;
    private readonly IVariationContextAccessor _variationContextAccessor;

    public LlmsController(
        UmbracoHelper umbracoHelper,
        ISiteInfoService siteInfo,
        ISeoMetadataService seo,
        IVariationContextAccessor variationContextAccessor)
    {
        _umbracoHelper = umbracoHelper;
        _siteInfo = siteInfo;
        _seo = seo;
        _variationContextAccessor = variationContextAccessor;
    }

    [HttpGet("/llms.txt")]
    public IActionResult Index()
    {
        _variationContextAccessor.VariationContext ??= new VariationContext("en-US");
        var output = new StringBuilder();
        output.AppendLine("# Centro");
        output.AppendLine();
        output.AppendLine("> Customer experience, business process outsourcing, healthcare revenue cycle management, and digital transformation services.");
        output.AppendLine();
        var origin = _seo.SiteOrigin.TrimEnd('/');
        output.AppendLine($"Canonical website: {origin}/");
        output.AppendLine();
        output.AppendLine("## Main pages");

        var contentRoot = _umbracoHelper
            .ContentAtRoot()
            .FirstOrDefault(x => x.ContentType.Alias.Equals(
                "contentTreeRoot",
                StringComparison.OrdinalIgnoreCase));

        if (contentRoot is not null)
        {
            foreach (var page in contentRoot
                .Descendants()
                .Where(IsDiscoverable)
                .Where(x => !x.ContentType.Alias.Equals(
                    BlogFolderUrlProvider.BlogDetailAlias,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Level)
                .ThenBy(x => x.SortOrder))
            {
                AppendLink(output, page);
            }
        }

        output.AppendLine();
        output.AppendLine("## Articles");
        foreach (var post in _siteInfo.GetBlogPosts().Where(IsDiscoverable))
        {
            AppendLink(output, post);
        }

        output.AppendLine();
        output.AppendLine("## Crawling");
        output.AppendLine($"- Sitemap: {origin}/sitemap.xml");
        output.AppendLine($"- Robots: {origin}/robots.txt");

        Response.Headers.CacheControl = "public, max-age=900";
        return Content(output.ToString(), "text/plain", Encoding.UTF8);
    }

    private bool IsDiscoverable(IPublishedContent content)
        // These nodes come from Umbraco's published cache.
        => content.TemplateId > 0
            && !_seo.IsNoIndex(content)
            && !_seo.IsExcludedFromSitemap(content);

    private void AppendLink(StringBuilder output, IPublishedContent content)
    {
        var title = content.Value<string>("postTitle") ?? content.Name;
        var description = content.Value<string>("sEODescription")
            ?? content.Value<string>("excerpt");
        output.Append("- [")
            .Append(title.Replace("[", "\\[").Replace("]", "\\]"))
            .Append("](")
            .Append(_seo.GetCanonicalUrl(content))
            .Append(')');

        if (!string.IsNullOrWhiteSpace(description))
        {
            output.Append(": ").Append(description.Trim());
        }

        output.AppendLine();
    }
}
