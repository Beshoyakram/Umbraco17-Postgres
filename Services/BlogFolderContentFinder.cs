using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Centrocdx.Services;

/// <summary>
/// Resolves flat blog URLs (/post-slug) to blogDetailPage nodes nested under Blog.
/// </summary>
public class BlogFolderContentFinder : IContentFinder
{
    private static readonly Guid BlogPageKey = Guid.Parse("a7010006-1111-4111-8111-111111111001");

    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public BlogFolderContentFinder(IUmbracoContextAccessor umbracoContextAccessor)
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

        var blog = umbracoContext.Content.GetById(BlogPageKey)
            ?? umbracoContext.Content
                .GetById(Guid.Parse("b490e316-b4b1-4636-b53e-4da7a378ad29"))
                ?.Children()
                .FirstOrDefault(x =>
                    x.ContentType.Alias.Equals(
                        BlogFolderUrlProvider.BlogPageAlias,
                        StringComparison.OrdinalIgnoreCase));

        if (blog is null)
        {
            return Task.FromResult(false);
        }

        var culture = request.Culture;
        var post = blog
            .ChildrenOfType(BlogFolderUrlProvider.BlogDetailAlias)
            .FirstOrDefault(x =>
                string.Equals(x.UrlSegment(culture), path, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.UrlSegment(), path, StringComparison.OrdinalIgnoreCase));

        if (post is null)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(post);
        return Task.FromResult(true);
    }
}
