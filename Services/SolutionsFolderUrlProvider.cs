using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace Centrocdx.Services;

/// <summary>
/// Keeps solution page URLs flat (/bpo, /health, /digital) even when nested under a Solutions folder.
/// </summary>
public class SolutionsFolderUrlProvider : IUrlProvider
{
    public const string ProviderAlias = "SolutionsFolder";
    public const string SolutionsHolderAlias = "solutionsHolder";
    public const string ContentTreeRootAlias = "contentTreeRoot";

    public string Alias => ProviderAlias;

    public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
    {
        if (content == null)
        {
            return null;
        }

        // Only handle pages that live under the Solutions folder (not the folder itself).
        if (content.ContentType.Alias.Equals(SolutionsHolderAlias, StringComparison.OrdinalIgnoreCase)
            || !HasSolutionsHolderAncestor(content))
        {
            return null;
        }

        var segments = new List<string>();
        foreach (var node in content.AncestorsOrSelf().Reverse())
        {
            if (node.ContentType.Alias.Equals(SolutionsHolderAlias, StringComparison.OrdinalIgnoreCase)
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

    private static bool HasSolutionsHolderAncestor(IPublishedContent content)
        => content.Ancestors().Any(x =>
            x.ContentType.Alias.Equals(SolutionsHolderAlias, StringComparison.OrdinalIgnoreCase));
}
