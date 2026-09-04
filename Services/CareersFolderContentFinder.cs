using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Centrocdx.Services;

/// <summary>
/// Resolves flat job URLs (/senior-content-writer) to careerDetailPage nodes nested under Careers.
/// Companion to <see cref="CareersFolderUrlProvider"/> — UrlProvider alone only affects outbound links.
/// </summary>
public class CareersFolderContentFinder : IContentFinder
{
    /// <summary>uSync key for the Careers listing page.</summary>
    private static readonly Guid CareersPageKey = Guid.Parse("a7010005-1111-4111-8111-111111111001");

    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public CareersFolderContentFinder(IUmbracoContextAccessor umbracoContextAccessor)
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

        var careers = umbracoContext.Content.GetById(CareersPageKey)
            ?? umbracoContext.Content
                .GetById(Guid.Parse("b490e316-b4b1-4636-b53e-4da7a378ad29"))
                ?.Children()
                .FirstOrDefault(x =>
                    x.ContentType.Alias.Equals(
                        CareersFolderUrlProvider.CareersPageAlias,
                        StringComparison.OrdinalIgnoreCase));

        if (careers is null)
        {
            return Task.FromResult(false);
        }

        var culture = request.Culture;
        var job = careers
            .ChildrenOfType(CareersFolderUrlProvider.CareerDetailAlias)
            .FirstOrDefault(x =>
                string.Equals(x.UrlSegment(culture), path, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.UrlSegment(), path, StringComparison.OrdinalIgnoreCase));

        if (job is null)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(job);
        return Task.FromResult(true);
    }
}
