using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace Centrocdx.Services;

public interface ISiteInfoService
{
    IPublishedContent? GetGlobalSettingsHolder();
    IPublishedContent? GetSiteMainInformation();
    IPublishedContent? GetFooter();
    IPublishedContent? GetHomePage();
    string? GetMediaUrl(IPublishedContent? media);
}

public class SiteInfoService : ISiteInfoService
{
    private readonly UmbracoHelper _umbracoHelper;

    public SiteInfoService(UmbracoHelper umbracoHelper)
    {
        _umbracoHelper = umbracoHelper;
    }

    public IPublishedContent? GetGlobalSettingsHolder()
        => _umbracoHelper.ContentAtRoot()
            .FirstOrDefault(x => x.ContentType.Alias == "globalSiteSettingHolder");

    public IPublishedContent? GetSiteMainInformation()
        => GetGlobalSettingsHolder()?
            .Children()
            .FirstOrDefault(x => x.ContentType.Alias == "siteMainInformation")
            ?? GetGlobalSettingsHolder()?
                .DescendantsOrSelfOfType("siteMainInformation")
                .FirstOrDefault();

    public IPublishedContent? GetFooter()
        => GetGlobalSettingsHolder()?
            .Children()
            .FirstOrDefault(x => x.ContentType.Alias == "footer")
            ?? _umbracoHelper.ContentAtRoot()
                .SelectMany(x => x.DescendantsOrSelfOfType("footer"))
                .FirstOrDefault();

    public IPublishedContent? GetHomePage()
        => _umbracoHelper.ContentAtRoot()
            .SelectMany(x => x.DescendantsOrSelfOfType("homePage"))
            .FirstOrDefault();

    public string? GetMediaUrl(IPublishedContent? media)
    {
        if (media == null)
        {
            return null;
        }

        var url = media.Url();
        return string.IsNullOrWhiteSpace(url) || url == "#" ? null : url;
    }
}
