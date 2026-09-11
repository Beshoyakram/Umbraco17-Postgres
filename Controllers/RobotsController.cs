using System.Text;
using Centrocdx.Services;
using Microsoft.AspNetCore.Mvc;

namespace Centrocdx.Controllers;

public class RobotsController : Controller
{
    private readonly ISeoMetadataService _seo;

    public RobotsController(ISeoMetadataService seo)
    {
        _seo = seo;
    }

    [HttpGet("/robots.txt")]
    public IActionResult Index()
    {
        var origin = _seo.SiteOrigin.TrimEnd('/');
        var output = new StringBuilder();
        output.AppendLine("User-agent: *");
        output.AppendLine("Allow: /");
        output.AppendLine();
        output.AppendLine("# Search engines and AI discovery crawlers are allowed to read public pages.");
        output.AppendLine("# Keep backoffice and private paths out of crawler queues.");
        output.AppendLine("Disallow: /umbraco/");
        output.AppendLine("Disallow: /App_Plugins/");
        output.AppendLine("Disallow: /install/");
        output.AppendLine();
        output.AppendLine($"Sitemap: {origin}/sitemap.xml");

        Response.Headers.CacheControl = "public, max-age=900";
        return Content(output.ToString(), "text/plain", Encoding.UTF8);
    }
}
