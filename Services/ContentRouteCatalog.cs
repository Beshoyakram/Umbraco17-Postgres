using Umbraco.Cms.Core.Models.PublishedContent;

namespace Centrocdx.Services;

/// <summary>
/// Stable public slugs that must survive content title changes and the WordPress migration.
/// </summary>
public static class ContentRouteCatalog
{
    private static readonly IReadOnlyDictionary<Guid, string> BlogSlugs =
        new Dictionary<Guid, string>
        {
            [Guid.Parse("a7010006-1111-4111-8111-111111110016")] =
                "2026-contact-center-trends-what-to-watch-this-year",
            [Guid.Parse("a7010006-1111-4111-8111-111111110011")] =
                "bilingual-excellence-at-scale-how-we-became-a-top-insurance-brokerages-most-trusted-partner",
            [Guid.Parse("53e16054-9717-4939-8b14-64e508f45700")] =
                "bpo-in-2025-what-the-next-decade-holds-for-business-process-outsourcing",
            [Guid.Parse("7e843c58-8048-4935-bf03-3cfd343afa62")] =
                "centro-forge-forging-influence-driving-growth",
            [Guid.Parse("a7010006-1111-4111-8111-11111111000f")] =
                "centro-named-iaop-global-outsourcing-100",
            [Guid.Parse("a7010006-1111-4111-8111-11111111000c")] =
                "centro-places-in-top-500-global-outsourcing-firm-index",
            [Guid.Parse("d856b2cf-76a9-4e3d-ac2c-bbf0b5ce1f24")] =
                "centro-receives-official-ntra-license-a-major-milestone-in-our-growth-journey",
            [Guid.Parse("4b167071-efab-4a7e-bcad-e2234d4abef0")] =
                "centrocdx-com-prior-authorization-automation-2027",
            [Guid.Parse("c08e32ad-2e3a-487b-a996-bf793ff1ab97")] =
                "cms-0057-f-prior-authorization-2026",
            [Guid.Parse("a7010006-1111-4111-8111-111111110002")] = "contact-center-staffing-calculator",
            [Guid.Parse("a7010006-1111-4111-8111-11111111001e")] =
                "corporate-responsibility-in-action-centros-emergency-response-initiative",
            [Guid.Parse("a7010006-1111-4111-8111-111111110013")] =
                "empowering-our-communities-g-i-v-e-and-the-egyptian-food-bank-collaboration",
            [Guid.Parse("a7010006-1111-4111-8111-111111110017")] =
                "from-inception-to-a-global-leader-the-journey-of-centro-cdx",
            [Guid.Parse("a7010006-1111-4111-8111-111111110015")] =
                "growing-careers-while-scaling-businesses",
            [Guid.Parse("a7010006-1111-4111-8111-111111110001")] =
                "healthcare-revenue-cycle-management-trends-in-2026-denials-staffing-and-the-ai-roi-gap",
            [Guid.Parse("a7010006-1111-4111-8111-111111110019")] =
                "how-outsourcing-improves-first-call-resolution-by-strategy",
            [Guid.Parse("a7010006-1111-4111-8111-11111111000a")] =
                "how-to-design-a-consistent-student-experience",
            [Guid.Parse("a7010006-1111-4111-8111-11111111001c")] =
                "maximize-revenue-minimize-delays-why-centros-rcm-solutions-matter",
            [Guid.Parse("a7010006-1111-4111-8111-11111111000e")] =
                "predictive-customer-experience-using-data-to-prevent-problems-before-they-happen",
            [Guid.Parse("a7010006-1111-4111-8111-11111111001a")] =
                "quality-assurance-in-contact-center-outsourcing-how-centro-maintains-a-91-satisfaction-rate",
            [Guid.Parse("a7010006-1111-4111-8111-11111111001b")] =
                "reducing-wait-times-increasing-loyalty-the-cx-impact-of-contact-center-outsourcing",
            [Guid.Parse("a7010006-1111-4111-8111-111111110003")] =
                "supporting-hope-centro-visits-childrens-cancer-hospital-egypt-57357-on-world-humanitarian-day",
            [Guid.Parse("a7010006-1111-4111-8111-11111111000d")] =
                "the-4-layers-of-consistent-customer-experience",
            [Guid.Parse("a7010006-1111-4111-8111-111111110018")] =
                "the-centro-effect-in-action-scaling-a-startup-from-potential-to-performance",
            [Guid.Parse("a7010006-1111-4111-8111-111111110010")] =
                "the-hidden-risk-of-inconsistent-customer-responses-across-channels",
            [Guid.Parse("a7010006-1111-4111-8111-111111110014")] =
                "the-power-of-our-core-values-displayed-in-our-everyday-work",
            [Guid.Parse("a7010006-1111-4111-8111-11111111001d")] =
                "under-the-hood-of-consumer-first-brands-how-bpo-powers-growth-with-invisible-strength",
            [Guid.Parse("a7010006-1111-4111-8111-111111110009")] =
                "what-buyers-should-ask-before-outsourcing-cx",
            [Guid.Parse("a7010006-1111-4111-8111-111111110012")] =
                "what-happens-after-the-call-the-overlooked-half-of-customer-experience",
            [Guid.Parse("a7010006-1111-4111-8111-111111110004")] =
                "what-happens-after-you-choose-an-offshore-customer-support-partner",
            [Guid.Parse("a7010006-1111-4111-8111-111111110006")] =
                "what-most-sla-reports-dont-tell-you",
            [Guid.Parse("a7010006-1111-4111-8111-111111110008")] =
                "what-we-heard-at-ccw-las-vegas-2026-the-future-of-cx-is-built-on-better-operations",
            [Guid.Parse("a7010006-1111-4111-8111-11111111000b")] =
                "why-outsourcing-education-services-is-critical-during-peak-academic-cycles",
            [Guid.Parse("a7010006-1111-4111-8111-111111110007")] =
                "why-strong-offshore-operations-still-deliver-high-quality-customer-support-outsourcing",
            [Guid.Parse("a7010006-1111-4111-8111-111111110005")] =
                "why-technology-cant-replace-great-digital-customer-experience"
        };

    private static readonly IReadOnlyDictionary<Guid, string> CareerSlugs =
        new Dictionary<Guid, string>
        {
            [Guid.Parse("a7010005-1111-4111-8111-111111111101")] = "senior-content-writer",
            [Guid.Parse("a7010005-1111-4111-8111-111111111102")] = "sales-support-specialist",
            [Guid.Parse("a7010005-1111-4111-8111-111111111103")] = "project-manager",
            [Guid.Parse("a7010005-1111-4111-8111-111111111104")] = "talent-acquisition-specialist",
            [Guid.Parse("a7010005-1111-4111-8111-111111111105")] = "gl-accountant",
            [Guid.Parse("a7010005-1111-4111-8111-111111111106")] = "sales-executive",
            [Guid.Parse("a7010005-1111-4111-8111-111111111107")] = "it-help-desk-specialist",
            [Guid.Parse("a7010005-1111-4111-8111-111111111108")] = "bd-representative",
            [Guid.Parse("a7010005-1111-4111-8111-111111111109")] = "data-entry-specialist"
        };

    public static string GetBlogSlug(IPublishedContent content, string? culture)
        => GetSlug(content, culture, BlogSlugs);

    public static string GetCareerSlug(IPublishedContent content, string? culture)
        => GetSlug(content, culture, CareerSlugs);

    public static bool IsKnownBlog(IPublishedContent content)
        => BlogSlugs.ContainsKey(content.Key);

    private static string GetSlug(
        IPublishedContent content,
        string? culture,
        IReadOnlyDictionary<Guid, string> stableSlugs)
    {
        if (stableSlugs.TryGetValue(content.Key, out var stableSlug))
        {
            return stableSlug;
        }

        return content.UrlSegment(culture) ?? content.UrlSegment() ?? string.Empty;
    }
}
