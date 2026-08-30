using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Centrocdx.Services;

/// <summary>
/// Copies solution page images from the theme folder into /media/solutions/…
/// so Umbraco Media items (managed in backoffice / uSync) resolve correctly.
/// </summary>
public class SolutionMediaSeedHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static int _ran;

    private readonly IMediaService _mediaService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SolutionMediaSeedHandler> _logger;

    public SolutionMediaSeedHandler(
        IMediaService mediaService,
        IWebHostEnvironment environment,
        ILogger<SolutionMediaSeedHandler> logger)
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
            var themeRoot = Path.Combine(_environment.WebRootPath, "theme", "centrotheme", "img");
            var mediaRoot = Path.Combine(_environment.WebRootPath, "media", "solutions");
            var copied = 0;
            var repaired = 0;

            foreach (var item in SeedItems)
            {
                if (!File.Exists(Path.Combine(themeRoot, item.ThemeFile)))
                {
                    _logger.LogWarning("Solution media seed skipped missing theme file {File}", item.ThemeFile);
                    continue;
                }

                var destDir = Path.Combine(mediaRoot, item.PageFolder);
                Directory.CreateDirectory(destDir);

                var destPath = Path.Combine(destDir, item.MediaFile);
                var sourcePath = Path.Combine(themeRoot, item.ThemeFile);

                if (!File.Exists(destPath))
                {
                    File.Copy(sourcePath, destPath, overwrite: false);
                    copied++;
                }

                var media = _mediaService.GetById(item.MediaKey);
                if (media == null)
                {
                    continue;
                }

                var webPath = $"/media/solutions/{item.PageFolder}/{item.MediaFile}";
                var json = $"{{\"src\":\"{webPath}\"}}";
                var current = media.GetValue<string>(Constants.Conventions.Media.File);
                if (!string.Equals(current, json, StringComparison.Ordinal) && !string.Equals(current, webPath, StringComparison.Ordinal))
                {
                    media.SetValue(Constants.Conventions.Media.File, json);
                    _mediaService.Save(media);
                    repaired++;
                }
            }

            if (copied > 0 || repaired > 0)
            {
                _logger.LogInformation("Solution media seed: copied {Copied} file(s), repaired {Repaired} media path(s).", copied, repaired);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Solution media seed skipped.");
        }

        return Task.CompletedTask;
    }

    private sealed record SolutionMediaSeedItem(
        Guid MediaKey,
        string PageFolder,
        string MediaFile,
        string ThemeFile);

    private static readonly SolutionMediaSeedItem[] SeedItems =
    [
        new(Guid.Parse("a7010002-2222-4222-8222-211111111701"), "bpo", "banner.png", "BPO 4.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111702"), "bpo", "intro.png", "image 67.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111703"), "bpo", "contact-center.png", "Contact-center.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111704"), "bpo", "hr-payroll.png", "Hr-&-Payroll.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111705"), "bpo", "it.png", "IT.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111706"), "bpo", "back-office.png", "Back-Office.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111711"), "health", "banner.png", "Medical.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111712"), "health", "intro.png", "image 68.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111713"), "health", "fifteen-years.png", "15-Years-Of-experience.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111714"), "health", "cost-effective.png", "Cost-Effective.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111715"), "health", "compliance.png", "compliance-Assurance.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111716"), "health", "patient-care.png", "Patient-Care.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111721"), "digital", "banner.png", "Digital-Transformation 2.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111722"), "digital", "intro.png", "image 69.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111723"), "digital", "proven-expertise.png", "Proven Expertise.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111724"), "digital", "tailored-strategies.png", "Tailored Strategies.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111725"), "digital", "end-to-end-support.png", "end to end support.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111726"), "digital", "future-ready.png", "Future Ready.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111727"), "digital", "operational.png", "Operational.png"),
        new(Guid.Parse("a7010002-2222-4222-8222-211111111728"), "digital", "revenue-growth.png", "Growth.png"),
    ];
}
