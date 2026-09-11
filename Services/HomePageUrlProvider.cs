using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace Centrocdx.Services;

/// <summary>
/// Makes the public homepage canonical at the domain root.
/// </summary>
public class HomePageUrlProvider : IUrlProvider
{
    public const string HomePageAlias = "homePage";
    public string Alias => "HomePage";

    public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
    {
        if (!content.ContentType.Alias.Equals(HomePageAlias, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return UrlInfo.AsUrl("/", Alias, culture, isExternal: false);
    }

    public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current)
        => Enumerable.Empty<UrlInfo>();

    public Task<UrlInfo?> GetPreviewUrlAsync(IContent content, string? culture, string? segment)
        => Task.FromResult<UrlInfo?>(null);
}
