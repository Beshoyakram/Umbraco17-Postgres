using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Centrocdx.Services;

/// <summary>
/// Resolves flat solution URLs (/bpo, /health, /digital) to pages nested
/// below the Solutions holder in the backoffice.
/// </summary>
public class SolutionsFolderContentFinder : IContentFinder
{
    private static readonly Guid ContentTreeRootKey =
        Guid.Parse("b490e316-b4b1-4636-b53e-4da7a378ad29");

    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public SolutionsFolderContentFinder(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var path = request.Uri.GetAbsolutePathDecoded().Trim('/');
        if (string.IsNullOrWhiteSpace(path) || path.Contains('/'))
        {
            return Task.FromResult(false);
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
            || umbracoContext.Content is null)
        {
            return Task.FromResult(false);
        }

        var solutions = umbracoContext.Content
            .GetById(ContentTreeRootKey)
            ?.Children()
            .FirstOrDefault(x =>
                x.ContentType.Alias.Equals(
                    SolutionsFolderUrlProvider.SolutionsHolderAlias,
                    StringComparison.OrdinalIgnoreCase));

        if (solutions is null)
        {
            return Task.FromResult(false);
        }

        var culture = request.Culture;
        var solution = solutions
            .Children()
            .FirstOrDefault(x =>
                string.Equals(x.UrlSegment(culture), path, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.UrlSegment(), path, StringComparison.OrdinalIgnoreCase));

        if (solution is null)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(solution);
        return Task.FromResult(true);
    }
}
