using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Centrocdx.Services;

/// <summary>
/// Resolves the domain root to the Home document nested below Content Tree.
/// </summary>
public class HomePageContentFinder : IContentFinder
{
    private static readonly Guid ContentTreeRootKey =
        Guid.Parse("b490e316-b4b1-4636-b53e-4da7a378ad29");

    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public HomePageContentFinder(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        if (!string.IsNullOrWhiteSpace(request.Uri.GetAbsolutePathDecoded().Trim('/')))
        {
            return Task.FromResult(false);
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
            || umbracoContext.Content is null)
        {
            return Task.FromResult(false);
        }

        var home = umbracoContext.Content
            .GetById(ContentTreeRootKey)
            ?.ChildrenOfType(HomePageUrlProvider.HomePageAlias)
            .FirstOrDefault();

        if (home is null)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(home);
        return Task.FromResult(true);
    }
}
