using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace Centrocdx.Controllers;

public class SitemapController : Controller
{
    private readonly UmbracoHelper _umbracoHelper;

    public SitemapController(UmbracoHelper umbracoHelper)
    {
        _umbracoHelper = umbracoHelper;
    }

    [HttpGet("/sitemap.xml")]
    public IActionResult Index()
    {
        var urls = new List<XElement>();

        foreach (var root in _umbracoHelper.ContentAtRoot())
        {
            CollectUrls(root, urls);
        }

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "urlset", urls));

        return Content(document.ToString(), "application/xml", Encoding.UTF8);
    }

    private static void CollectUrls(IPublishedContent content, List<XElement> urls)
    {
        // Include pages that have a template (public website pages).
        if (content.TemplateId > 0)
        {
            var absoluteUrl = content.Url(mode: UrlMode.Absolute);
            if (!string.IsNullOrWhiteSpace(absoluteUrl) && absoluteUrl != "#")
            {
                XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                urls.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", absoluteUrl),
                    new XElement(ns + "lastmod", content.UpdateDate.ToString("yyyy-MM-dd"))));
            }
        }

        foreach (var child in content.Children())
        {
            CollectUrls(child, urls);
        }
    }
}
