using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace Centrocdx.Services;

/// <summary>
/// Keeps the Free Consultation / Get In Touch page at the live WordPress slug /contactus/.
/// </summary>
public class ContactUsUrlProvider : IUrlProvider
{
    public const string ProviderAlias = "ContactUs";
    public const string GetInTouchPageAlias = "getInTouchPage";
    public const string PublicSlug = "contactus";

    public string Alias => ProviderAlias;

    public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
    {
        if (content == null
            || !content.ContentType.Alias.Equals(GetInTouchPageAlias, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return UrlInfo.AsUrl($"/{PublicSlug}/", ProviderAlias, culture, isExternal: false);
    }

    public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current)
        => Enumerable.Empty<UrlInfo>();

    public Task<UrlInfo?> GetPreviewUrlAsync(IContent content, string? culture, string? segment)
        => Task.FromResult<UrlInfo?>(null);
}
