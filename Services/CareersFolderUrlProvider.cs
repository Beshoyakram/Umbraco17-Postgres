using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace Centrocdx.Services;

/// <summary>
/// Keeps job detail URLs flat (/senior-content-writer) while nested under Careers in the backoffice.
/// </summary>
public class CareersFolderUrlProvider : IUrlProvider
{
    public const string ProviderAlias = "CareersFolder";
    public const string CareersPageAlias = "careersPage";
    public const string CareerDetailAlias = "careerDetailPage";
    public const string ContentTreeRootAlias = "contentTreeRoot";

    public string Alias => ProviderAlias;

    public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
    {
        if (content == null
            || !content.ContentType.Alias.Equals(CareerDetailAlias, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = new List<string>();
        foreach (var node in content.AncestorsOrSelf().Reverse())
        {
            if (node.ContentType.Alias.Equals(CareersPageAlias, StringComparison.OrdinalIgnoreCase)
                || node.ContentType.Alias.Equals(ContentTreeRootAlias, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var segment = node.UrlSegment(culture);
            if (!string.IsNullOrWhiteSpace(segment))
            {
                segments.Add(segment);
            }
        }

        if (segments.Count == 0)
        {
            return null;
        }

        var path = "/" + string.Join("/", segments);
        return UrlInfo.AsUrl(path, ProviderAlias, culture, isExternal: false);
    }

    public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current)
        => Enumerable.Empty<UrlInfo>();

    public Task<UrlInfo?> GetPreviewUrlAsync(IContent content, string? culture, string? segment)
        => Task.FromResult<UrlInfo?>(null);
}
