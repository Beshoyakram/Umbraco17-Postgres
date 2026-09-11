using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Centrocdx.Services;

/// <summary>
/// Ensures blog banner + post media paths point at /media/blog/… files.
/// </summary>
public class BlogMediaSeedHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static int _ran;
    private static readonly Guid BlogBannerKey = Guid.Parse("a7010006-2222-4222-8222-211111111701");

    private readonly IMediaService _mediaService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<BlogMediaSeedHandler> _logger;

    public BlogMediaSeedHandler(
        IMediaService mediaService,
        IWebHostEnvironment environment,
        ILogger<BlogMediaSeedHandler> logger)
    {
        _mediaService = mediaService;
        _environment = environment;
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _ran, 1) == 1)
        {
            return Task.CompletedTask;
        }

        try
        {
            var mediaRoot = Path.Combine(_environment.WebRootPath, "media", "blog");
            Directory.CreateDirectory(mediaRoot);

            var themeBanner = Path.Combine(_environment.WebRootPath, "theme", "centrotheme", "img", "BLOG 4.png");
            var bannerDest = Path.Combine(mediaRoot, "banner.png");
            if (File.Exists(themeBanner) && !File.Exists(bannerDest))
            {
                File.Copy(themeBanner, bannerDest, overwrite: false);
            }

            RepairMediaPath(BlogBannerKey, "/media/blog/banner.png");

            foreach (var item in PostImages)
            {
                RepairMediaPath(item.MediaKey, $"/media/blog/{item.FileName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blog media seed skipped.");
        }

        return Task.CompletedTask;
    }

    private void RepairMediaPath(Guid mediaKey, string webPath)
    {
        var media = _mediaService.GetById(mediaKey);
        if (media == null)
        {
            return;
        }

        var json = $"{{\"src\":\"{webPath}\"}}";
        var current = media.GetValue<string>(Constants.Conventions.Media.File);
        if (!string.Equals(current, json, StringComparison.Ordinal)
            && !string.Equals(current, webPath, StringComparison.Ordinal))
        {
            media.SetValue(Constants.Conventions.Media.File, json);
            _mediaService.Save(media);
        }
    }

    private sealed record PostImageSeed(Guid MediaKey, string FileName);

    private static readonly PostImageSeed[] PostImages =
    [
        new(Guid.Parse("a7010006-2222-4222-8222-211111110710"), "healthcare-revenue-cycle-management-trends-in-2026-denials-staffing-and-the-ai-roi-gap.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110711"), "contact-center-staffing-calculator.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110712"), "supporting-hope-centro-visits-childrens-cancer-hospital-egypt-57357-on-world-humanitarian-day.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110713"), "what-happens-after-you-choose-an-offshore-customer-support-partner.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110714"), "why-technology-cant-replace-great-digital-customer-experience.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110715"), "what-most-sla-reports-dont-tell-you.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110716"), "why-strong-offshore-operations-still-deliver-high-quality-customer-support-outsourcing.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110717"), "what-we-heard-at-ccw-las-vegas-2026-the-future-of-cx-is-built-on-better-operations.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110718"), "what-buyers-should-ask-before-outsourcing-cx.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110719"), "how-to-design-a-consistent-student-experience.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111071a"), "why-outsourcing-education-services-is-critical-during-peak-academic-cycles.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111071b"), "centro-places-in-top-500-global-outsourcing-firm-index.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111071c"), "the-4-layers-of-consistent-customer-experience.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111071d"), "predictive-customer-experience-using-data-to-prevent-problems-before-they-happen.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111071e"), "centro-named-iaop-global-outsourcing-100.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111071f"), "the-hidden-risk-of-inconsistent-customer-responses-across-channels.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110720"), "bilingual-excellence-at-scale-how-we-became-a-top-insurance-brokerages-most-trusted-partner.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110721"), "what-happens-after-the-call-the-overlooked-half-of-customer-experience.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110722"), "empowering-our-communities-g-i-v-e-and-the-egyptian-food-bank-collaboration.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110723"), "the-power-of-our-core-values-displayed-in-our-everyday-work.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110724"), "growing-careers-while-scaling-businesses.jpeg"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110725"), "2026-contact-center-trends-what-to-watch-this-year.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110726"), "from-inception-to-a-global-leader-the-journey-of-centro-cdx.jpg"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110727"), "the-centro-effect-in-action-scaling-a-startup-from-potential-to-performance.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110728"), "how-outsourcing-improves-first-call-resolution-by-strategy.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110729"), "quality-assurance-in-contact-center-outsourcing-how-centro-maintains-a-91-satisfaction-rate.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111072a"), "reducing-wait-times-increasing-loyalty-the-cx-impact-of-contact-center-outsourcing.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111072b"), "maximize-revenue-minimize-delays-why-centros-rcm-solutions-matter.jpg"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111072c"), "under-the-hood-of-consumer-first-brands-how-bpo-powers-growth-with-invisible-strength.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111072d"), "corporate-responsibility-in-action-centros-emergency-response-initiative.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111072e"), "bpo-in-2025-what-the-next-decade-holds-for-business-process-outsourcing.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-21111111072f"), "centro-forge-forging-influence-driving-growth.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110730"), "centro-receives-official-ntra-license-a-major-milestone-in-our-growth-journey.jpg"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110731"), "centrocdx-com-prior-authorization-automation-2027.png"),
        new(Guid.Parse("a7010006-2222-4222-8222-211111110732"), "cms-0057-f-prior-authorization-2026.png")
    ];
}
