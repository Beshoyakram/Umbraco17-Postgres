using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Centrocdx.Services;

/// <summary>
/// Resolves /contactus to the Get In Touch page (mirrors https://centrocdx.com/contactus/).
/// </summary>
public class ContactUsContentFinder : IContentFinder
{
    private static readonly Guid GetInTouchPageKey = Guid.Parse("a7010003-1111-4111-8111-111111111001");
    private static readonly Guid ContentTreeRootKey = Guid.Parse("b490e316-b4b1-4636-b53e-4da7a378ad29");

    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public ContactUsContentFinder(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var path = request.Uri.GetAbsolutePathDecoded().Trim('/');
        if (!string.Equals(path, ContactUsUrlProvider.PublicSlug, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
            || umbracoContext.Content is null)
        {
            return Task.FromResult(false);
        }

        var page = umbracoContext.Content.GetById(GetInTouchPageKey)
            ?? umbracoContext.Content
                .GetById(ContentTreeRootKey)
                ?.ChildrenOfType(ContactUsUrlProvider.GetInTouchPageAlias)
                .FirstOrDefault()
            ?? umbracoContext.Content
                .GetById(ContentTreeRootKey)
                ?.DescendantsOrSelfOfType(ContactUsUrlProvider.GetInTouchPageAlias)
                .FirstOrDefault();

        if (page is null)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(page);
        return Task.FromResult(true);
    }
}
